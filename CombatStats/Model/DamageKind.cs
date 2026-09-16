using System;

namespace CombatStats.Model
{
    /// <summary>
    /// The damage types the game splits a hit into. The values are indexes into the per-hit
    /// arrays the meter passes around, so their order is part of the wire format and must not
    /// change without raising <c>EventCodec.Version</c>.
    /// </summary>
    public enum DamageKind
    {
        Blunt = 0,
        Slash = 1,
        Pierce = 2,
        Chop = 3,
        Pickaxe = 4,
        Fire = 5,
        Frost = 6,
        Lightning = 7,
        Poison = 8,
        Spirit = 9,

        /// <summary>
        /// The game's type-less <c>damage</c> field: what goes through resistances untouched.
        /// Rare on weapons, common on falls and on a few effects, and counted so that the totals
        /// of the meter match the health the target really lost.
        /// </summary>
        Generic = 10,
    }

    /// <summary>Names, colours and the two groups the meter treats differently.</summary>
    public static class DamageKinds
    {
        /// <summary>How many kinds there are; the length of every per-hit array.</summary>
        public const int Count = 11;

        /// <summary>Every kind, in the order of the enum.</summary>
        public static readonly DamageKind[] All =
        {
            DamageKind.Blunt,
            DamageKind.Slash,
            DamageKind.Pierce,
            DamageKind.Chop,
            DamageKind.Pickaxe,
            DamageKind.Fire,
            DamageKind.Frost,
            DamageKind.Lightning,
            DamageKind.Poison,
            DamageKind.Spirit,
            DamageKind.Generic,
        };

        /// <summary>
        /// The key the game itself uses for this kind in item tooltips, so the meter shows the
        /// name in the player's own language without shipping a single translation.
        /// </summary>
        public static string LocalizationKey(DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return "$inventory_blunt";
                case DamageKind.Slash: return "$inventory_slash";
                case DamageKind.Pierce: return "$inventory_pierce";
                case DamageKind.Chop: return "$inventory_chop";
                case DamageKind.Pickaxe: return "$inventory_pickaxe";
                case DamageKind.Fire: return "$inventory_fire";
                case DamageKind.Frost: return "$inventory_frost";
                case DamageKind.Lightning: return "$inventory_lightning";
                case DamageKind.Poison: return "$inventory_poison";
                case DamageKind.Spirit: return "$inventory_spirit";
                case DamageKind.Generic: return "$inventory_damage";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>What to show when the game has no word for the key (a stripped localization).</summary>
        public static string EnglishName(DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return "Blunt";
                case DamageKind.Slash: return "Slash";
                case DamageKind.Pierce: return "Pierce";
                case DamageKind.Chop: return "Chop";
                case DamageKind.Pickaxe: return "Pickaxe";
                case DamageKind.Fire: return "Fire";
                case DamageKind.Frost: return "Frost";
                case DamageKind.Lightning: return "Lightning";
                case DamageKind.Poison: return "Poison";
                case DamageKind.Spirit: return "Spirit";
                case DamageKind.Generic: return "Damage";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>
        /// The default colour of the kind, muted on purpose: a segmented bar should read as one
        /// object on a dark HUD, not as a row of warning lights. Overridable in the config.
        /// </summary>
        public static string ColorHex(DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return "#a09d97";
                case DamageKind.Slash: return "#b0b8be";
                case DamageKind.Pierce: return "#c3b382";
                case DamageKind.Chop: return "#b09a6a";
                case DamageKind.Pickaxe: return "#a79b8c";
                case DamageKind.Fire: return "#bb7644";
                case DamageKind.Frost: return "#7ea3b4";
                case DamageKind.Lightning: return "#988dba";
                case DamageKind.Poison: return "#829a5d";
                case DamageKind.Spirit: return "#d5ccae";
                case DamageKind.Generic: return "#c9bfa6";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>
        /// Fire, poison and spirit are cut out of the blow and tick on the target later, without
        /// an attacker. Their ticks are attributed through <c>DotAttribution</c>.
        /// </summary>
        public static bool IsOverTime(DamageKind kind)
        {
            return kind == DamageKind.Fire || kind == DamageKind.Poison || kind == DamageKind.Spirit;
        }

        /// <summary>Chopping and mining damage: what tools do to trees and rock, not to creatures.</summary>
        public static bool IsToolDamage(DamageKind kind)
        {
            return kind == DamageKind.Chop || kind == DamageKind.Pickaxe;
        }
    }
}
