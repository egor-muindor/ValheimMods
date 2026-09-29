using System.Collections.Generic;

namespace TidyChests.Restock
{
    /// <summary>One inventory slot the Restock button tops up.</summary>
    public sealed class RestockTarget
    {
        public RestockTarget(int index, string name, int quality, int worldLevel, int count, int maxStack, int percent)
        {
            Index = index;
            Name = name;
            Quality = quality;
            WorldLevel = worldLevel;
            Count = count;
            MaxStack = maxStack;
            Percent = percent;
        }

        /// <summary>An empty slot that remembers <paramref name="name"/>: any quality and world level will do.</summary>
        public static RestockTarget Empty(int index, string name, int percent)
        {
            return new RestockTarget(index, name, 0, -1, 0, 0, percent);
        }

        /// <summary>Position in the list handed to the planner; moves refer to it.</summary>
        public int Index { get; }

        /// <summary>Shared item name (localisation token).</summary>
        public string Name { get; }

        /// <summary>Quality of the stack in the slot; 0 when the slot is empty and any quality will do.</summary>
        public int Quality { get; }

        /// <summary>World level of the stack in the slot; -1 when the slot is empty.</summary>
        public int WorldLevel { get; }

        /// <summary>Units in the slot now.</summary>
        public int Count { get; }

        /// <summary>Largest stack of the item; 0 when the slot is empty and it is not known yet.</summary>
        public int MaxStack { get; }

        /// <summary>How full the slot is kept, in percent of <see cref="MaxStack"/>.</summary>
        public int Percent { get; }
    }

    /// <summary>One stack inside a chest. Mutable: the planner takes from it while planning.</summary>
    public sealed class RestockStack
    {
        public RestockStack(int index, string name, int quality, int worldLevel, int count, int maxStack)
        {
            Index = index;
            Name = name;
            Quality = quality;
            WorldLevel = worldLevel;
            Count = count;
            MaxStack = maxStack;
        }

        /// <summary>Position in the chest's item list handed to the planner; moves refer to it.</summary>
        public int Index { get; }

        public string Name { get; }

        public int Quality { get; }

        public int WorldLevel { get; }

        public int Count { get; set; }

        public int MaxStack { get; }
    }

    /// <summary>A chest items may be taken from.</summary>
    public sealed class RestockSource
    {
        public RestockSource(int index, float distance, List<RestockStack> stacks)
        {
            Index = index;
            Distance = distance;
            Stacks = stacks;
        }

        /// <summary>Position in the list handed to the planner; moves refer to it.</summary>
        public int Index { get; }

        /// <summary>Distance from the player in metres; nearer chests are emptied first.</summary>
        public float Distance { get; }

        public List<RestockStack> Stacks { get; }
    }

    /// <summary>Take <see cref="Amount"/> units of stack <see cref="Stack"/> of chest <see cref="Source"/> into slot <see cref="Target"/>.</summary>
    public readonly struct RestockMove
    {
        public RestockMove(int target, int source, int stack, int amount)
        {
            Target = target;
            Source = source;
            Stack = stack;
            Amount = amount;
        }

        public int Target { get; }

        public int Source { get; }

        public int Stack { get; }

        public int Amount { get; }
    }

    /// <summary>The planner's answer: the moves in execution order plus totals for messages.</summary>
    public sealed class RestockPlan
    {
        public RestockPlan(IReadOnlyList<RestockMove> moves, int units, int slotsFilled, int alreadyStocked, int notFound)
        {
            Moves = moves;
            Units = units;
            SlotsFilled = slotsFilled;
            AlreadyStocked = alreadyStocked;
            NotFound = notFound;
        }

        public IReadOnlyList<RestockMove> Moves { get; }

        /// <summary>Units taken in total.</summary>
        public int Units { get; }

        /// <summary>Slots that receive at least one unit.</summary>
        public int SlotsFilled { get; }

        /// <summary>Slots that already hold what they should.</summary>
        public int AlreadyStocked { get; }

        /// <summary>Slots short of their level for which no chest holds anything.</summary>
        public int NotFound { get; }
    }
}
