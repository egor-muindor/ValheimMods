using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Muindor.ServerConfig
{
    /// <summary>
    /// The settings a server may decide for its clients, and the current answer to "whose settings
    /// are we running on".
    ///
    /// A setting bound through <see cref="Bind{T}(string,string,T,string)"/> is synced: when the
    /// server runs the same mod with <see cref="Priority"/> on, its value replaces the client's for
    /// as long as the connection lasts. Everything bound through the plain
    /// <see cref="ConfigFile.Bind{T}(ConfigDefinition,T,ConfigDescription)"/> stays the player's own,
    /// which is what keys, window positions and other local comforts want.
    ///
    /// Nothing here makes the mod required on either side: see <see cref="ConfigChannel"/>.
    /// </summary>
    public sealed class ConfigSync
    {
        /// <summary>Marks a setting as one the server can decide, in the config file the player reads.</summary>
        private const string SyncedNote = "[synced] ";

        private readonly Dictionary<string, ISyncedEntry> _byKey = new Dictionary<string, ISyncedEntry>(StringComparer.Ordinal);

        private readonly List<ISyncedEntry> _entries = new List<ISyncedEntry>();

        private readonly HashSet<ConfigEntryBase> _watched = new HashSet<ConfigEntryBase>();

        private readonly ConfigFile _config;

        private readonly ManualLogSource _log;

        public ConfigSync(ConfigFile config, string pluginGuid, string modName, string modVersion, ManualLogSource log)
        {
            _config = config;
            _log = log;
            PluginGuid = pluginGuid;
            ModName = modName;
            ModVersion = modVersion;

            Priority = config.Bind("Server", "ConfigPriority", false,
                "Server only, and off by default. Turn it on in the configuration of a dedicated server, or of the player " +
                "hosting the session, to have every client that also has the mod run on the settings marked [synced] below " +
                "instead of its own. The clients' own config files are never touched: their settings come back the moment " +
                "they disconnect. This never makes the mod required on either side - players without it are unaffected, and " +
                "a player with it can still join a server that does not have it.");
            _watched.Add(Priority);
            config.SettingChanged += OnSettingChanged;
        }

        /// <summary>Raised on the server when a synced setting, or <see cref="Priority"/> itself, changes.</summary>
        public event Action? Changed;

        /// <summary>The plugin GUID, used to name this mod's RPC.</summary>
        public string PluginGuid { get; }

        public string ModName { get; }

        public string ModVersion { get; }

        /// <summary>Server side: this server's settings replace those of the clients that have the mod.</summary>
        public ConfigEntry<bool> Priority { get; }

        /// <summary>Client side: true while the settings come from the server.</summary>
        public bool Locked { get; private set; }

        /// <summary>Client side: the mod version the server runs, while its settings are in charge.</summary>
        public string? ServerVersion { get; private set; }

        /// <summary>Client side: how many settings the server decided.</summary>
        public int LockedCount { get; private set; }

        /// <summary>Binds a setting the server may decide.</summary>
        public SyncedEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return Bind(section, key, defaultValue, new ConfigDescription(description));
        }

        /// <summary>Binds a setting the server may decide, with a value range or other metadata.</summary>
        public SyncedEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription description)
        {
            var marked = new ConfigDescription(SyncedNote + description.Description, description.AcceptableValues, description.Tags);
            ConfigEntry<T> local = _config.Bind(section, key, defaultValue, marked);
            var entry = new SyncedEntry<T>(local);
            _byKey.Add(entry.Key, entry);
            _entries.Add(entry);
            _watched.Add(local);
            return entry;
        }

        /// <summary>Server side: this server's answer for a client that just connected.</summary>
        public ConfigPayload BuildPayload()
        {
            bool priority = Priority.Value;
            var values = new List<KeyValuePair<string, string>>(priority ? _entries.Count : 0);
            if (priority)
            {
                foreach (ISyncedEntry entry in _entries)
                {
                    try
                    {
                        values.Add(new KeyValuePair<string, string>(entry.Key, entry.LocalAsString()));
                    }
                    catch (Exception exception)
                    {
                        _log.LogWarning($"{entry.Key} could not be sent to the clients: {exception.Message}");
                    }
                }
            }

            return new ConfigPayload(ModVersion, priority, values);
        }

        /// <summary>
        /// Client side: takes what the server sent. Settings the server did not send, or sent with a
        /// value this version cannot read, keep the player's own value.
        /// </summary>
        public void Apply(ConfigPayload payload)
        {
            ClearValues();
            if (!payload.Priority)
            {
                Locked = false;
                ServerVersion = null;
                LockedCount = 0;
                _log.LogInfo($"The server runs {ModName} {payload.ModVersion} with ConfigPriority off; keeping the local settings");
                return;
            }

            int applied = 0;
            var unknown = new List<string>();
            foreach (KeyValuePair<string, string> pair in payload.Values)
            {
                if (!_byKey.TryGetValue(pair.Key, out ISyncedEntry entry))
                {
                    unknown.Add(pair.Key);
                    continue;
                }

                if (entry.ApplyServerValue(pair.Value, out string error))
                {
                    applied++;
                }
                else
                {
                    _log.LogWarning($"Keeping the local {pair.Key}: the server sent a value this version cannot read, {error}");
                }
            }

            Locked = true;
            ServerVersion = payload.ModVersion;
            LockedCount = applied;

            int missing = _entries.Count - applied;
            string extra = unknown.Count > 0 ? $", {unknown.Count} unknown here ({string.Join(", ", unknown.ToArray())})" : string.Empty;
            string kept = missing > 0 ? $", {missing} kept local" : string.Empty;
            _log.LogInfo($"The server ({ModName} {payload.ModVersion}) decides the settings: {applied} applied{kept}{extra}");
        }

        /// <summary>Client side: back to the player's own settings.</summary>
        public void Clear()
        {
            if (!Locked && LockedCount == 0)
            {
                return;
            }

            ClearValues();
            Locked = false;
            ServerVersion = null;
            LockedCount = 0;
            _log.LogInfo("Back to the local settings");
        }

        /// <summary>One phrase for the log and the console command: where the settings come from.</summary>
        public string Describe()
        {
            return Locked
                ? $"settings from the server ({ModName} {ServerVersion}, {LockedCount} values)"
                : "local settings";
        }

        private void ClearValues()
        {
            foreach (ISyncedEntry entry in _entries)
            {
                entry.ClearServerValue();
            }
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            if (Changed != null && args.ChangedSetting is ConfigEntryBase changed && _watched.Contains(changed))
            {
                Changed();
            }
        }
    }
}
