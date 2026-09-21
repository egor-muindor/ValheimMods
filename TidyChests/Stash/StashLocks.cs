using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TidyChests.Stash
{
    /// <summary>
    /// The player's locks: item names and inventory slots the Stash button must leave alone.
    /// Both lists come from the config file and are written back as they are toggled in the
    /// inventory. Pure: no game types, so it is unit-tested.
    /// </summary>
    public sealed class StashLocks
    {
        /// <summary>One inventory slot: column and row, the hotbar being row 0.</summary>
        public readonly struct SlotRef : IEquatable<SlotRef>
        {
            public SlotRef(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }

            public int Y { get; }

            public bool Equals(SlotRef other) => X == other.X && Y == other.Y;

            public override bool Equals(object? obj) => obj is SlotRef other && Equals(other);

            public override int GetHashCode() => X * 397 ^ Y;

            public override string ToString() => FormatSlot(X, Y);
        }

        private readonly List<string> _items = new List<string>();

        private readonly HashSet<string> _itemSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SlotRef> _slots = new List<SlotRef>();

        private readonly HashSet<SlotRef> _slotSet = new HashSet<SlotRef>();

        /// <summary>
        /// Reads the two config lines: item names separated by commas, slots as <c>x:y</c> pairs
        /// separated by commas. Blanks, repeats and malformed slots are dropped, the order is kept.
        /// </summary>
        public static StashLocks Parse(string? items, string? slots)
        {
            var locks = new StashLocks();
            foreach (string part in Split(items))
            {
                locks.LockItem(part);
            }

            foreach (string part in Split(slots))
            {
                if (TryParseSlot(part, out int x, out int y))
                {
                    locks.LockSlot(x, y);
                }
            }

            return locks;
        }

        /// <summary>The locked item names, oldest first.</summary>
        public IReadOnlyList<string> Items => _items;

        /// <summary>The locked slots, oldest first.</summary>
        public IReadOnlyList<SlotRef> Slots => _slots;

        public bool IsItemLocked(string? name)
        {
            return !string.IsNullOrEmpty(name) && _itemSet.Contains(Normalize(name!));
        }

        public bool IsSlotLocked(int x, int y)
        {
            return _slotSet.Contains(new SlotRef(x, y));
        }

        /// <summary>Locks an item. False when it was already locked or the name is empty.</summary>
        public bool LockItem(string? name)
        {
            string normalized = Normalize(name);
            if (normalized.Length == 0 || !_itemSet.Add(normalized))
            {
                return false;
            }

            _items.Add(normalized);
            return true;
        }

        /// <summary>Unlocks an item. False when it was not locked.</summary>
        public bool UnlockItem(string? name)
        {
            string normalized = Normalize(name);
            if (normalized.Length == 0 || !_itemSet.Remove(normalized))
            {
                return false;
            }

            _items.RemoveAll(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        /// <summary>Locks the item when it is unlocked and the other way round. Returns the new state.</summary>
        public bool ToggleItem(string? name)
        {
            if (IsItemLocked(name))
            {
                UnlockItem(name);
                return false;
            }

            return LockItem(name);
        }

        /// <summary>Locks a slot. False when it was already locked or the position is negative.</summary>
        public bool LockSlot(int x, int y)
        {
            if (x < 0 || y < 0 || !_slotSet.Add(new SlotRef(x, y)))
            {
                return false;
            }

            _slots.Add(new SlotRef(x, y));
            return true;
        }

        /// <summary>Unlocks a slot. False when it was not locked.</summary>
        public bool UnlockSlot(int x, int y)
        {
            if (!_slotSet.Remove(new SlotRef(x, y)))
            {
                return false;
            }

            _slots.Remove(new SlotRef(x, y));
            return true;
        }

        /// <summary>Locks the slot when it is unlocked and the other way round. Returns the new state.</summary>
        public bool ToggleSlot(int x, int y)
        {
            if (IsSlotLocked(x, y))
            {
                UnlockSlot(x, y);
                return false;
            }

            return LockSlot(x, y);
        }

        /// <summary>The item names as one config line.</summary>
        public string FormatItems()
        {
            return string.Join(", ", _items);
        }

        /// <summary>The slots as one config line: <c>x:y, x:y</c>.</summary>
        public string FormatSlots()
        {
            return string.Join(", ", _slots.Select(slot => FormatSlot(slot.X, slot.Y)));
        }

        public static string FormatSlot(int x, int y)
        {
            return x.ToString(CultureInfo.InvariantCulture) + ":" + y.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>One line for the log: how many of each.</summary>
        public string Describe()
        {
            return $"{_items.Count} locked items, {_slots.Count} locked slots";
        }

        private static bool TryParseSlot(string text, out int x, out int y)
        {
            x = y = 0;
            string[] parts = text.Split(':');
            return parts.Length == 2
                   && int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out x)
                   && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out y)
                   && x >= 0 && y >= 0;
        }

        private static IEnumerable<string> Split(string? list)
        {
            if (string.IsNullOrWhiteSpace(list))
            {
                yield break;
            }

            foreach (string entry in list!.Split(',', ';'))
            {
                string trimmed = entry.Trim();
                if (trimmed.Length > 0)
                {
                    yield return trimmed;
                }
            }
        }

        private static string Normalize(string? name)
        {
            return (name ?? "").Trim();
        }
    }
}
