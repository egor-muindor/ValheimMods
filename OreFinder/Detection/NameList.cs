using System;
using System.Collections.Generic;

namespace OreFinder.Detection
{
    /// <summary>
    /// A plain list of names from the config (<c>Pickables</c>, <c>Trees</c>): comma, semicolon
    /// or whitespace separated, matched case-insensitively with <c>$item_</c> keys accepted.
    /// Pure string logic, no game dependency.
    /// </summary>
    public sealed class NameList
    {
        private static readonly char[] Separators = { ',', ';', '\n', '\r', '\t', ' ' };

        private readonly HashSet<string> _names;

        private NameList(HashSet<string> names)
        {
            _names = names;
        }

        public bool IsEmpty => _names.Count == 0;

        public int Count => _names.Count;

        public static NameList Parse(string? list)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(list))
            {
                foreach (string raw in list!.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
                {
                    string name = OreFilter.Normalize(raw);
                    if (name.Length > 0)
                    {
                        names.Add(name);
                    }
                }
            }

            return new NameList(names);
        }

        public bool Contains(string name)
        {
            return name.Length > 0 && _names.Contains(OreFilter.Normalize(name));
        }

        public string Describe()
        {
            return _names.Count == 0 ? "none" : string.Join(", ", _names);
        }
    }
}
