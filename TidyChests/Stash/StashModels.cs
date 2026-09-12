using System.Collections.Generic;

namespace TidyChests.Stash
{
    /// <summary>One candidate stack in the player's inventory.</summary>
    public sealed class ItemSnapshot
    {
        public ItemSnapshot(int index, string name, int quality, int worldLevel, int count, int maxStack)
        {
            Index = index;
            Name = name;
            Quality = quality;
            WorldLevel = worldLevel;
            Count = count;
            MaxStack = maxStack;
        }

        /// <summary>Position in the list handed to the planner; moves refer to it.</summary>
        public int Index { get; }

        /// <summary>Shared item name (localisation token); "similar item" means the same name.</summary>
        public string Name { get; }

        public int Quality { get; }

        public int WorldLevel { get; }

        public int Count { get; }

        public int MaxStack { get; }
    }

    /// <summary>One stack inside a container. Mutable: the planner fills it up while planning.</summary>
    public sealed class StackSnapshot
    {
        public StackSnapshot(string name, int quality, int worldLevel, int count, int maxStack)
        {
            Name = name;
            Quality = quality;
            WorldLevel = worldLevel;
            Count = count;
            MaxStack = maxStack;
        }

        public string Name { get; }

        public int Quality { get; }

        public int WorldLevel { get; }

        public int Count { get; set; }

        public int MaxStack { get; }

        public int FreeSpace => MaxStack - Count;

        /// <summary>True when units of <paramref name="item"/> can be added to this stack.</summary>
        public bool Accepts(ItemSnapshot item)
        {
            return MaxStack > 1 && Name == item.Name && Quality == item.Quality && WorldLevel == item.WorldLevel;
        }
    }

    /// <summary>A container that may receive items. Mutable: the planner consumes its space while planning.</summary>
    public sealed class ContainerSnapshot
    {
        public ContainerSnapshot(int index, string name, float distance, List<StackSnapshot> stacks, int emptySlots)
        {
            Index = index;
            Name = name;
            Distance = distance;
            Stacks = stacks;
            EmptySlots = emptySlots;
        }

        /// <summary>Position in the list handed to the planner; moves refer to it.</summary>
        public int Index { get; }

        /// <summary>Display name, for logs.</summary>
        public string Name { get; }

        /// <summary>Distance from the player in metres; nearer containers are filled first.</summary>
        public float Distance { get; }

        public List<StackSnapshot> Stacks { get; }

        public int EmptySlots { get; set; }

        /// <summary>True when the container already holds an item with this name, whatever its quality.</summary>
        public bool Holds(string name)
        {
            foreach (StackSnapshot stack in Stacks)
            {
                if (stack.Name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Move <see cref="Amount"/> units of the item at <see cref="Item"/> into the container at <see cref="Container"/>.</summary>
    public readonly struct StashMove
    {
        public StashMove(int item, int container, int amount)
        {
            Item = item;
            Container = container;
            Amount = amount;
        }

        public int Item { get; }

        public int Container { get; }

        public int Amount { get; }
    }

    /// <summary>The planner's answer: the moves in execution order plus totals for messages.</summary>
    public sealed class StashPlan
    {
        public StashPlan(IReadOnlyList<StashMove> moves, int units, int itemsTouched, int containersUsed)
        {
            Moves = moves;
            Units = units;
            ItemsTouched = itemsTouched;
            ContainersUsed = containersUsed;
        }

        public IReadOnlyList<StashMove> Moves { get; }

        /// <summary>Units moved in total (a stack of 40 wood counts 40).</summary>
        public int Units { get; }

        /// <summary>Inventory stacks that lose at least one unit.</summary>
        public int ItemsTouched { get; }

        /// <summary>Containers that receive at least one unit.</summary>
        public int ContainersUsed { get; }
    }
}
