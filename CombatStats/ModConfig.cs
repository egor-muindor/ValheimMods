using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using CombatStats.Model;
using Muindor.ServerConfig;
using UnityEngine;

namespace CombatStats
{
    /// <summary>
    /// Typed access to <c>muindor.CombatStats.cfg</c>.
    ///
    /// Only what players exchange over the network is bound through <see cref="Sync"/>, so a
    /// server that also runs CombatStats with <c>ConfigPriority</c> on can decide whether combat
    /// data travels between its players at all. Keys, windows, colours and which extra meters a
    /// player wants to see stay their own.
    /// </summary>
    public sealed class ModConfig
    {
        /// <summary>The windows offered by the detail view when the setting cannot be read.</summary>
        public const string DefaultWindows = "30, 300, 600, 1800";

        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;

            Sync = new ConfigSync(config, MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, Plugin.Log);

            Enabled = Sync.Bind("General", "Enabled", true,
                "Record combat and show the windows. With this off the mod does nothing at all.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log every recorded event and every packet. Noisy; for working out why a number looks wrong.");
            HistoryMinutes = config.Bind("General", "HistoryMinutes", 30,
                new ConfigDescription("How far back the meter remembers, in minutes. This is also the longest window the detail view can show.",
                    new AcceptableValueRange<int>(1, 60)));

            CountDamageTaken = config.Bind("Meters", "CountDamageTaken", false,
                "Also record the damage players take, on its own tab of the detail window.");
            CountHealing = config.Bind("Meters", "CountHealing", false,
                "Also record healing: food, potions and magic.");
            CountObjectDamage = config.Bind("Meters", "CountObjectDamage", false,
                "Also record damage to trees, ore, buildings and ships, on its own tab, so felling a forest does not drown the combat numbers.");
            CountPets = config.Bind("Meters", "CountPets", false,
                "Count what tamed creatures and summons deal. Their damage goes to the row of the player they follow, or to a row named after the creature when no owner can be found.");

            CompactKey = config.Bind("Compact", "CompactKey", new KeyboardShortcut(KeyCode.F10),
                "Cycles the compact window: shown while fighting, always shown, off. Modifiers are allowed, e.g. \"F10 + LeftShift\".");
            CompactMode = config.Bind("Compact", "CompactMode", Model.MeterMode.Auto,
                "Auto: the window appears with the first hit and fades out once the fight goes quiet. Always: it stays on screen. Off: it is never shown, but the detail window still has the numbers.");
            CompactWindow = config.Bind("Compact", "CompactWindow", 30,
                new ConfigDescription("Seconds the compact window adds up.",
                    new AcceptableValueRange<int>(5, 600)));
            CompactRows = config.Bind("Compact", "CompactRows", 5,
                new ConfigDescription("How many players the compact window lists. Your own row is always among them.",
                    new AcceptableValueRange<int>(1, 12)));
            CompactHideAfter = config.Bind("Compact", "CompactHideAfter", 5f,
                new ConfigDescription("Seconds without a single event before the compact window fades out.",
                    new AcceptableValueRange<float>(1f, 120f)));
            CompactAnchor = config.Bind("Compact", "CompactAnchor", ScreenAnchor.TopLeft,
                "Which edge of the screen the compact window is pinned to. The offset below is measured from there.");
            CompactOffset = config.Bind("Compact", "CompactOffset", new Vector2(24f, -180f),
                "Position of the compact window from its anchor, in UI pixels (x right, y up).");
            CompactWidth = config.Bind("Compact", "CompactWidth", 320f,
                new ConfigDescription("Width of the compact window in UI pixels.",
                    new AcceptableValueRange<float>(160f, 700f)));
            CompactScale = config.Bind("Compact", "CompactScale", 1f,
                new ConfigDescription("Size of the compact window, 1 being the size the game's own text is.",
                    new AcceptableValueRange<float>(0.5f, 2f)));
            CompactOpacity = config.Bind("Compact", "CompactOpacity", 0.9f,
                new ConfigDescription("How solid the backing of the compact window is. It fades towards the bottom from this value.",
                    new AcceptableValueRange<float>(0.1f, 1f)));
            CompactCaption = config.Bind("Compact", "CompactCaption", true,
                "Show the dim caption line (\"damage - 30 s\") above the rows.");
            CompactShare = config.Bind("Compact", "CompactShare", true,
                "Show each player's share of the total, in percent, next to their damage.");
            CompactBars = config.Bind("Compact", "CompactBars", true,
                "Draw the bar under each row, split into the damage kinds it is made of.");

            DetailKey = config.Bind("Detail", "DetailKey", new KeyboardShortcut(KeyCode.D, KeyCode.LeftControl),
                "Opens and closes the detail window. Modifiers are allowed; set it to \"None\" to disable the window.");
            DetailSize = config.Bind("Detail", "DetailSize", new Vector2(660f, 480f),
                "Width and height of the detail window in UI pixels.");
            DetailWindows = config.Bind("Detail", "DetailWindows", DefaultWindows,
                "The windows the tabs of the detail view offer, in seconds, comma-separated. Anything longer than HistoryMinutes is capped to it.");

            ShareDamage = Sync.Bind("Sharing", "ShareDamage", true,
                "Send what this client sees to the other players who have the mod, so everyone's meter is complete. With this off you still see your own damage and everything your own client is host of.");
            ShareRadius = Sync.Bind("Sharing", "ShareRadius", 96f,
                new ConfigDescription("Events sent from farther away than this many metres are ignored, so a fight on the other side of the map stays out of your meter.",
                    new AcceptableValueRange<float>(16f, 256f)));
            ShareInterval = config.Bind("Sharing", "ShareInterval", 0.5f,
                new ConfigDescription("Seconds between two packets. Events are collected in between and sent together.",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            Colors = config.Bind("Colours", "Colors", "",
                "Overrides for the damage kind colours, comma-separated, for example: Fire=#ff7733, Poison=#66aa44. Kinds left out keep the muted defaults.");
        }

        /// <summary>The settings a server may decide, and where the current ones come from.</summary>
        public ConfigSync Sync { get; }

        public SyncedEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public ConfigEntry<int> HistoryMinutes { get; }

        public ConfigEntry<bool> CountDamageTaken { get; }

        public ConfigEntry<bool> CountHealing { get; }

        public ConfigEntry<bool> CountObjectDamage { get; }

        public ConfigEntry<bool> CountPets { get; }

        public ConfigEntry<KeyboardShortcut> CompactKey { get; }

        public ConfigEntry<MeterMode> CompactMode { get; }

        public ConfigEntry<int> CompactWindow { get; }

        public ConfigEntry<int> CompactRows { get; }

        public ConfigEntry<float> CompactHideAfter { get; }

        public ConfigEntry<ScreenAnchor> CompactAnchor { get; }

        public ConfigEntry<Vector2> CompactOffset { get; }

        public ConfigEntry<float> CompactWidth { get; }

        public ConfigEntry<float> CompactScale { get; }

        public ConfigEntry<float> CompactOpacity { get; }

        public ConfigEntry<bool> CompactCaption { get; }

        public ConfigEntry<bool> CompactShare { get; }

        public ConfigEntry<bool> CompactBars { get; }

        public ConfigEntry<KeyboardShortcut> DetailKey { get; }

        public ConfigEntry<Vector2> DetailSize { get; }

        public ConfigEntry<string> DetailWindows { get; }

        public SyncedEntry<bool> ShareDamage { get; }

        public SyncedEntry<float> ShareRadius { get; }

        public ConfigEntry<float> ShareInterval { get; }

        public ConfigEntry<string> Colors { get; }

        /// <summary>Seconds the ring of buckets covers.</summary>
        public int HistorySeconds => Math.Max(60, HistoryMinutes.Value * 60);

        /// <summary>Re-reads the config file from disk.</summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>
        /// The windows the detail view offers, in seconds: parsed from the setting, sorted, capped
        /// to what the ring actually holds, and never empty.
        /// </summary>
        public List<int> ParseWindows()
        {
            var windows = new List<int>();
            foreach (string part in (DetailWindows.Value ?? string.Empty).Split(','))
            {
                string text = part.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) || seconds < 1)
                {
                    Plugin.Log.LogWarning($"DetailWindows: '{text}' is not a number of seconds, ignoring it.");
                    continue;
                }

                int capped = Math.Min(seconds, HistorySeconds);
                if (!windows.Contains(capped))
                {
                    windows.Add(capped);
                }
            }

            if (windows.Count == 0)
            {
                windows.Add(Math.Min(30, HistorySeconds));
                windows.Add(HistorySeconds);
            }

            windows.Sort();
            return windows;
        }

        /// <summary>
        /// The colour of each damage kind: the muted default, with whatever the player overrode in
        /// the <c>Colors</c> setting parsed over it. Unreadable entries are logged and skipped.
        /// </summary>
        public Color[] ParseColors()
        {
            var colors = new Color[DamageKinds.Count];
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = ParseColor(DamageKinds.ColorHex(DamageKinds.All[index]), Color.white);
            }

            foreach (string part in (Colors.Value ?? string.Empty).Split(','))
            {
                string text = part.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                int equals = text.IndexOf('=');
                if (equals <= 0)
                {
                    Plugin.Log.LogWarning($"Colors: '{text}' is not a \"Kind=#rrggbb\" pair, ignoring it.");
                    continue;
                }

                string kindName = text.Substring(0, equals).Trim();
                string hex = text.Substring(equals + 1).Trim();

                if (!Enum.TryParse(kindName, ignoreCase: true, out DamageKind kind))
                {
                    Plugin.Log.LogWarning($"Colors: '{kindName}' is not a damage kind, ignoring it.");
                    continue;
                }

                if (!ColorUtility.TryParseHtmlString(hex.StartsWith("#", StringComparison.Ordinal) ? hex : "#" + hex, out Color color))
                {
                    Plugin.Log.LogWarning($"Colors: '{hex}' is not a colour, ignoring it.");
                    continue;
                }

                colors[(int)kind] = color;
            }

            return colors;
        }

        /// <summary>One line with the active settings, for the log.</summary>
        public string Describe()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            var extras = new List<string>();
            if (CountDamageTaken.Value)
            {
                extras.Add("damage taken");
            }

            if (CountHealing.Value)
            {
                extras.Add("healing");
            }

            if (CountObjectDamage.Value)
            {
                extras.Add("objects");
            }

            if (CountPets.Value)
            {
                extras.Add("pets");
            }

            return $"{(Enabled.Value ? "enabled" : "disabled")}, {HistoryMinutes.Value} min of history, " +
                   $"compact window {CompactMode.Value} over {CompactWindow.Value} s ({CompactKey.Value}), " +
                   $"detail window {DetailKey.Value}, " +
                   $"sharing {(ShareDamage.Value ? "on" : "off")} within {ShareRadius.Value.ToString("0.#", culture)} m, " +
                   $"also counting: {(extras.Count > 0 ? string.Join(", ", extras.ToArray()) : "nothing")}, {Sync.Describe()}";
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : fallback;
        }
    }
}
