using System;

namespace TidyChests.Restock
{
    /// <summary>What an item in a slot mod's slot is, as far as restocking goes.</summary>
    public enum RestockKind
    {
        Food,
        Mead,
        Ammo,
        Other,
    }

    /// <summary>
    /// How full the Restock button keeps the items in slot mods' slots (quick slots, food and
    /// ammo slots, the quiver), by kind of item. Pure: no game types, so it is unit-tested.
    /// </summary>
    public sealed class RestockLevels
    {
        public RestockLevels(int food, int mead, int ammo, int other)
        {
            Food = Clamp(food);
            Mead = Clamp(mead);
            Ammo = Clamp(ammo);
            Other = Clamp(other);
        }

        public int Food { get; }

        public int Mead { get; }

        public int Ammo { get; }

        public int Other { get; }

        /// <summary>
        /// The kind of an item from its type name and food values: consumables that feed are
        /// food, the other consumables meads, both ammo types ammo.
        /// </summary>
        public static RestockKind KindOf(string typeName, bool feeds)
        {
            switch (typeName)
            {
                case "Consumable":
                    return feeds ? RestockKind.Food : RestockKind.Mead;
                case "Ammo":
                case "AmmoNonEquipable":
                    return RestockKind.Ammo;
                default:
                    return RestockKind.Other;
            }
        }

        /// <summary>The percentage for a kind; 0 means the Restock button leaves such items alone.</summary>
        public int PercentFor(RestockKind kind)
        {
            switch (kind)
            {
                case RestockKind.Food:
                    return Food;
                case RestockKind.Mead:
                    return Mead;
                case RestockKind.Ammo:
                    return Ammo;
                default:
                    return Other;
            }
        }

        /// <summary>
        /// How many units a slot kept at <paramref name="percent"/> should hold, rounded up so a
        /// small stack is never kept at zero: half of a stack of 1 is 1, half of 25 is 13.
        /// </summary>
        public static int Wanted(int maxStack, int percent)
        {
            if (maxStack <= 1 || percent <= 0)
            {
                return 0;
            }

            int units = (int)Math.Ceiling(maxStack * (Clamp(percent) / 100.0));
            return Math.Max(1, Math.Min(maxStack, units));
        }

        /// <summary>One line for the log.</summary>
        public string Describe()
        {
            return $"slot mods: food {Food}%, meads {Mead}%, ammo {Ammo}%, other {Other}%";
        }

        private static int Clamp(int percent)
        {
            return Math.Max(0, Math.Min(100, percent));
        }
    }
}
