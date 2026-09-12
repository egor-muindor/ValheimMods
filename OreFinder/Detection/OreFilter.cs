using System;
using System.Collections.Generic;
using System.Text;

namespace OreFinder.Detection
{
    /// <summary>
    /// Decides which mineable objects count as ore. Pure string logic, no game dependency.
    ///
    /// A mineable object is described by its prefab name and the names of the items it drops:
    /// item prefab names such as <c>CopperOre</c> and localisation keys such as
    /// <c>$item_copperore</c>. With an empty list ("all ores") an object is ore when one of its
    /// drops has the word Ore or Scrap in its name. With a list, an object is ore when one of
    /// its drops or its own prefab name is in the list.
    /// </summary>
    public sealed class OreFilter
    {
        private const string ItemKeyPrefix = "$item_";

        private static readonly char[] Separators = { ',', ';', '\n', '\r', '\t', ' ' };

        private static readonly string[] OreWords = { "ore", "scrap" };

        private readonly HashSet<string> _names;

        private OreFilter(HashSet<string> names)
        {
            _names = names;
        }

        /// <summary>True when no list is configured and the built-in ore rule applies.</summary>
        public bool IsAllOres => _names.Count == 0;

        /// <summary>Parses the <c>Ores</c> setting: names separated by commas, semicolons or whitespace.</summary>
        public static OreFilter Parse(string? list)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(list))
            {
                foreach (string raw in list!.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
                {
                    string name = Normalize(raw);
                    if (name.Length > 0)
                    {
                        names.Add(name);
                    }
                }
            }

            return new OreFilter(names);
        }

        /// <summary>
        /// Built-in rule: the name contains the word Ore or Scrap. Words are split at
        /// separators and at lower-to-upper case changes, so <c>CopperOre</c>,
        /// <c>FlametalOreNew</c>, <c>IronScrap</c> and <c>copper_ore</c> are ore, while
        /// <c>LeatherScraps</c>, <c>ShieldCore</c>, <c>Stone</c> and <c>Obsidian</c> are not.
        /// </summary>
        public static bool IsOreItemName(string name)
        {
            foreach (string word in Words(name))
            {
                foreach (string oreWord in OreWords)
                {
                    if (string.Equals(word, oreWord, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tells whether an object is ore. <paramref name="oreItem"/> receives the drop that
        /// made it ore (for the name and colour of the highlight), or the object prefab name
        /// when it was matched by object name and drops nothing that looks like ore.
        /// </summary>
        public bool TryMatch(string objectPrefabName, IReadOnlyList<string> dropItemNames, out string oreItem)
        {
            if (_names.Count == 0)
            {
                return TryFirstOreDrop(dropItemNames, out oreItem);
            }

            for (int i = 0; i < dropItemNames.Count; i++)
            {
                if (_names.Contains(Normalize(dropItemNames[i])))
                {
                    oreItem = dropItemNames[i];
                    return true;
                }
            }

            if (_names.Contains(Normalize(objectPrefabName)))
            {
                if (TryFirstOreDrop(dropItemNames, out oreItem))
                {
                    return true;
                }

                oreItem = dropItemNames.Count > 0 ? dropItemNames[0] : objectPrefabName;
                return true;
            }

            oreItem = string.Empty;
            return false;
        }

        /// <summary>The configured names, or "all ores".</summary>
        public string Describe()
        {
            return IsAllOres ? "all ores" : string.Join(", ", _names);
        }

        /// <summary>Lower-case, trimmed, without the <c>$item_</c> prefix of localisation keys.</summary>
        public static string Normalize(string name)
        {
            string normalized = name.Trim().ToLowerInvariant();
            if (normalized.StartsWith(ItemKeyPrefix, StringComparison.Ordinal))
            {
                normalized = normalized.Substring(ItemKeyPrefix.Length);
            }

            return normalized;
        }

        private static bool TryFirstOreDrop(IReadOnlyList<string> dropItemNames, out string oreItem)
        {
            for (int i = 0; i < dropItemNames.Count; i++)
            {
                if (IsOreItemName(dropItemNames[i]))
                {
                    oreItem = dropItemNames[i];
                    return true;
                }
            }

            oreItem = string.Empty;
            return false;
        }

        private static IEnumerable<string> Words(string name)
        {
            var word = new StringBuilder();
            char previous = '\0';
            foreach (char c in name)
            {
                bool boundary = !char.IsLetterOrDigit(c)
                                || (char.IsUpper(c) && char.IsLower(previous));
                if (boundary && word.Length > 0)
                {
                    yield return word.ToString();
                    word.Clear();
                }

                if (char.IsLetterOrDigit(c))
                {
                    word.Append(c);
                }

                previous = c;
            }

            if (word.Length > 0)
            {
                yield return word.ToString();
            }
        }
    }
}
