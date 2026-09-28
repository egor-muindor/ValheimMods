using System;
using System.Collections.Generic;

namespace TidyChests.Sort
{
    /// <summary>How the item kinds of a chest compare for each <see cref="ChestSortOrder"/>.</summary>
    public static class SortKeys
    {
        /// <summary>
        /// The groups of <see cref="ChestSortOrder.Type"/>, by <c>ItemDrop.ItemData.ItemType</c> name:
        /// weapons, shields, tools, armour, ammo, food, materials, trophies, then everything else.
        /// </summary>
        private static readonly string[][] TypeGroups =
        {
            new[] { "OneHandedWeapon", "TwoHandedWeapon", "TwoHandedWeaponLeft", "Bow", "Attach_Atgeir", "Torch" },
            new[] { "Shield" },
            new[] { "Tool" },
            new[] { "Helmet", "Chest", "Legs", "Hands", "Shoulder", "Utility", "Trinket" },
            new[] { "Ammo", "AmmoNonEquipable" },
            new[] { "Consumable", "Fish" },
            new[] { "Material" },
            new[] { "Trophy" },
            new[] { "Misc", "Customization" },
        };

        private static readonly Dictionary<string, int> TypeRanks = BuildTypeRanks();

        /// <summary>Rank of an item type name for <see cref="ChestSortOrder.Type"/>; unknown types (a mod's own) go last.</summary>
        public static int TypeRank(string typeName)
        {
            return TypeRanks.TryGetValue(typeName ?? "", out int rank) ? rank : TypeGroups.Length;
        }

        /// <summary>
        /// Compares two stacks by their kind: the order's own key, then the better quality and the
        /// higher world level first, then the prefab name and the shared name so that the order
        /// never depends on where the stacks lay before.
        /// </summary>
        public static int Compare(SortStack a, SortStack b, ChestSortOrder order)
        {
            int result = 0;
            switch (order)
            {
                case ChestSortOrder.Name:
                    result = string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase);
                    break;
                case ChestSortOrder.Type:
                    result = a.TypeRank.CompareTo(b.TypeRank);
                    break;
            }

            if (result == 0)
            {
                result = string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
            }

            if (result == 0)
            {
                result = b.Quality.CompareTo(a.Quality);
            }

            if (result == 0)
            {
                result = b.WorldLevel.CompareTo(a.WorldLevel);
            }

            if (result == 0)
            {
                result = string.CompareOrdinal(a.Id, b.Id);
            }

            if (result == 0)
            {
                result = string.CompareOrdinal(a.Name, b.Name);
            }

            return result;
        }

        private static Dictionary<string, int> BuildTypeRanks()
        {
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int rank = 0; rank < TypeGroups.Length; rank++)
            {
                foreach (string type in TypeGroups[rank])
                {
                    ranks[type] = rank;
                }
            }

            return ranks;
        }
    }
}
