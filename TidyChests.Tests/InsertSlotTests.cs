using System.Collections.Generic;
using TidyChests.Sort;
using Xunit;

namespace TidyChests.Tests
{
    public class InsertSlotTests
    {
        private static SortStack Stack(string id, int x = -1, int y = -1, int quality = 1)
        {
            return new SortStack(0, new GridCell(x, y), id, "$item_" + id.ToLowerInvariant(), id, SortKeys.TypeRank("Material"),
                quality, 4, 0, 1, 50);
        }

        /// <summary>A chest from rows of ids, "." for an empty cell.</summary>
        private static List<SortStack> Chest(params string[] rows)
        {
            var stacks = new List<SortStack>();
            for (int y = 0; y < rows.Length; y++)
            {
                string[] cells = rows[y].Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < cells.Length; x++)
                {
                    if (cells[x] != ".")
                    {
                        stacks.Add(Stack(cells[x], x, y));
                    }
                }
            }

            return stacks;
        }

        private static GridCell? Pick(List<SortStack> chest, string id, ChestSortLayout layout, int width = 4, int height = 3, int quality = 1)
        {
            return InsertSlot.Pick(chest, Stack(id, quality: quality), width, height, ChestSortOrder.Id, layout);
        }

        [Fact]
        public void EmptyChestTakesTheFirstCell()
        {
            Assert.Equal(new GridCell(0, 0), Pick(new List<SortStack>(), "Wood", ChestSortLayout.Columns));
        }

        [Fact]
        public void FullChestHasNoCell()
        {
            List<SortStack> chest = Chest("A A", "A A");
            Assert.Null(Pick(chest, "A", ChestSortLayout.Columns, 2, 2));
        }

        [Fact]
        public void ColumnsGoesUnderTheSameItemRatherThanUnderTheItemBefore()
        {
            // Coal fills column 0, Stone has room under it in column 1, Wood in column 2.
            List<SortStack> chest = Chest(
                "Coal Stone Wood .",
                "Coal Stone Wood .",
                "Coal .     .    .");
            Assert.Equal(new GridCell(2, 2), Pick(chest, "Wood", ChestSortLayout.Columns));
            Assert.Equal(new GridCell(1, 2), Pick(chest, "Stone", ChestSortLayout.Columns));
        }

        [Fact]
        public void ColumnsStartsTheNextColumnWhenTheItemsColumnIsFull()
        {
            List<SortStack> chest = Chest(
                "Coal Wood . .",
                "Coal Wood . .",
                ".    Wood . .");
            Assert.Equal(new GridCell(2, 0), Pick(chest, "Wood", ChestSortLayout.Columns));
        }

        [Fact]
        public void NewItemGoesBetweenTheItemsAroundItInTheOrder()
        {
            List<SortStack> chest = Chest(
                "Coal . . Wood",
                ".    . . .   ",
                ".    . . .   ");
            // Stone sorts between Coal and Wood and starts a column of its own there.
            Assert.Equal(new GridCell(1, 0), Pick(chest, "Stone", ChestSortLayout.Columns));

            // Past the end of the order it goes after the last item.
            Assert.Equal(new GridCell(3, 1), Pick(chest, "Zinc", ChestSortLayout.Columns));
            // Before the first item there is no empty cell, so the first empty column after it.
            Assert.Equal(new GridCell(1, 0), Pick(chest, "Amber", ChestSortLayout.Columns));
        }

        [Fact]
        public void RowsFillsTheItemsRow()
        {
            List<SortStack> chest = Chest(
                "Coal Coal .    .",
                "Wood Wood Wood .",
                ".    .    .    .");
            Assert.Equal(new GridCell(3, 1), Pick(chest, "Wood", ChestSortLayout.Rows));
            Assert.Equal(new GridCell(2, 0), Pick(chest, "Coal", ChestSortLayout.Rows));
        }

        [Fact]
        public void SequentialGoesRightAfterTheSameItem()
        {
            List<SortStack> chest = Chest(
                "Coal Wood . .",
                ".    .    . .",
                ".    .    . .");
            Assert.Equal(new GridCell(2, 0), Pick(chest, "Wood", ChestSortLayout.Sequential));
        }

        [Fact]
        public void BetterQualityGoesBeforeTheSameItemOfLowerQuality()
        {
            var chest = new List<SortStack> { Stack("Sword", 1, 0, quality: 1) };
            Assert.Equal(new GridCell(0, 0), Pick(chest, "Sword", ChestSortLayout.Sequential, quality: 3));
            Assert.Equal(new GridCell(2, 0), Pick(chest, "Sword", ChestSortLayout.Sequential, quality: 1));
        }

        [Fact]
        public void UnsortedChestStillPrefersTheSameItemsNeighbourhood()
        {
            List<SortStack> chest = Chest(
                "Wood Coal . .",
                ".    .    . Wood",
                ".    .    . .");
            GridCell? cell = Pick(chest, "Wood", ChestSortLayout.Sequential);
            Assert.NotNull(cell);
            Assert.True(cell!.Value.Equals(new GridCell(0, 2)) || cell.Value.Y == 1 || cell.Value.Y == 2);
        }
    }
}
