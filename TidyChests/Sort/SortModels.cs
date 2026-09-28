using System;
using System.Collections.Generic;

namespace TidyChests.Sort
{
    /// <summary>What decides the order of the item kinds in a sorted chest.</summary>
    public enum ChestSortOrder
    {
        /// <summary>Prefab name, ascending: Blueberries, Coal, Wood.</summary>
        Id,

        /// <summary>Display name in the current language.</summary>
        Name,

        /// <summary>Item type group first (weapons, tools, armour, ammo, food, materials, trophies, misc), then the prefab name.</summary>
        Type,
    }

    /// <summary>How the sorted stacks are laid out in the chest grid.</summary>
    public enum ChestSortLayout
    {
        /// <summary>Every item kind starts a new column, filled top to bottom.</summary>
        Columns,

        /// <summary>Every item kind starts a new row, filled left to right.</summary>
        Rows,

        /// <summary>Packed densely left to right, row by row, with no gaps.</summary>
        Sequential,
    }

    /// <summary>A cell of the chest grid, x to the right and y down, both from 0.</summary>
    public struct GridCell : IEquatable<GridCell>
    {
        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(GridCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is GridCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        public override string ToString()
        {
            return $"{X}:{Y}";
        }
    }

    /// <summary>One stack in the chest, as the planner sees it.</summary>
    public sealed class SortStack
    {
        public SortStack(int index, GridCell cell, string id, string name, string displayName, int typeRank,
            int quality, int maxQuality, int worldLevel, int count, int maxStack)
        {
            Index = index;
            Cell = cell;
            Id = id;
            Name = name;
            DisplayName = displayName;
            TypeRank = typeRank;
            Quality = quality;
            MaxQuality = maxQuality;
            WorldLevel = worldLevel;
            Count = count;
            MaxStack = maxStack;
        }

        /// <summary>Position in the list handed to the planner; placements and moves refer to it.</summary>
        public int Index { get; }

        public GridCell Cell { get; }

        /// <summary>Prefab name, the item's ID.</summary>
        public string Id { get; }

        /// <summary>Shared item name (localization token); together with quality and world level it is what vanilla stacks by.</summary>
        public string Name { get; }

        /// <summary>Localized name, for <see cref="ChestSortOrder.Name"/>.</summary>
        public string DisplayName { get; }

        /// <summary>Rank of the item type group, for <see cref="ChestSortOrder.Type"/>; see <see cref="SortKeys.TypeRank"/>.</summary>
        public int TypeRank { get; }

        public int Quality { get; }

        public int MaxQuality { get; }

        public int WorldLevel { get; }

        public int Count { get; }

        public int MaxStack { get; }

        /// <summary>True when vanilla would stack the two into one: same name, quality and world level.</summary>
        public bool SameKind(SortStack other)
        {
            return Name == other.Name && Quality == other.Quality && WorldLevel == other.WorldLevel;
        }
    }

    /// <summary>Where a stack ends up and with how many units; a stack emptied by merging is <see cref="Removed"/>.</summary>
    public sealed class SortPlacement
    {
        public SortPlacement(int index, GridCell cell, int count)
        {
            Index = index;
            Cell = cell;
            Count = count;
        }

        public int Index { get; }

        public GridCell Cell { get; }

        public int Count { get; }

        public bool Removed => Count <= 0;
    }

    /// <summary>
    /// One request in the order MultiUserChest executes it on the chest's owner: move
    /// <see cref="Amount"/> units of the stack at <see cref="From"/> to <see cref="To"/>. An empty
    /// target takes them, a stack of the same kind is topped up, and a different item is swapped
    /// with the whole stack.
    /// </summary>
    public sealed class SortMove
    {
        public SortMove(int index, GridCell from, GridCell to, int amount, int stackCount)
        {
            Index = index;
            From = from;
            To = to;
            Amount = amount;
            StackCount = stackCount;
        }

        /// <summary>The planner's index of the stack being moved.</summary>
        public int Index { get; }

        public GridCell From { get; }

        public GridCell To { get; }

        public int Amount { get; }

        /// <summary>Units in the moving stack just before the move, as the owner will see them.</summary>
        public int StackCount { get; }

        public override string ToString()
        {
            return $"#{Index} {From} -> {To} x{Amount}";
        }
    }

    /// <summary>The result of planning one sort.</summary>
    public sealed class SortPlan
    {
        public SortPlan(IReadOnlyList<SortPlacement> placements, IReadOnlyList<SortMove> moves, int kinds, int skipped, bool hasChanges)
        {
            HasChanges = hasChanges;
            Placements = placements;
            Moves = moves;
            Kinds = kinds;
            Skipped = skipped;
        }

        /// <summary>The sorted state, one entry per input stack, in input order.</summary>
        public IReadOnlyList<SortPlacement> Placements { get; }

        /// <summary>The requests that turn the current state into <see cref="Placements"/> through MultiUserChest.</summary>
        public IReadOnlyList<SortMove> Moves { get; }

        /// <summary>Number of item kinds in the chest.</summary>
        public int Kinds { get; }

        /// <summary>Stacks the move list could not put in place (a same-name swap in a chest without an empty cell).</summary>
        public int Skipped { get; }

        /// <summary>False when the chest is already sorted and merged.</summary>
        public bool HasChanges { get; }
    }
}
