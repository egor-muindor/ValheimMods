using System;
using System.Collections.Generic;
using System.Linq;

namespace TidyChests.Sort
{
    /// <summary>
    /// Decides how one chest is sorted: groups the stacks into item kinds, orders the kinds,
    /// merges each kind into as few stacks as possible and lays them out in the grid. The
    /// result is given twice: as the final state, which the chest's owner writes in one go,
    /// and as a list of moves in MultiUserChest's semantics, which a player who does not own
    /// the chest sends as requests. The moves are simulated on a model of the grid, so every
    /// request refers to where a stack will be when the owner gets to it.
    /// </summary>
    public static class SortPlanner
    {
        public static SortPlan Plan(IReadOnlyList<SortStack> stacks, int width, int height, ChestSortOrder order, ChestSortLayout layout)
        {
            int count = stacks.Count;
            var unchanged = new SortPlan(stacks.Select(s => new SortPlacement(s.Index, s.Cell, s.Count)).ToList(), new List<SortMove>(), 0, 0, false);
            if (count == 0 || width <= 0 || height <= 0 || stacks.Where((s, i) => s.Index != i).Any())
            {
                return unchanged;
            }

            List<Kind> kinds = GroupKinds(stacks, order);
            int[] needed = kinds.Select(k => k.Needed).ToArray();
            if (needed.Sum() > width * height)
            {
                return unchanged;
            }

            List<GridCell>[] cells = Layout(needed, width, height, layout);
            var model = new Model(stacks, width, height);
            if (!model.Valid)
            {
                return unchanged;
            }

            var target = new GridCell?[count];
            var keepersInOrder = new List<int>();
            for (int k = 0; k < kinds.Count; k++)
            {
                keepersInOrder.AddRange(AssignAndMerge(kinds[k], cells[k], model, target));
            }

            int skipped = 0;
            foreach (int keeper in keepersInOrder)
            {
                GridCell destination = target[keeper]!.Value;
                int occupant = model.At(destination);
                while (occupant >= 0 && occupant != keeper && stacks[occupant].SameKind(stacks[keeper]))
                {
                    // Earlier swaps can leave a stack of the same kind in this one's cell. Stacks of
                    // one kind are interchangeable, so they trade cells rather than being merged.
                    target[keeper] = target[occupant];
                    target[occupant] = destination;
                    destination = target[keeper]!.Value;
                    occupant = model.At(destination);
                }

                if (model.Position[keeper].Equals(destination))
                {
                    continue;
                }

                if (occupant >= 0 && MultiUserChestStacks(stacks[occupant], stacks[keeper]))
                {
                    // MultiUserChest would try to merge these two instead of swapping them; park the
                    // occupant in an empty cell first.
                    GridCell? empty = model.FirstEmpty();
                    if (empty == null)
                    {
                        skipped++;
                        continue;
                    }

                    model.Move(occupant, empty.Value, model.Count[occupant]);
                }

                model.Move(keeper, destination, model.Count[keeper]);
            }

            var placements = new List<SortPlacement>(count);
            bool changed = false;
            for (int i = 0; i < count; i++)
            {
                SortStack stack = stacks[i];
                SortPlacement placement = target[i] == null
                    ? new SortPlacement(i, stack.Cell, 0)
                    : new SortPlacement(i, target[i]!.Value, model.Count[i]);
                changed |= placement.Removed || !placement.Cell.Equals(stack.Cell) || placement.Count != stack.Count;
                placements.Add(placement);
            }

            return new SortPlan(placements, model.Moves, kinds.Count, skipped, changed);
        }

        /// <summary>
        /// The cells of each kind, in placement order. <paramref name="needed"/> holds the number
        /// of stacks per kind, in sort order; their sum must fit into the grid.
        /// </summary>
        public static List<GridCell>[] Layout(int[] needed, int width, int height, ChestSortLayout layout)
        {
            bool columns = layout == ChestSortLayout.Columns;
            int lineLength = columns ? height : width;
            int total = width * height;
            int remaining = needed.Sum();
            int cursor = 0;
            bool dense = layout == ChestSortLayout.Sequential;

            var result = new List<GridCell>[needed.Length];
            for (int k = 0; k < needed.Length; k++)
            {
                if (!dense)
                {
                    // A new kind starts a fresh line while everything left still fits from there;
                    // after that the rest is packed densely.
                    int lineStart = (cursor + lineLength - 1) / lineLength * lineLength;
                    if (total - lineStart >= remaining)
                    {
                        cursor = lineStart;
                    }
                    else
                    {
                        dense = true;
                    }
                }

                var kindCells = new List<GridCell>(needed[k]);
                for (int i = 0; i < needed[k]; i++, cursor++)
                {
                    kindCells.Add(columns ? new GridCell(cursor / height, cursor % height) : new GridCell(cursor % width, cursor / width));
                }

                result[k] = kindCells;
                remaining -= needed[k];
            }

            return result;
        }

        /// <summary>
        /// True when MultiUserChest's <c>InventoryHelper.CanStack</c> takes the two for stackable, in
        /// which case its move request merges instead of swapping. It ignores the world level.
        /// </summary>
        private static bool MultiUserChestStacks(SortStack there, SortStack moving)
        {
            return there.Name == moving.Name && (there.MaxQuality <= 1 || there.Quality == moving.Quality) && there.MaxStack != 1;
        }

        private static List<Kind> GroupKinds(IReadOnlyList<SortStack> stacks, ChestSortOrder order)
        {
            var byKey = new Dictionary<string, Kind>(StringComparer.Ordinal);
            var kinds = new List<Kind>();
            foreach (SortStack stack in stacks)
            {
                string key = $"{stack.Name}\n{stack.Quality}\n{stack.WorldLevel}";
                if (!byKey.TryGetValue(key, out Kind? kind))
                {
                    kind = new Kind(stack);
                    byKey[key] = kind;
                    kinds.Add(kind);
                }

                kind.Stacks.Add(stack);
            }

            kinds.Sort((a, b) => SortKeys.Compare(a.First, b.First, order));
            return kinds;
        }

        /// <summary>
        /// Picks the stacks of <paramref name="kind"/> that stay (the ones already in one of its
        /// cells first, then the largest), gives each a cell, pours the others into them and
        /// evens out the partial stacks until at most one is left. Returns the stacks that stay,
        /// in cell order.
        /// </summary>
        private static List<int> AssignAndMerge(Kind kind, List<GridCell> cells, Model model, GridCell?[] target)
        {
            var cellOrder = new Dictionary<GridCell, int>();
            for (int i = 0; i < cells.Count; i++)
            {
                cellOrder[cells[i]] = i;
            }

            var keepers = new List<int>();
            var taken = new HashSet<GridCell>();
            foreach (SortStack stack in kind.Stacks)
            {
                if (cellOrder.ContainsKey(stack.Cell))
                {
                    keepers.Add(stack.Index);
                    target[stack.Index] = stack.Cell;
                    taken.Add(stack.Cell);
                }
            }

            List<SortStack> others = kind.Stacks
                .Where(s => target[s.Index] == null)
                .OrderByDescending(s => s.Count)
                .ThenBy(s => s.Index)
                .ToList();
            var donors = new List<int>();
            int nextCell = 0;
            foreach (SortStack stack in others)
            {
                if (keepers.Count < cells.Count)
                {
                    while (taken.Contains(cells[nextCell]))
                    {
                        nextCell++;
                    }

                    keepers.Add(stack.Index);
                    target[stack.Index] = cells[nextCell];
                    taken.Add(cells[nextCell]);
                }
                else
                {
                    donors.Add(stack.Index);
                }
            }

            keepers.Sort((a, b) => cellOrder[target[a]!.Value].CompareTo(cellOrder[target[b]!.Value]));
            if (!kind.Mergeable)
            {
                return keepers;
            }

            int max = kind.First.MaxStack;
            donors.Sort();
            foreach (int donor in donors)
            {
                foreach (int keeper in keepers)
                {
                    int space = max - model.Count[keeper];
                    if (space > 0 && model.Count[donor] > 0)
                    {
                        model.Move(donor, model.Position[keeper], Math.Min(space, model.Count[donor]));
                    }
                }
            }

            while (true)
            {
                List<int> partial = keepers.Where(k => model.Count[k] < max).ToList();
                if (partial.Count < 2)
                {
                    break;
                }

                int first = partial[0];
                int last = partial[partial.Count - 1];
                model.Move(last, model.Position[first], Math.Min(model.Count[last], max - model.Count[first]));
            }

            return keepers;
        }

        private sealed class Kind
        {
            public Kind(SortStack first)
            {
                First = first;
            }

            public SortStack First { get; }

            public List<SortStack> Stacks { get; } = new List<SortStack>();

            /// <summary>Stackable, and no stack holds more than the stack size (a mod could have put more in).</summary>
            public bool Mergeable => First.MaxStack > 1 && Stacks.All(s => s.MaxStack == First.MaxStack && s.Count <= s.MaxStack);

            /// <summary>Number of stacks the kind needs after merging.</summary>
            public int Needed => Mergeable ? (Stacks.Sum(s => s.Count) + First.MaxStack - 1) / First.MaxStack : Stacks.Count;
        }

        /// <summary>The chest grid while the moves are planned, with the moves recorded as they are made.</summary>
        private sealed class Model
        {
            private readonly IReadOnlyList<SortStack> _stacks;

            private readonly int _width;

            private readonly int _height;

            private readonly Dictionary<GridCell, int> _cells = new Dictionary<GridCell, int>();

            public Model(IReadOnlyList<SortStack> stacks, int width, int height)
            {
                _stacks = stacks;
                _width = width;
                _height = height;
                Position = new GridCell[stacks.Count];
                Count = new int[stacks.Count];
                Valid = true;
                foreach (SortStack stack in stacks)
                {
                    Position[stack.Index] = stack.Cell;
                    Count[stack.Index] = stack.Count;
                    bool inside = stack.Cell.X >= 0 && stack.Cell.Y >= 0 && stack.Cell.X < width && stack.Cell.Y < height;
                    if (!inside || _cells.ContainsKey(stack.Cell))
                    {
                        Valid = false;
                    }

                    _cells[stack.Cell] = stack.Index;
                }
            }

            /// <summary>False when two stacks share a cell or one lies outside the grid; such a chest is left alone.</summary>
            public bool Valid { get; }

            public GridCell[] Position { get; }

            public int[] Count { get; }

            public List<SortMove> Moves { get; } = new List<SortMove>();

            public int At(GridCell cell)
            {
                return _cells.TryGetValue(cell, out int index) ? index : -1;
            }

            public GridCell? FirstEmpty()
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        var cell = new GridCell(x, y);
                        if (!_cells.ContainsKey(cell))
                        {
                            return cell;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Records and applies one move. Only whole stacks go to an empty cell or swap; only
            /// stacks of the same kind are merged. The planner never asks for anything else.
            /// </summary>
            public void Move(int index, GridCell to, int amount)
            {
                GridCell from = Position[index];
                Moves.Add(new SortMove(index, from, to, amount, Count[index]));
                int occupant = At(to);
                if (occupant < 0)
                {
                    _cells.Remove(from);
                    _cells[to] = index;
                    Position[index] = to;
                }
                else if (_stacks[index].SameKind(_stacks[occupant]))
                {
                    Count[occupant] += amount;
                    Count[index] -= amount;
                    if (Count[index] == 0)
                    {
                        _cells.Remove(from);
                    }
                }
                else
                {
                    _cells[from] = occupant;
                    _cells[to] = index;
                    Position[occupant] = from;
                    Position[index] = to;
                }
            }
        }
    }
}
