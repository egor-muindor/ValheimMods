using System;
using System.Collections.Generic;

namespace TidyChests.Index
{
    /// <summary>
    /// The items the player pinned to the top of the chest list, in the order they were pinned.
    ///
    /// Kept as shared item names (<c>$item_wood</c>) rather than as the names on screen: that is
    /// what the index keys its rows by, and it survives a language change. The list is stored in
    /// the config file as one comma-separated line, so it can also be written by hand.
    ///
    /// Pure: no game types, so it is covered by unit tests.
    /// </summary>
    public sealed class FavoriteList
    {
        private readonly List<string> _names = new List<string>();

        private readonly HashSet<string> _set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Reads the config line. Blanks and repeats are dropped, the order is kept.</summary>
        public static FavoriteList Parse(string? text)
        {
            var list = new FavoriteList();
            if (string.IsNullOrEmpty(text))
            {
                return list;
            }

            foreach (string part in text!.Split(','))
            {
                list.Add(part.Trim());
            }

            return list;
        }

        public int Count => _names.Count;

        /// <summary>The pinned names, oldest first.</summary>
        public IReadOnlyList<string> Names => _names;

        public bool Contains(string? name)
        {
            return !string.IsNullOrEmpty(name) && _set.Contains(name!);
        }

        /// <summary>Pins an item. False when it was already pinned or the name is empty.</summary>
        public bool Add(string? name)
        {
            if (string.IsNullOrEmpty(name) || !_set.Add(name!))
            {
                return false;
            }

            _names.Add(name!);
            return true;
        }

        /// <summary>Unpins an item. False when it was not pinned.</summary>
        public bool Remove(string? name)
        {
            if (string.IsNullOrEmpty(name) || !_set.Remove(name!))
            {
                return false;
            }

            // Removal goes through the set's comparer, so the stored spelling may differ in case.
            for (int i = 0; i < _names.Count; i++)
            {
                if (string.Equals(_names[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    _names.RemoveAt(i);
                    break;
                }
            }

            return true;
        }

        /// <summary>Pins an item that was not pinned, unpins one that was. Returns the new state.</summary>
        public bool Toggle(string? name)
        {
            if (Contains(name))
            {
                Remove(name);
                return false;
            }

            return Add(name);
        }

        /// <summary>The config line to save.</summary>
        public string Format()
        {
            return string.Join(", ", _names.ToArray());
        }
    }
}
