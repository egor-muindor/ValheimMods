using System;
using System.Collections.Generic;
using System.Text;

namespace Muindor.ServerConfig
{
    /// <summary>
    /// What a server sends to a client: the mod version the server runs, whether its configuration
    /// wins over the client's, and the settings themselves as <c>Section.Key</c> to string pairs.
    ///
    /// Plain text on purpose. The format needs neither the game nor BepInEx, so it is unit tested,
    /// and a client whose mod version knows fewer (or more) settings than the server's skips the
    /// keys it does not know instead of failing to read the rest.
    /// </summary>
    public sealed class ConfigPayload
    {
        /// <summary>First line of every payload. The number goes up if the format ever changes.</summary>
        public const string Header = "muindor.config/1";

        public ConfigPayload(string modVersion, bool priority, IEnumerable<KeyValuePair<string, string>>? values = null)
        {
            ModVersion = modVersion ?? string.Empty;
            Priority = priority;
            var list = new List<KeyValuePair<string, string>>();
            if (values != null)
            {
                list.AddRange(values);
            }

            Values = list;
        }

        /// <summary>The mod version the server runs. Shown in the client's log, never acted upon.</summary>
        public string ModVersion { get; }

        /// <summary>True when the server's settings replace the client's.</summary>
        public bool Priority { get; }

        /// <summary>The settings, as <c>Section.Key</c> to the value written the way the config file writes it.</summary>
        public IList<KeyValuePair<string, string>> Values { get; }

        public string Serialize()
        {
            var text = new StringBuilder();
            text.Append(Header).Append('\n');
            text.Append(Escape(ModVersion, escapeEquals: false)).Append('\n');
            text.Append(Priority ? "true" : "false").Append('\n');
            foreach (KeyValuePair<string, string> pair in Values)
            {
                text.Append(Escape(pair.Key, escapeEquals: true))
                    .Append('=')
                    .Append(Escape(pair.Value ?? string.Empty, escapeEquals: false))
                    .Append('\n');
            }

            return text.ToString();
        }

        /// <summary>
        /// Reads a payload. Returns false with a reason for anything that is not one, so a packet
        /// from another mod or from a future format is ignored instead of throwing.
        /// </summary>
        public static bool TryParse(string? text, out ConfigPayload? payload, out string error)
        {
            payload = null;
            if (string.IsNullOrEmpty(text))
            {
                error = "the payload is empty";
                return false;
            }

            string[] lines = text!.Split('\n');
            if (lines.Length < 3)
            {
                error = "the payload is truncated";
                return false;
            }

            if (Trim(lines[0]) != Header)
            {
                error = $"unknown format \"{Trim(lines[0])}\", expected \"{Header}\"";
                return false;
            }

            string modVersion = Unescape(Trim(lines[1]));
            string priorityText = Trim(lines[2]);
            if (priorityText != "true" && priorityText != "false")
            {
                error = $"\"{priorityText}\" is not a valid priority flag";
                return false;
            }

            var values = new List<KeyValuePair<string, string>>();
            for (int i = 3; i < lines.Length; i++)
            {
                string line = Trim(lines[i]);
                if (line.Length == 0)
                {
                    continue;
                }

                int separator = IndexOfSeparator(line);
                if (separator < 0)
                {
                    error = $"line {i + 1} has no key/value separator";
                    return false;
                }

                string key = Unescape(line.Substring(0, separator));
                if (key.Length == 0)
                {
                    error = $"line {i + 1} has an empty key";
                    return false;
                }

                values.Add(new KeyValuePair<string, string>(key, Unescape(line.Substring(separator + 1))));
            }

            payload = new ConfigPayload(modVersion, priorityText == "true", values);
            error = string.Empty;
            return true;
        }

        /// <summary>Drops the carriage return a transport may have added; the payload itself never contains one.</summary>
        private static string Trim(string line)
        {
            return line.EndsWith("\r", StringComparison.Ordinal) ? line.Substring(0, line.Length - 1) : line;
        }

        /// <summary>The first '=' that is not escaped, or -1.</summary>
        private static int IndexOfSeparator(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (line[i] == '=')
                {
                    return i;
                }
            }

            return -1;
        }

        private static string Escape(string value, bool escapeEquals)
        {
            var text = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        text.Append("\\\\");
                        break;
                    case '\n':
                        text.Append("\\n");
                        break;
                    case '\r':
                        text.Append("\\r");
                        break;
                    case '=' when escapeEquals:
                        text.Append("\\=");
                        break;
                    default:
                        text.Append(c);
                        break;
                }
            }

            return text.ToString();
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf('\\') < 0)
            {
                return value;
            }

            var text = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    text.Append(value[i]);
                    continue;
                }

                char next = value[++i];
                switch (next)
                {
                    case 'n':
                        text.Append('\n');
                        break;
                    case 'r':
                        text.Append('\r');
                        break;
                    default:
                        // Covers "\\" and "\=", and keeps anything unknown as it was written.
                        text.Append(next);
                        break;
                }
            }

            return text.ToString();
        }
    }
}
