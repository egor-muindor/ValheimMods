using System;
using System.Collections.Generic;

namespace OreFinder.Detection
{
    /// <summary>
    /// The player's own names for the ores, from the <c>Names</c> setting: pairs such as
    /// <c>CopperOre=C, TinOre=T</c>. Keys are ore item names or object prefab names, matched
    /// like the <c>Ores</c> list (case-insensitive, <c>$item_</c> keys accepted). Pure string
    /// logic, no game dependency.
    /// </summary>
    public sealed class OreNames
    {
        private static readonly char[] EntrySeparators = { ',', ';', '\n', '\r' };

        private static readonly char[] PairSeparators = { '=', ':' };

        private readonly Dictionary<string, string> _names;

        private OreNames(Dictionary<string, string> names)
        {
            _names = names;
        }

        /// <summary>True when no pair was configured.</summary>
        public bool IsEmpty => _names.Count == 0;

        /// <summary>Number of configured names.</summary>
        public int Count => _names.Count;

        /// <summary>
        /// Parses <c>key=name</c> pairs separated by commas, semicolons or line breaks. A colon
        /// works instead of the equals sign. Entries without a key or a name are ignored; a
        /// key given twice keeps its last name.
        /// </summary>
        public static OreNames Parse(string? list)
        {
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(list))
            {
                foreach (string entry in list!.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries))
                {
                    int separator = entry.IndexOfAny(PairSeparators);
                    if (separator < 0)
                    {
                        continue;
                    }

                    string key = OreFilter.Normalize(entry.Substring(0, separator));
                    string name = entry.Substring(separator + 1).Trim();
                    if (key.Length > 0 && name.Length > 0)
                    {
                        names[key] = name;
                    }
                }
            }

            return new OreNames(names);
        }

        /// <summary>The name for an ore object: by its ore item first, then by its own prefab name.</summary>
        public bool TryGet(string oreItem, string objectPrefabName, out string name)
        {
            if (_names.TryGetValue(OreFilter.Normalize(oreItem), out name))
            {
                return true;
            }

            if (_names.TryGetValue(OreFilter.Normalize(objectPrefabName), out name))
            {
                return true;
            }

            name = string.Empty;
            return false;
        }

        /// <summary>The configured pairs, for the log.</summary>
        public string Describe()
        {
            if (_names.Count == 0)
            {
                return "none";
            }

            var pairs = new List<string>(_names.Count);
            foreach (KeyValuePair<string, string> pair in _names)
            {
                pairs.Add($"{pair.Key}={pair.Value}");
            }

            return string.Join(", ", pairs);
        }
    }
}
