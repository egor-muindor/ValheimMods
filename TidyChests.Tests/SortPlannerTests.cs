using System.Collections.Generic;
using System.Linq;
using TidyChests.Sort;
using Xunit;

namespace TidyChests.Tests
{
    public class SortPlannerTests
    {
        private sealed class Chest
        {
            private readonly List<SortStack> _stacks = new List<SortStack>();

            public Chest(int width = 8, int height = 4)
            {
                Width = width;
                Height = height;
            }

            public int Width { get; }

            public int Height { get; }

            public IReadOnlyList<SortStack> Stacks => _stacks;

            public Chest Add(int x, int y, string id, int count = 1, int maxStack = 50, int quality = 1, int worldLevel = 0,
                string? displayName = null, string type = "Material", int maxQuality = 1)
            {
                _stacks.Add(new SortStack(_stacks.Count, new GridCell(x, y), id, "$item_" + id.ToLowerInvariant(), displayName ?? id,
                    SortKeys.TypeRank(type), quality, maxQuality, worldLevel, count, maxStack));
                return this;
            }

            public SortPlan Plan(ChestSortOrder order = ChestSortOrder.Id, ChestSortLayout layout = ChestSortLayout.Columns)
            {
                return SortPlanner.Plan(_stacks, Width, Height, order, layout);
            }
        }

        /// <summary>The sorted grid as rows of ids ("." for empty), with the counts after a colon when asked.</summary>
        private static string[] Render(Chest chest, SortPlan plan, bool counts = false)
        {
            var grid = new string[chest.Height, chest.Width];
            foreach (SortPlacement placement in plan.Placements.Where(p => !p.Removed))
            {
                Assert.Null(grid[placement.Cell.Y, placement.Cell.X]);
                SortStack stack = chest.Stacks[placement.Index];
                grid[placement.Cell.Y, placement.Cell.X] = counts ? $"{stack.Id}:{placement.Count}" : stack.Id;
            }

            var rows = new string[chest.Height];
            for (int y = 0; y < chest.Height; y++)
            {
                var cells = new List<string>();
                for (int x = 0; x < chest.Width; x++)
                {
                    cells.Add(grid[y, x] ?? ".");
                }

                rows[y] = string.Join(" ", cells);
            }

            return rows;
        }

        /// <summary>A chest whose content sorts to A A A B B C (three non-stackable A, two B, one C), scattered.</summary>
        private static Chest Scattered()
        {
            return new Chest()
                .Add(7, 3, "C", maxStack: 1)
                .Add(2, 1, "A", maxStack: 1)
                .Add(5, 0, "B", maxStack: 1)
                .Add(0, 3, "A", maxStack: 1)
                .Add(3, 2, "B", maxStack: 1)
                .Add(6, 1, "A", maxStack: 1);
        }

        [Fact]
        public void ColumnsStartEveryKindInANewColumn()
        {
            Chest chest = Scattered();

            Assert.Equal(new[]
            {
                "A B C . . . . .",
                "A B . . . . . .",
                "A . . . . . . .",
                ". . . . . . . .",
            }, Render(chest, chest.Plan(layout: ChestSortLayout.Columns)));
        }

        [Fact]
        public void RowsStartEveryKindInANewRow()
        {
            Chest chest = Scattered();

            Assert.Equal(new[]
            {
                "A A A . . . . .",
                "B B . . . . . .",
                "C . . . . . . .",
                ". . . . . . . .",
            }, Render(chest, chest.Plan(layout: ChestSortLayout.Rows)));
        }

        [Fact]
        public void SequentialPacksEverythingDensely()
        {
            Chest chest = Scattered();

            Assert.Equal(new[]
            {
                "A A A B B C . .",
                ". . . . . . . .",
                ". . . . . . . .",
                ". . . . . . . .",
            }, Render(chest, chest.Plan(layout: ChestSortLayout.Sequential)));
        }

        [Fact]
        public void KindLongerThanAColumnCarriesOnIntoTheNext()
        {
            var chest = new Chest(4, 3);
            for (int i = 0; i < 4; i++)
            {
                chest.Add(i, 2, "A", maxStack: 1);
            }

            chest.Add(0, 0, "B", maxStack: 1);

            Assert.Equal(new[]
            {
                "A A B .",
                "A . . .",
                "A . . .",
            }, Render(chest, chest.Plan()));
        }

        [Fact]
        public void MoreKindsThanRowsPacksTheRestDensely()
        {
            var chest = new Chest(4, 3);
            foreach (string id in new[] { "F", "E", "D", "C", "B", "A" })
            {
                chest.Add(chest.Stacks.Count % 4, chest.Stacks.Count / 4, id, maxStack: 1);
            }

            chest.Add(3, 2, "A", maxStack: 1);

            Assert.Equal(new[]
            {
                "A A . .",
                "B . . .",
                "C D E F",
            }, Render(chest, chest.Plan(layout: ChestSortLayout.Rows)));
        }

        [Fact]
        public void DenseFallbackKeepsEveryStackInsideAFullChest()
        {
            var chest = new Chest(3, 2);
            string[] ids = { "B", "A", "C", "A", "D", "E" };
            for (int i = 0; i < ids.Length; i++)
            {
                chest.Add(i % 3, i / 3, ids[i], maxStack: 1);
            }

            Assert.Equal(new[]
            {
                "A B D",
                "A C E",
            }, Render(chest, chest.Plan()));
        }

        [Fact]
        public void PartialStacksOfTheSameKindAreMerged()
        {
            Chest chest = new Chest()
                .Add(3, 0, "Wood", 30)
                .Add(4, 2, "Wood", 30)
                .Add(1, 1, "Wood", 25)
                .Add(6, 3, "Stone", 10);

            SortPlan plan = chest.Plan(layout: ChestSortLayout.Rows);

            Assert.Equal(new[]
            {
                "Stone:10 . . . . . . .",
                "Wood:50 Wood:35 . . . . . .",
                ". . . . . . . .",
                ". . . . . . . .",
            }, Render(chest, plan, counts: true).Select(r => r).ToArray());
            Assert.Single(plan.Placements, p => p.Removed);
            Assert.Equal(85, plan.Placements.Where(p => chest.Stacks[p.Index].Id == "Wood").Sum(p => p.Count));
        }

        [Fact]
        public void DifferentQualityOrWorldLevelIsNeverMerged()
        {
            Chest chest = new Chest()
                .Add(0, 0, "Wood", 10)
                .Add(1, 0, "Wood", 10, worldLevel: 1)
                .Add(2, 0, "Arrow", 10, quality: 2, maxQuality: 3)
                .Add(3, 0, "Arrow", 10, quality: 1, maxQuality: 3);

            SortPlan plan = chest.Plan(layout: ChestSortLayout.Sequential);

            Assert.DoesNotContain(plan.Placements, p => p.Removed);
            Assert.Equal(new[] { "Arrow:10 Arrow:10 Wood:10 Wood:10 . . . ." }, Render(chest, plan, counts: true).Take(1));
            // Better quality and the higher world level come first within the same item.
            Assert.Equal(2, chest.Stacks[plan.Placements.Single(p => p.Cell.Equals(new GridCell(0, 0))).Index].Quality);
            Assert.Equal(1, chest.Stacks[plan.Placements.Single(p => p.Cell.Equals(new GridCell(2, 0))).Index].WorldLevel);
        }

        [Fact]
        public void NameOrderUsesTheDisplayName()
        {
            Chest chest = new Chest()
                .Add(0, 0, "Wood", displayName: "Древесина")
                .Add(1, 0, "Stone", displayName: "Камень")
                .Add(2, 0, "Amber", displayName: "Янтарь");

            SortPlan plan = chest.Plan(ChestSortOrder.Name, ChestSortLayout.Sequential);

            Assert.Equal("Wood Stone Amber . . . . .", Render(chest, plan)[0]);
        }

        [Fact]
        public void TypeOrderGroupsByTypeThenId()
        {
            Chest chest = new Chest()
                .Add(0, 0, "Wood", type: "Material")
                .Add(1, 0, "SwordIron", maxStack: 1, type: "OneHandedWeapon")
                .Add(2, 0, "Bread", type: "Consumable")
                .Add(3, 0, "Axe", maxStack: 1, type: "Tool")
                .Add(4, 0, "Coal", type: "Material");

            SortPlan plan = chest.Plan(ChestSortOrder.Type, ChestSortLayout.Sequential);

            Assert.Equal("SwordIron Axe Bread Coal Wood . . .", Render(chest, plan)[0]);
        }

        [Fact]
        public void IdOrderIsCaseInsensitive()
        {
            Chest chest = new Chest()
                .Add(0, 0, "wood")
                .Add(1, 0, "Coal")
                .Add(2, 0, "amber");

            Assert.Equal("amber Coal wood . . . . .", Render(chest, chest.Plan(layout: ChestSortLayout.Sequential))[0]);
        }

        [Fact]
        public void SortedChestNeedsNoChange()
        {
            Chest chest = new Chest()
                .Add(0, 0, "A", 50)
                .Add(0, 1, "A", 20)
                .Add(1, 0, "B", maxStack: 1);

            SortPlan plan = chest.Plan();

            Assert.False(plan.HasChanges);
            Assert.Empty(plan.Moves);
        }

        [Fact]
        public void EmptyChestNeedsNoChange()
        {
            SortPlan plan = new Chest().Plan();

            Assert.False(plan.HasChanges);
            Assert.Empty(plan.Placements);
        }

        [Fact]
        public void StackAlreadyInPlaceIsNotMoved()
        {
            // Two full stacks of A: the one sitting in A's second cell stays, the other one moves in above it.
            Chest chest = new Chest()
                .Add(0, 1, "A", 50)
                .Add(5, 3, "A", 50);

            SortPlan plan = chest.Plan();

            SortMove move = Assert.Single(plan.Moves);
            Assert.Equal(1, move.Index);
            Assert.Equal(new GridCell(0, 0), move.To);
        }

        [Theory]
        [InlineData(ChestSortLayout.Columns)]
        [InlineData(ChestSortLayout.Rows)]
        [InlineData(ChestSortLayout.Sequential)]
        public void MovesReplayedWithMultiUserChestRulesGiveTheSortedChest(ChestSortLayout layout)
        {
            Chest chest = new Chest(5, 3)
                .Add(0, 0, "Wood", 30)
                .Add(1, 0, "Stone", 50)
                .Add(2, 0, "Wood", 45)
                .Add(3, 0, "Coal", 1)
                .Add(4, 0, "Sword", maxStack: 1)
                .Add(0, 1, "Stone", 7)
                .Add(1, 1, "Axe", maxStack: 1)
                .Add(2, 1, "Wood", 50)
                .Add(3, 1, "Coal", 49)
                .Add(4, 1, "Coal", 49)
                .Add(0, 2, "Amber", 3)
                .Add(4, 2, "Stone", 50);

            SortPlan plan = chest.Plan(layout: layout);

            Assert.Equal(0, plan.Skipped);
            AssertReplayMatches(chest, plan);
        }

        [Fact]
        public void FullChestIsSortedWithSwapsOnly()
        {
            var chest = new Chest(3, 2);
            string[] ids = { "F", "E", "D", "C", "B", "A" };
            for (int i = 0; i < ids.Length; i++)
            {
                chest.Add(i % 3, i / 3, ids[i], maxStack: 1);
            }

            SortPlan plan = chest.Plan(layout: ChestSortLayout.Sequential);

            Assert.Equal(new[] { "A B C", "D E F" }, Render(chest, plan));
            Assert.True(plan.Moves.Count <= 6);
            AssertReplayMatches(chest, plan);
        }

        [Fact]
        public void SameNameDifferentWorldLevelIsSwappedThroughAnEmptyCell()
        {
            // MultiUserChest takes these two for stackable and would try to merge them instead of swapping.
            Chest chest = new Chest(3, 1)
                .Add(0, 0, "Wood", 10, worldLevel: 0)
                .Add(1, 0, "Wood", 10, worldLevel: 1);

            SortPlan plan = chest.Plan(layout: ChestSortLayout.Sequential);

            Assert.Equal(0, plan.Skipped);
            Assert.Equal(1, chest.Stacks[plan.Placements.Single(p => p.Cell.Equals(new GridCell(0, 0))).Index].WorldLevel);
            Assert.Equal(3, plan.Moves.Count);
            AssertReplayMatches(chest, plan);
        }

        [Fact]
        public void SameNameSwapWithoutAnEmptyCellIsSkipped()
        {
            Chest chest = new Chest(2, 1)
                .Add(0, 0, "Wood", 10, worldLevel: 0)
                .Add(1, 0, "Wood", 10, worldLevel: 1);

            SortPlan plan = chest.Plan(layout: ChestSortLayout.Sequential);

            Assert.True(plan.HasChanges);
            Assert.Equal(2, plan.Skipped);
            Assert.Empty(plan.Moves);
        }

        [Fact]
        public void RandomChestsKeepEveryUnitAndReplayCleanly()
        {
            var random = new System.Random(1234);
            string[] ids = { "Wood", "Stone", "Coal", "Resin", "Sword", "Amber" };
            for (int round = 0; round < 500; round++)
            {
                int width = random.Next(1, 9);
                int height = random.Next(1, 6);
                var chest = new Chest(width, height);
                var cells = Enumerable.Range(0, width * height).OrderBy(_ => random.Next()).ToList();
                int stacks = random.Next(0, cells.Count + 1);
                for (int i = 0; i < stacks; i++)
                {
                    string id = ids[random.Next(ids.Length)];
                    int maxStack = id == "Sword" ? 1 : id == "Amber" ? 20 : 50;
                    chest.Add(cells[i] % width, cells[i] / width, id, random.Next(1, maxStack + 1), maxStack, worldLevel: random.Next(3) == 0 ? 1 : 0);
                }

                var layout = (ChestSortLayout)random.Next(3);
                SortPlan plan = chest.Plan(ChestSortOrder.Id, layout);

                foreach (IGrouping<string, SortStack> kind in chest.Stacks.GroupBy(s => $"{s.Id}/{s.WorldLevel}"))
                {
                    int units = kind.Sum(s => s.Count);
                    List<SortPlacement> kept = plan.Placements.Where(p => !p.Removed && kind.Contains(chest.Stacks[p.Index])).ToList();
                    Assert.Equal(units, kept.Sum(p => p.Count));
                    int max = kind.First().MaxStack;
                    Assert.True((max == 1 ? kind.Count() : (units + max - 1) / max) == kept.Count,
                        $"round {round} {layout} {width}x{height} {kind.Key}: " + string.Join(", ", chest.Stacks.Select(st => $"{st.Id}/{st.WorldLevel}:{st.Count}@{st.Cell}")) +
                        " => " + string.Join(", ", plan.Placements.Select(pl => $"{chest.Stacks[pl.Index].Id}:{pl.Count}@{pl.Cell}")));
                    Assert.True(kept.Count(p => p.Count < max) <= 1 || max == 1);
                }

                if (plan.Skipped == 0)
                {
                    AssertReplayMatches(chest, plan);
                    SortPlan again = SortPlanner.Plan(Resorted(chest, plan).Stacks, width, height, ChestSortOrder.Id, layout);
                    Assert.False(again.HasChanges, $"round {round} is not stable");
                }
            }
        }

        private static Chest Resorted(Chest chest, SortPlan plan)
        {
            var sorted = new Chest(chest.Width, chest.Height);
            foreach (SortPlacement placement in plan.Placements.Where(p => !p.Removed))
            {
                SortStack stack = chest.Stacks[placement.Index];
                sorted.Add(placement.Cell.X, placement.Cell.Y, stack.Id, placement.Count, stack.MaxStack, stack.Quality, stack.WorldLevel);
            }

            return sorted;
        }

        private static void AssertReplayMatches(Chest chest, SortPlan plan)
        {
            MucGrid grid = MucGrid.From(chest);
            foreach (SortMove move in plan.Moves)
            {
                Assert.True(grid.Apply(move), $"move {move} was refused");
            }

            var expected = new SortedDictionary<string, string>();
            foreach (SortPlacement placement in plan.Placements.Where(p => !p.Removed))
            {
                SortStack stack = chest.Stacks[placement.Index];
                expected[placement.Cell.ToString()] = $"{stack.Id}/{stack.Quality}/{stack.WorldLevel}:{placement.Count}";
            }

            Assert.Equal(expected, grid.Describe());
        }

        private sealed class MucStack
        {
            public MucStack(SortStack source)
            {
                Id = source.Id;
                Name = source.Name;
                Quality = source.Quality;
                MaxQuality = source.MaxQuality;
                WorldLevel = source.WorldLevel;
                Count = source.Count;
                MaxStack = source.MaxStack;
            }

            public string Id { get; }

            public string Name { get; }

            public int Quality { get; }

            public int MaxQuality { get; }

            public int WorldLevel { get; }

            public int Count { get; set; }

            public int MaxStack { get; }
        }

        /// <summary>
        /// The chest as MultiUserChest's owner side changes it: <c>RequestItemMove</c> checks the
        /// prefab at the source, then <c>InventoryHelper.MoveItem</c> moves into an empty cell,
        /// merges into a stack it considers stackable (vanilla <c>AddItem</c> refusing another
        /// world level), or swaps whole stacks.
        /// </summary>
        private sealed class MucGrid
        {
            private readonly Dictionary<GridCell, MucStack> _cells = new Dictionary<GridCell, MucStack>();

            public static MucGrid From(Chest chest)
            {
                var grid = new MucGrid();
                foreach (SortStack stack in chest.Stacks)
                {
                    grid._cells[stack.Cell] = new MucStack(stack);
                }

                return grid;
            }

            public MucStack? At(GridCell cell)
            {
                return _cells.TryGetValue(cell, out MucStack? stack) ? stack : null;
            }

            public bool Apply(SortMove move)
            {
                MucStack? item = At(move.From);
                if (item == null || item.Count != move.StackCount)
                {
                    return false;
                }

                MucStack? target = At(move.To);
                if (target == item)
                {
                    return true;
                }

                if (target == null)
                {
                    int amount = System.Math.Min(move.Amount, item.Count);
                    if (amount == item.Count)
                    {
                        _cells.Remove(move.From);
                        _cells[move.To] = item;
                    }
                    else
                    {
                        item.Count -= amount;
                        _cells[move.To] = new MucStack(new SortStack(0, move.To, item.Id, item.Name, item.Id, 0, item.Quality, item.MaxQuality, item.WorldLevel, amount, item.MaxStack));
                    }

                    return true;
                }

                bool mucStackable = target.Name == item.Name && (target.MaxQuality <= 1 || target.Quality == item.Quality) && target.MaxStack != 1;
                if (mucStackable)
                {
                    bool vanillaSameType = target.Name == item.Name && target.WorldLevel == item.WorldLevel && (target.MaxQuality <= 1 || target.Quality == item.Quality);
                    int space = target.MaxStack - target.Count;
                    if (!vanillaSameType || space <= 0)
                    {
                        return false;
                    }

                    int moved = System.Math.Min(space, move.Amount);
                    target.Count += moved;
                    item.Count -= moved;
                    if (item.Count == 0)
                    {
                        _cells.Remove(move.From);
                    }

                    return true;
                }

                if (move.Amount != item.Count)
                {
                    return false;
                }

                _cells[move.From] = target;
                _cells[move.To] = item;
                return true;
            }

            public SortedDictionary<string, string> Describe()
            {
                var result = new SortedDictionary<string, string>();
                foreach (KeyValuePair<GridCell, MucStack> cell in _cells)
                {
                    result[cell.Key.ToString()] = $"{cell.Value.Id}/{cell.Value.Quality}/{cell.Value.WorldLevel}:{cell.Value.Count}";
                }

                return result;
            }
        }
    }
}
