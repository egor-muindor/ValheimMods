using System;
using System.Collections.Generic;

namespace TidyChests.Sort
{
    /// <summary>
    /// Picks the empty cell where a new stack fits the chest's sort order best, without moving
    /// anything already in the chest. The cells are read in the order a sorted chest is filled
    /// (column by column for <see cref="ChestSortLayout.Columns"/>, row by row otherwise), and the
    /// chosen cell is the one with the fewest stacks on the wrong side of it: sorting after the
    /// new item but lying before the cell, or sorting before it and lying after. Among equally
    /// good cells it prefers the column (or row) that already holds the same item, then an empty
    /// column (or row), the way the Sort button starts every item in a line of its own, then the
    /// cell closest to the same item, just after it rather than just before, then the first one.
    /// </summary>
    public static class InsertSlot
    {
        public static GridCell? Pick(IReadOnlyList<SortStack> stacks, SortStack incoming, int width, int height, ChestSortOrder order, ChestSortLayout layout)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            int total = width * height;
            var at = new SortStack?[total];
            var sameLines = new HashSet<int>();
            var usedLines = new HashSet<int>();
            var samePositions = new List<int>();
            int totalLess = 0;
            foreach (SortStack stack in stacks)
            {
                if (stack.Cell.X < 0 || stack.Cell.Y < 0 || stack.Cell.X >= width || stack.Cell.Y >= height)
                {
                    continue;
                }

                int position = Position(stack.Cell, width, height, layout);
                at[position] = stack;
                usedLines.Add(Line(stack.Cell, layout));
                if (SortKeys.Compare(stack, incoming, order) < 0)
                {
                    totalLess++;
                }

                if (stack.Name == incoming.Name)
                {
                    samePositions.Add(position);
                    sameLines.Add(Line(stack.Cell, layout));
                }
            }

            GridCell? best = null;
            int bestCost = int.MaxValue;
            int bestLine = -1;
            int bestDistance = int.MaxValue;
            int greaterBefore = 0;
            int lessBefore = 0;
            for (int position = 0; position < total; position++)
            {
                SortStack? stack = at[position];
                if (stack != null)
                {
                    int compared = SortKeys.Compare(stack, incoming, order);
                    if (compared > 0)
                    {
                        greaterBefore++;
                    }
                    else if (compared < 0)
                    {
                        lessBefore++;
                    }

                    continue;
                }

                GridCell cell = CellAt(position, width, height, layout);
                int cost = greaterBefore + totalLess - lessBefore;
                int lineRank = LineRank(Line(cell, layout), sameLines, usedLines);
                int distance = Distance(position, samePositions);
                bool better = cost < bestCost
                              || (cost == bestCost && lineRank > bestLine)
                              || (cost == bestCost && lineRank == bestLine && distance < bestDistance);
                if (better)
                {
                    best = cell;
                    bestCost = cost;
                    bestLine = lineRank;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static int Position(GridCell cell, int width, int height, ChestSortLayout layout)
        {
            return layout == ChestSortLayout.Columns ? cell.X * height + cell.Y : cell.Y * width + cell.X;
        }

        private static GridCell CellAt(int position, int width, int height, ChestSortLayout layout)
        {
            return layout == ChestSortLayout.Columns
                ? new GridCell(position / height, position % height)
                : new GridCell(position % width, position / width);
        }

        /// <summary>The column for <see cref="ChestSortLayout.Columns"/>, the row for <see cref="ChestSortLayout.Rows"/>; a packed chest has no lines.</summary>
        private static int Line(GridCell cell, ChestSortLayout layout)
        {
            switch (layout)
            {
                case ChestSortLayout.Columns:
                    return cell.X;
                case ChestSortLayout.Rows:
                    return cell.Y;
                default:
                    return -1;
            }
        }

        /// <summary>2 for a line that holds the same item, 1 for an empty line, 0 for any other; a packed chest has no lines.</summary>
        private static int LineRank(int line, HashSet<int> sameLines, HashSet<int> usedLines)
        {
            if (line < 0)
            {
                return 0;
            }

            return sameLines.Contains(line) ? 2 : usedLines.Contains(line) ? 0 : 1;
        }

        /// <summary>Distance to the nearest stack of the same item in fill order, a cell just after one counting as nearer than a cell just before.</summary>
        private static int Distance(int position, List<int> samePositions)
        {
            int best = int.MaxValue;
            foreach (int same in samePositions)
            {
                int distance = position > same ? 2 * (position - same) : 2 * (same - position) + 1;
                best = Math.Min(best, distance);
            }

            return best;
        }
    }
}
