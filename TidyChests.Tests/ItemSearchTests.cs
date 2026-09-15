using System.Collections.Generic;
using System.Linq;
using TidyChests.Index;
using Xunit;

namespace TidyChests.Tests
{
    public class ItemSearchTests
    {
        private static ChestItem Item(string displayName, int count, string? name = null)
        {
            return new ChestItem(name ?? "$item_" + displayName.ToLowerInvariant(), displayName, count);
        }

        private static ChestContents Chest(int index, float distance, params ChestItem[] items)
        {
            return new ChestContents(index, $"chest {index}", distance, items.ToList());
        }

        [Fact]
        public void CountsOfTheSameItemAreAddedOverChests()
        {
            var chests = new[]
            {
                Chest(0, 4f, Item("Wood", 30)),
                Chest(1, 2f, Item("Wood", 12), Item("Stone", 5)),
            };

            List<ItemTotal> totals = ItemSearch.Summarize(chests);

            ItemTotal wood = totals.Single(total => total.DisplayName == "Wood");
            Assert.Equal(42, wood.Count);
            Assert.Equal(2, wood.ChestCount);
            Assert.Equal(2f, wood.NearestDistance);
        }

        [Fact]
        public void SeveralStacksInOneChestCountAsOneChest()
        {
            var chests = new[] { Chest(0, 3f, Item("Wood", 40), Item("Wood", 10)) };

            ItemTotal wood = Assert.Single(ItemSearch.Summarize(chests));

            Assert.Equal(50, wood.Count);
            Assert.Equal(1, wood.ChestCount);
        }

        [Fact]
        public void RowsAreSortedByDisplayName()
        {
            var chests = new[] { Chest(0, 1f, Item("Wood", 1), Item("Coal", 1), Item("Stone", 1)) };

            List<ItemTotal> totals = ItemSearch.Summarize(chests);

            Assert.Equal(new[] { "Coal", "Stone", "Wood" }, totals.Select(total => total.DisplayName));
        }

        [Fact]
        public void EmptyStacksAreIgnored()
        {
            var chests = new[] { Chest(0, 1f, Item("Wood", 0), Item("Stone", 3)) };

            ItemTotal stone = Assert.Single(ItemSearch.Summarize(chests));

            Assert.Equal("Stone", stone.DisplayName);
        }

        [Fact]
        public void AnEmptyQueryKeepsEveryRow()
        {
            List<ItemTotal> totals = ItemSearch.Summarize(new[] { Chest(0, 1f, Item("Wood", 1), Item("Stone", 1)) });

            Assert.Equal(2, ItemSearch.Filter(totals, "   ").Count);
        }

        [Fact]
        public void TheQueryIgnoresCase()
        {
            List<ItemTotal> totals = ItemSearch.Summarize(new[] { Chest(0, 1f, Item("Wood", 1)) });

            Assert.Single(ItemSearch.Filter(totals, "WOO"));
        }

        [Fact]
        public void NamesThatStartWithTheQueryComeFirst()
        {
            var chests = new[] { Chest(0, 1f, Item("Ironpit", 1), Item("Black iron", 1), Item("Iron", 1)) };
            List<ItemTotal> totals = ItemSearch.Summarize(chests);

            List<ItemTotal> filtered = ItemSearch.Filter(totals, "iron");

            Assert.Equal(new[] { "Iron", "Ironpit", "Black iron" }, filtered.Select(total => total.DisplayName));
        }

        [Fact]
        public void TheRawTokenIsSearchedToo()
        {
            var chests = new[] { Chest(0, 1f, Item("Surtling core", 1, "$item_surtlingcore")) };
            List<ItemTotal> totals = ItemSearch.Summarize(chests);

            Assert.Single(ItemSearch.Filter(totals, "surtlingcore"));
        }

        [Fact]
        public void ATokenMatchRanksBelowADisplayNameMatch()
        {
            var chests = new[]
            {
                Chest(0, 1f, Item("Ember", 1, "$item_surtlingcore")),
                Chest(1, 1f, Item("Surtling core", 1, "$item_core")),
            };
            List<ItemTotal> totals = ItemSearch.Summarize(chests);

            List<ItemTotal> filtered = ItemSearch.Filter(totals, "surtling");

            Assert.Equal(new[] { "Surtling core", "Ember" }, filtered.Select(total => total.DisplayName));
        }

        [Fact]
        public void RowsThatMatchNothingAreDropped()
        {
            List<ItemTotal> totals = ItemSearch.Summarize(new[] { Chest(0, 1f, Item("Wood", 1)) });

            Assert.Empty(ItemSearch.Filter(totals, "obsidian"));
        }

        [Fact]
        public void MatchesAgreesWithFilter()
        {
            List<ItemTotal> totals = ItemSearch.Summarize(new[] { Chest(0, 1f, Item("Wood", 1), Item("Stone", 1)) });

            Assert.True(ItemSearch.Matches(totals.Single(total => total.DisplayName == "Wood"), "oo"));
            Assert.False(ItemSearch.Matches(totals.Single(total => total.DisplayName == "Stone"), "oo"));
        }
    }
}
