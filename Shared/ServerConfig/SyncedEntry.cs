using System;
using BepInEx.Configuration;

namespace Muindor.ServerConfig
{
    /// <summary>A synced setting seen without its type parameter, for <see cref="ConfigSync"/>.</summary>
    public interface ISyncedEntry
    {
        /// <summary>The setting's address in the config file: <c>Section.Key</c>.</summary>
        string Key { get; }

        /// <summary>This machine's own value, written the way the config file writes it.</summary>
        string LocalAsString();

        /// <summary>Takes a value from the server. Returns false with a reason when it cannot be read.</summary>
        bool ApplyServerValue(string text, out string error);

        /// <summary>Forgets the server's value, so the local one is in charge again.</summary>
        void ClearServerValue();
    }

    /// <summary>
    /// A setting that the server may decide instead of the player. <see cref="Value"/> is the one
    /// the mod runs on; <see cref="LocalValue"/> is always this machine's own, from its config file.
    ///
    /// The server's value is never written to disk, so it disappears with the connection and the
    /// player's own config file is untouched.
    /// </summary>
    public sealed class SyncedEntry<T> : ISyncedEntry
    {
        private T _serverValue = default!;

        private bool _hasServerValue;

        internal SyncedEntry(ConfigEntry<T> local)
        {
            Local = local;
            Key = $"{local.Definition.Section}.{local.Definition.Key}";
        }

        /// <summary>The entry in this machine's config file.</summary>
        public ConfigEntry<T> Local { get; }

        public string Key { get; }

        /// <summary>The value the mod runs on: the server's while it is in charge, this machine's otherwise.</summary>
        public T Value => _hasServerValue ? _serverValue : Local.Value;

        /// <summary>This machine's own value. Setting it writes the config file, as any other setting does.</summary>
        public T LocalValue
        {
            get => Local.Value;
            set => Local.Value = value;
        }

        /// <summary>True while the server decides this value, so changing the local one has no effect right now.</summary>
        public bool IsLocked => _hasServerValue;

        public string LocalAsString()
        {
            return TomlTypeConverter.ConvertToString(Local.Value!, typeof(T));
        }

        public bool ApplyServerValue(string text, out string error)
        {
            object value;
            try
            {
                value = TomlTypeConverter.ConvertToValue(text, typeof(T));
            }
            catch (Exception exception)
            {
                error = $"\"{text}\" is not a valid {typeof(T).Name} ({exception.Message})";
                return false;
            }

            // A server with a different version may send a value outside this version's range;
            // clamping keeps the mod running on the nearest value it accepts.
            AcceptableValueBase? range = Local.Description?.AcceptableValues;
            if (range != null && !range.IsValid(value))
            {
                value = range.Clamp(value);
            }

            _serverValue = (T)value;
            _hasServerValue = true;
            error = string.Empty;
            return true;
        }

        public void ClearServerValue()
        {
            _hasServerValue = false;
            _serverValue = default!;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }
}
