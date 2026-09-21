using System;
using System.Collections.Generic;
using System.Linq;

namespace TidyChests.Stash
{
    /// <summary>
    /// Decides which inventory items may leave the inventory. Pure: no game types, so it is
    /// unit-tested. Built once per stash from the config values.
    /// </summary>
    public sealed class StashRules
    {
        private readonly bool _includeHotbar;

        private readonly HashSet<string> _types;

        private readonly HashSet<string> _blacklist;

        private readonly StashLocks _locks;

        private StashRules(bool includeHotbar, HashSet<string> types, HashSet<string> blacklist, StashLocks locks)
        {
            _includeHotbar = includeHotbar;
            _types = types;
            _blacklist = blacklist;
            _locks = locks;
        }

        /// <summary>Item types that may be stashed, sorted, as configured (unknown names dropped).</summary>
        public IReadOnlyList<string> ItemTypes => _types.OrderBy(type => type, StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>Blacklisted names, normalised (no leading <c>$</c>), sorted.</summary>
        public IReadOnlyList<string> Blacklist => _blacklist.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>
        /// Parses the settings. <paramref name="knownTypeNames"/> are the valid item type names;
        /// entries that are not among them are reported through <paramref name="warn"/> and ignored.
        /// </summary>
        public static StashRules Parse(StashRuleSettings settings, IEnumerable<string> knownTypeNames, Action<string>? warn = null)
        {
            var known = new HashSet<string>(knownTypeNames, StringComparer.OrdinalIgnoreCase);
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string entry in Split(settings.ItemTypes))
            {
                if (known.Contains(entry))
                {
                    types.Add(entry);
                }
                else
                {
                    warn?.Invoke($"Unknown item type '{entry}' in ItemTypes is ignored. Valid types: {string.Join(", ", knownTypeNames)}");
                }
            }

            var blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string entry in Split(settings.Blacklist))
            {
                blacklist.Add(Normalize(entry));
            }

            return new StashRules(settings.IncludeHotbar, types, blacklist, StashLocks.Parse(settings.LockedItems, settings.LockedSlots));
        }

        /// <summary>The first reason that keeps the item in the inventory, or <see cref="StashVerdict.Stash"/>.</summary>
        public StashVerdict Judge(ItemFacts facts)
        {
            if (facts.MaxStack <= 1)
            {
                return StashVerdict.NotStackable;
            }

            if (facts.QuestItem)
            {
                return StashVerdict.QuestItem;
            }

            if (facts.Equipped)
            {
                return StashVerdict.Equipped;
            }

            if (facts.ModSlot)
            {
                return StashVerdict.ModSlot;
            }

            if (facts.Hotbar && !_includeHotbar)
            {
                return StashVerdict.Hotbar;
            }

            if (!_types.Contains(facts.TypeName))
            {
                return StashVerdict.TypeExcluded;
            }

            if (IsBlacklisted(facts.PrefabName) || IsBlacklisted(facts.SharedName))
            {
                return StashVerdict.Blacklisted;
            }

            if (_locks.IsSlotLocked(facts.GridX, facts.GridY))
            {
                return StashVerdict.LockedSlot;
            }

            if (_locks.IsItemLocked(facts.SharedName))
            {
                return StashVerdict.LockedItem;
            }

            return StashVerdict.Stash;
        }

        /// <summary>One line with the active rules, for the log.</summary>
        public string Describe()
        {
            string types = _types.Count == 0 ? "none" : string.Join(", ", ItemTypes);
            string blacklist = _blacklist.Count == 0 ? "empty" : string.Join(", ", Blacklist);
            return $"types: {types}; blacklist: {blacklist}; {_locks.Describe()}";
        }

        private bool IsBlacklisted(string name)
        {
            string normalized = Normalize(name);
            return normalized.Length > 0 && _blacklist.Contains(normalized);
        }

        private static IEnumerable<string> Split(string list)
        {
            if (string.IsNullOrWhiteSpace(list))
            {
                yield break;
            }

            foreach (string entry in list.Split(',', ';'))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length > 0)
                {
                    yield return trimmed;
                }
            }
        }

        private static string Normalize(string name)
        {
            return (name ?? "").Trim().TrimStart('$');
        }
    }
}
