using System.Collections.Generic;
using CombatStats.Model;

namespace CombatStats.Stats
{
    /// <summary>One combatant's line in a window.</summary>
    public sealed class CombatantRow
    {
        public CombatantRow(long id, string name, float total, int hits, float max, float share, float[] byKind, bool estimated)
        {
            Id = id;
            Name = name;
            Total = total;
            Hits = hits;
            Max = max;
            Share = share;
            ByKind = byKind;
            Estimated = estimated;
        }

        /// <summary>Stable id of the combatant: the user id half of their character's ZDOID.</summary>
        public long Id { get; }

        /// <summary>Player name as it was when the snapshot was taken.</summary>
        public string Name { get; }

        /// <summary>Damage over the window.</summary>
        public float Total { get; }

        /// <summary>Events over the window; a blow that carries two damage kinds is one hit.</summary>
        public int Hits { get; }

        /// <summary>The largest single event.</summary>
        public float Max { get; }

        /// <summary>Share of the window's total, 0..1.</summary>
        public float Share { get; }

        /// <summary>Damage per kind, indexed by <see cref="DamageKind"/>.</summary>
        public float[] ByKind { get; }

        /// <summary>
        /// At least one event in the window came from the local fallback (the target was owned by
        /// a client without the mod), so the number is a pre-mitigation estimate.
        /// </summary>
        public bool Estimated { get; }

        /// <summary>Damage per hit over the window.</summary>
        public float Average => Hits > 0 ? Total / Hits : 0f;
    }

    /// <summary>What one window of the ring adds up to. Read-only once built.</summary>
    public sealed class WindowSnapshot
    {
        /// <summary>An empty window; handed out instead of null so the drawing code stays plain.</summary>
        public static readonly WindowSnapshot Empty =
            new WindowSnapshot(0, 0f, 0, 0f, new float[DamageKinds.Count], new List<CombatantRow>());

        public WindowSnapshot(int windowSeconds, float total, int hits, float max, float[] totalByKind, IReadOnlyList<CombatantRow> rows)
        {
            WindowSeconds = windowSeconds;
            Total = total;
            Hits = hits;
            Max = max;
            TotalByKind = totalByKind;
            Rows = rows;
        }

        /// <summary>Seconds the window covers, after capping to the ring's own length.</summary>
        public int WindowSeconds { get; }

        /// <summary>Damage from every combatant in the window.</summary>
        public float Total { get; }

        /// <summary>Events from every combatant in the window.</summary>
        public int Hits { get; }

        /// <summary>The largest single event in the window, from anyone.</summary>
        public float Max { get; }

        /// <summary>Damage per kind over everyone, indexed by <see cref="DamageKind"/>.</summary>
        public float[] TotalByKind { get; }

        /// <summary>Combatants, largest total first.</summary>
        public IReadOnlyList<CombatantRow> Rows { get; }

        /// <summary>Damage per hit over everyone.</summary>
        public float Average => Hits > 0 ? Total / Hits : 0f;
    }
}
