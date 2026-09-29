using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TidyChests.Stash;

namespace TidyChests.Restock
{
    /// <summary>
    /// The inventory slots marked for restocking: how full the Restock button keeps each one, in
    /// percent of a stack, and the item it was last seen holding, so an emptied slot is refilled
    /// with the same thing. Read from the config line and written back as the marks are toggled.
    /// Pure: no game types, so it is unit-tested.
    /// </summary>
    public sealed class RestockMarks
    {
        /// <summary>One marked slot.</summary>
        public sealed class Mark
        {
            public Mark(StashLocks.SlotRef slot, int percent, string item)
            {
                Slot = slot;
                Percent = percent;
                Item = item;
            }

            public StashLocks.SlotRef Slot { get; }

            /// <summary>How full the slot is kept, 1 to 100 percent of a stack.</summary>
            public int Percent { get; set; }

            /// <summary>The item last seen in the slot (<c>$item_arrow_wood</c>), empty when it has always been empty.</summary>
            public string Item { get; set; }
        }

        public const int FullPercent = 100;

        private readonly List<Mark> _marks = new List<Mark>();

        /// <summary>
        /// Reads the config line: comma-separated <c>x:y=percent</c> entries, optionally followed
        /// by <c>:item</c>. Malformed entries, repeated slots and percentages outside 1..100 are dropped.
        /// </summary>
        public static RestockMarks Parse(string? line)
        {
            var marks = new RestockMarks();
            if (string.IsNullOrWhiteSpace(line))
            {
                return marks;
            }

            foreach (string entry in line!.Split(',', ';'))
            {
                string trimmed = entry.Trim();
                int equals = trimmed.IndexOf('=');
                if (equals <= 0 || !StashLocks.TryParseSlot(trimmed.Substring(0, equals), out int x, out int y))
                {
                    continue;
                }

                string rest = trimmed.Substring(equals + 1);
                int colon = rest.IndexOf(':');
                string percentText = colon < 0 ? rest : rest.Substring(0, colon);
                string item = colon < 0 ? "" : rest.Substring(colon + 1).Trim();
                if (!int.TryParse(percentText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int percent)
                    || percent < 1 || percent > FullPercent
                    || marks.Find(x, y) != null)
                {
                    continue;
                }

                marks._marks.Add(new Mark(new StashLocks.SlotRef(x, y), percent, item));
            }

            return marks;
        }

        /// <summary>The marked slots, oldest first.</summary>
        public IReadOnlyList<Mark> Marks => _marks;

        /// <summary>The mark on a slot, or null.</summary>
        public Mark? Find(int x, int y)
        {
            return _marks.FirstOrDefault(mark => mark.Slot.X == x && mark.Slot.Y == y);
        }

        /// <summary>How full the slot is kept, 0 when it is not marked.</summary>
        public int PercentAt(int x, int y)
        {
            return Find(x, y)?.Percent ?? 0;
        }

        /// <summary>
        /// Steps the slot to its next state: not marked, then a full stack, then
        /// <paramref name="halfPercent"/>, then not marked again. <paramref name="item"/> is what
        /// lies in the slot now, if anything. Returns the new percentage, 0 for not marked.
        /// </summary>
        public int Cycle(int x, int y, int halfPercent, string? item)
        {
            if (x < 0 || y < 0)
            {
                return 0;
            }

            Mark? mark = Find(x, y);
            if (mark == null)
            {
                _marks.Add(new Mark(new StashLocks.SlotRef(x, y), FullPercent, item ?? ""));
                return FullPercent;
            }

            int half = Math.Max(1, Math.Min(FullPercent, halfPercent));
            if (mark.Percent >= FullPercent && half < FullPercent)
            {
                mark.Percent = half;
                Remember(x, y, item);
                return half;
            }

            _marks.Remove(mark);
            return 0;
        }

        /// <summary>Records what lies in a marked slot. True when that changed the mark.</summary>
        public bool Remember(int x, int y, string? item)
        {
            Mark? mark = Find(x, y);
            if (mark == null || string.IsNullOrEmpty(item) || mark.Item == item)
            {
                return false;
            }

            mark.Item = item!;
            return true;
        }

        /// <summary>The marks as one config line: <c>x:y=percent:item, ...</c>.</summary>
        public string Format()
        {
            return string.Join(", ", _marks.Select(mark =>
                StashLocks.FormatSlot(mark.Slot.X, mark.Slot.Y) + "=" + mark.Percent.ToString(CultureInfo.InvariantCulture) +
                (mark.Item.Length > 0 ? ":" + mark.Item : "")));
        }

        /// <summary>One line for the log.</summary>
        public string Describe()
        {
            return $"{_marks.Count} restock slots";
        }
    }
}
