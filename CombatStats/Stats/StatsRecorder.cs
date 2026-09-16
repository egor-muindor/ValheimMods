using System;
using System.Collections.Generic;
using CombatStats.Model;

namespace CombatStats.Stats
{
    /// <summary>
    /// Every event of one channel, kept in a ring of one-second buckets.
    ///
    /// A bucket knows which second it holds, so the ring never has to be swept: when a second
    /// comes round again the old contents are dropped on the first write. Entries are pooled,
    /// which is what keeps a long fight from allocating.
    /// </summary>
    public sealed class StatsRecorder
    {
        private readonly Bucket[] _buckets;

        private readonly Stack<Entry> _pool = new Stack<Entry>();

        private readonly int _seconds;

        public StatsRecorder(int seconds)
        {
            if (seconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "A ring holds at least one second.");
            }

            _seconds = seconds;
            _buckets = new Bucket[seconds];
            for (int index = 0; index < seconds; index++)
            {
                _buckets[index] = new Bucket();
            }

            LastEventTime = double.NegativeInfinity;
        }

        /// <summary>When the last event was recorded, in the same clock <c>Record</c> is called with.</summary>
        public double LastEventTime { get; private set; }

        /// <summary>Seconds the ring covers.</summary>
        public int Seconds => _seconds;

        /// <summary>
        /// Records one event. <paramref name="byKind"/> is read, never kept, so the caller may
        /// reuse the same array for every event.
        /// </summary>
        public void Record(double now, long combatantId, float[] byKind, bool estimated)
        {
            if (byKind == null || byKind.Length != DamageKinds.Count)
            {
                throw new ArgumentException($"A hit carries {DamageKinds.Count} values.", nameof(byKind));
            }

            float total = 0f;
            for (int kind = 0; kind < DamageKinds.Count; kind++)
            {
                total += byKind[kind];
            }

            if (total <= 0f)
            {
                return;
            }

            Bucket bucket = BucketFor(now, out long second);
            if (bucket.Second != second)
            {
                Reset(bucket, second);
            }

            if (!bucket.Entries.TryGetValue(combatantId, out Entry? entry) || entry == null)
            {
                entry = _pool.Count > 0 ? _pool.Pop() : new Entry();
                entry.Reset();
                bucket.Entries[combatantId] = entry;
            }

            for (int kind = 0; kind < DamageKinds.Count; kind++)
            {
                entry.ByKind[kind] += byKind[kind];
            }

            entry.Total += total;
            entry.Hits++;
            entry.Estimated |= estimated;
            if (total > entry.Max)
            {
                entry.Max = total;
            }

            if (now > LastEventTime)
            {
                LastEventTime = now;
            }
        }

        /// <summary>
        /// Adds up the last <paramref name="windowSeconds"/> seconds. A window longer than the
        /// ring is capped at the ring, and the snapshot says which length it really used.
        /// </summary>
        public WindowSnapshot Snapshot(double now, int windowSeconds, Func<long, string> nameOf)
        {
            int window = Math.Max(1, Math.Min(windowSeconds, _seconds));
            long last = (long)Math.Floor(now);
            long first = last - window + 1;

            var totals = new Dictionary<long, Entry>();
            var totalByKind = new float[DamageKinds.Count];
            float total = 0f;
            int hits = 0;
            float max = 0f;

            for (long second = first; second <= last; second++)
            {
                Bucket bucket = _buckets[IndexOf(second)];
                if (bucket.Second != second)
                {
                    continue;
                }

                foreach (KeyValuePair<long, Entry> pair in bucket.Entries)
                {
                    if (!totals.TryGetValue(pair.Key, out Entry? sum) || sum == null)
                    {
                        sum = new Entry();
                        sum.Reset();
                        totals[pair.Key] = sum;
                    }

                    Entry source = pair.Value;
                    for (int kind = 0; kind < DamageKinds.Count; kind++)
                    {
                        sum.ByKind[kind] += source.ByKind[kind];
                        totalByKind[kind] += source.ByKind[kind];
                    }

                    sum.Total += source.Total;
                    sum.Hits += source.Hits;
                    sum.Estimated |= source.Estimated;
                    if (source.Max > sum.Max)
                    {
                        sum.Max = source.Max;
                    }

                    total += source.Total;
                    hits += source.Hits;
                    if (source.Max > max)
                    {
                        max = source.Max;
                    }
                }
            }

            var rows = new List<CombatantRow>(totals.Count);
            foreach (KeyValuePair<long, Entry> pair in totals)
            {
                Entry sum = pair.Value;
                rows.Add(new CombatantRow(
                    pair.Key,
                    nameOf != null ? nameOf(pair.Key) : string.Empty,
                    sum.Total,
                    sum.Hits,
                    sum.Max,
                    total > 0f ? sum.Total / total : 0f,
                    sum.ByKind,
                    sum.Estimated));
            }

            rows.Sort(ByDamage);
            return new WindowSnapshot(window, total, hits, max, totalByKind, rows);
        }

        /// <summary>Drops everything recorded so far; used when a session ends.</summary>
        public void Clear()
        {
            foreach (Bucket bucket in _buckets)
            {
                Reset(bucket, long.MinValue);
            }

            LastEventTime = double.NegativeInfinity;
        }

        private static int ByDamage(CombatantRow left, CombatantRow right)
        {
            int byTotal = right.Total.CompareTo(left.Total);
            return byTotal != 0 ? byTotal : left.Id.CompareTo(right.Id);
        }

        private Bucket BucketFor(double now, out long second)
        {
            second = (long)Math.Floor(now);
            return _buckets[IndexOf(second)];
        }

        private int IndexOf(long second)
        {
            return (int)(((second % _seconds) + _seconds) % _seconds);
        }

        private void Reset(Bucket bucket, long second)
        {
            foreach (KeyValuePair<long, Entry> pair in bucket.Entries)
            {
                _pool.Push(pair.Value);
            }

            bucket.Entries.Clear();
            bucket.Second = second;
        }

        /// <summary>One second of events, for however many combatants were seen in it.</summary>
        private sealed class Bucket
        {
            public long Second = long.MinValue;

            public readonly Dictionary<long, Entry> Entries = new Dictionary<long, Entry>();
        }

        /// <summary>One combatant's share of a bucket, or of a window while it is being summed.</summary>
        private sealed class Entry
        {
            public readonly float[] ByKind = new float[DamageKinds.Count];

            public float Total;

            public int Hits;

            public float Max;

            public bool Estimated;

            public void Reset()
            {
                Array.Clear(ByKind, 0, ByKind.Length);
                Total = 0f;
                Hits = 0;
                Max = 0f;
                Estimated = false;
            }
        }
    }
}
