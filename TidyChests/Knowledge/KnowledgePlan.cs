using System;
using System.Collections.Generic;

namespace TidyChests.Knowledge
{
    /// <summary>
    /// Which items found in nearby chests are worth teaching the player next. Pure: no game
    /// types, so it is covered by unit tests.
    /// </summary>
    public static class KnowledgePlan
    {
        /// <summary>
        /// The names among <paramref name="candidates"/> that the player does not know yet and
        /// that are not in <paramref name="skip"/> (already queued, or failed before), at most
        /// <paramref name="max"/> of them. Sorted, so the same chests always produce the same
        /// order of unlock messages.
        /// </summary>
        public static List<string> NextBatch(
            IEnumerable<string> candidates,
            ICollection<string> known,
            ICollection<string> skip,
            int max)
        {
            var batch = new List<string>();
            if (candidates == null || max <= 0)
            {
                return batch;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in candidates)
            {
                if (string.IsNullOrEmpty(name)
                    || (known != null && known.Contains(name))
                    || (skip != null && skip.Contains(name))
                    || !seen.Add(name))
                {
                    continue;
                }

                batch.Add(name);
            }

            batch.Sort(StringComparer.Ordinal);
            if (batch.Count > max)
            {
                batch.RemoveRange(max, batch.Count - max);
            }

            return batch;
        }
    }
}
