using System;
using System.Collections.Generic;

namespace TidyChests.Index
{
    /// <summary>
    /// Turns the contents of the containers in range into the rows of the browser: one row per
    /// item kind with the total count, and a name filter over those rows. Pure: no game types,
    /// so it is covered by unit tests.
    /// </summary>
    public static class ItemSearch
    {
        /// <summary>One row per item kind, sorted by display name.</summary>
        public static List<ItemTotal> Summarize(IReadOnlyList<ChestContents> chests)
        {
            var counts = new Dictionary<string, int>();
            var chestCounts = new Dictionary<string, int>();
            var lastChest = new Dictionary<string, int>();
            var distances = new Dictionary<string, float>();
            var displayNames = new Dictionary<string, string>();
            var order = new List<string>();

            for (int position = 0; position < chests.Count; position++)
            {
                ChestContents chest = chests[position];
                foreach (ChestItem item in chest.Items)
                {
                    if (string.IsNullOrEmpty(item.Name) || item.Count <= 0)
                    {
                        continue;
                    }

                    if (!counts.ContainsKey(item.Name))
                    {
                        order.Add(item.Name);
                        counts[item.Name] = 0;
                        chestCounts[item.Name] = 0;
                        lastChest[item.Name] = -1;
                        distances[item.Name] = chest.Distance;
                        displayNames[item.Name] = item.DisplayName;
                    }

                    counts[item.Name] += item.Count;

                    // Two stacks of the same item in one chest are still one chest.
                    if (lastChest[item.Name] != position)
                    {
                        lastChest[item.Name] = position;
                        chestCounts[item.Name]++;
                    }

                    if (chest.Distance < distances[item.Name])
                    {
                        distances[item.Name] = chest.Distance;
                    }
                }
            }

            var totals = new List<ItemTotal>(order.Count);
            foreach (string name in order)
            {
                totals.Add(new ItemTotal(name, displayNames[name], counts[name], chestCounts[name], distances[name]));
            }

            totals.Sort(CompareByDisplayName);
            return totals;
        }

        /// <summary>
        /// The rows that match <paramref name="query"/>: names that start with it first, then
        /// names that contain it, then rows matched only through their raw token. An empty
        /// query keeps every row. The comparison ignores case and surrounding whitespace.
        /// </summary>
        public static List<ItemTotal> Filter(IReadOnlyList<ItemTotal> totals, string query)
        {
            string trimmed = (query ?? "").Trim();
            var result = new List<ItemTotal>(totals.Count);
            if (trimmed.Length == 0)
            {
                result.AddRange(totals);
                return result;
            }

            var ranks = new Dictionary<ItemTotal, int>();
            foreach (ItemTotal total in totals)
            {
                int rank = Rank(total, trimmed);
                if (rank >= 0)
                {
                    ranks[total] = rank;
                    result.Add(total);
                }
            }

            result.Sort((a, b) =>
            {
                int byRank = ranks[a].CompareTo(ranks[b]);
                return byRank != 0 ? byRank : CompareByDisplayName(a, b);
            });

            return result;
        }

        /// <summary>True when the row is kept for <paramref name="query"/>.</summary>
        public static bool Matches(ItemTotal total, string query)
        {
            return Rank(total, (query ?? "").Trim()) >= 0;
        }

        /// <summary>0 for a name that starts with the query, 1 for a name that contains it, 2 for a token match, -1 for no match.</summary>
        private static int Rank(ItemTotal total, string query)
        {
            if (query.Length == 0)
            {
                return 0;
            }

            string displayName = total.DisplayName ?? "";
            if (displayName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (displayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1;
            }

            if ((total.Name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 2;
            }

            return -1;
        }

        private static int CompareByDisplayName(ItemTotal a, ItemTotal b)
        {
            int byName = string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase);
            return byName != 0 ? byName : string.CompareOrdinal(a.Name, b.Name);
        }
    }
}
