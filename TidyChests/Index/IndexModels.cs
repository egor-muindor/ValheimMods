using System.Collections.Generic;

namespace TidyChests.Index
{
    /// <summary>One item kind inside one container: how many units of it are there.</summary>
    public sealed class ChestItem
    {
        public ChestItem(string name, string displayName, int count)
        {
            Name = name;
            DisplayName = displayName;
            Count = count;
        }

        /// <summary>Shared item name (localisation token); the same name means the same item.</summary>
        public string Name { get; }

        /// <summary>The name as the player reads it, in the game's language.</summary>
        public string DisplayName { get; }

        /// <summary>Units in this container (a stack of 40 wood plus a stack of 10 counts 50).</summary>
        public int Count { get; }
    }

    /// <summary>The readable contents of one container, as seen from the player.</summary>
    public sealed class ChestContents
    {
        public ChestContents(int index, string name, float distance, IReadOnlyList<ChestItem> items)
        {
            Index = index;
            Name = name;
            Distance = distance;
            Items = items;
        }

        /// <summary>Position in the list handed to the search; the caller maps it back to a container.</summary>
        public int Index { get; }

        /// <summary>Display name of the container, for logs.</summary>
        public string Name { get; }

        /// <summary>Distance from the player in metres.</summary>
        public float Distance { get; }

        public IReadOnlyList<ChestItem> Items { get; }
    }

    /// <summary>One row of the browser: an item kind totalled over the containers that hold it.</summary>
    public sealed class ItemTotal
    {
        public ItemTotal(string name, string displayName, int count, int chestCount, float nearestDistance)
        {
            Name = name;
            DisplayName = displayName;
            Count = count;
            ChestCount = chestCount;
            NearestDistance = nearestDistance;
        }

        public string Name { get; }

        public string DisplayName { get; }

        /// <summary>Units over all containers in range.</summary>
        public int Count { get; }

        /// <summary>Containers that hold at least one unit.</summary>
        public int ChestCount { get; }

        /// <summary>Distance to the nearest container holding it, in metres.</summary>
        public float NearestDistance { get; }
    }
}
