using System.Collections.Generic;
using System.Linq;
using TidyChests.Index;
using Xunit;

namespace TidyChests.Tests
{
    /// <summary>The order the chest list puts its rows in: the header's sort and the pinned block.</summary>
    public class BrowserOrderTests
    {
        private static ItemTotal Row(string displayName, int count, string? name = null)
        {
            return new ItemTotal(name ?? "$item_" + displayName.ToLowerInvariant(), displayName, count, count > 0 ? 1 : 0, 5f);
        }

        private static readonly ItemTotal[] Rows =
        {
            Row("Wood", 120),
            Row("Coal", 40),
            Row("Iron", 40),
            Row("Stone", 300),
        };

        private static string[] Names(IEnumerable<ItemTotal> rows)
        {
            return rows.Select(row => row.DisplayName).ToArray();
        }

        [Fact]
        public void MostOfItFirstIsTheDefault()
        {
            List<ItemTotal> arranged = ItemSearch.Arrange(Rows, "", BrowserSort.CountDescending, null);

            Assert.Equal(new[] { "Stone", "Wood", "Coal", "Iron" }, Names(arranged));
        }

        [Fact]
        public void LeastOfItFirstIsTheOtherDirection()
        {
            List<ItemTotal> arranged = ItemSearch.Arrange(Rows, "", BrowserSort.CountAscending, null);

            Assert.Equal(new[] { "Coal", "Iron", "Wood", "Stone" }, Names(arranged));
        }

        [Theory]
        [InlineData(BrowserSort.CountDescending)]
        [InlineData(BrowserSort.CountAscending)]
        public void EqualCountsAreBrokenByNameSoTheListDoesNotJump(BrowserSort sort)
        {
            List<ItemTotal> first = ItemSearch.Arrange(Rows, "", sort, null);
            List<ItemTotal> second = ItemSearch.Arrange(Rows.Reverse().ToList(), "", sort, null);

            Assert.Equal(Names(first), Names(second));
            Assert.True(first.IndexOf(first.Single(r => r.DisplayName == "Coal")) <
                        first.IndexOf(first.Single(r => r.DisplayName == "Iron")));
        }

        [Fact]
        public void SortsByNameBothWays()
        {
            Assert.Equal(new[] { "Coal", "Iron", "Stone", "Wood" }, Names(ItemSearch.Arrange(Rows, "", BrowserSort.NameAscending, null)));
            Assert.Equal(new[] { "Wood", "Stone", "Iron", "Coal" }, Names(ItemSearch.Arrange(Rows, "", BrowserSort.NameDescending, null)));
        }

        [Fact]
        public void PinnedItemsComeFirstInTheChosenOrder()
        {
            FavoriteList pinned = FavoriteList.Parse("$item_iron, $item_coal");

            List<ItemTotal> arranged = ItemSearch.Arrange(Rows, "", BrowserSort.CountDescending, pinned);

            Assert.Equal(new[] { "Coal", "Iron", "Stone", "Wood" }, Names(arranged));
        }

        [Fact]
        public void APinnedItemThatRanOutStaysAtTheTop()
        {
            var rows = new List<ItemTotal>(Rows) { Row("Flax", 0) };
            FavoriteList pinned = FavoriteList.Parse("$item_flax");

            List<ItemTotal> arranged = ItemSearch.Arrange(rows, "", BrowserSort.CountDescending, pinned);

            Assert.Equal("Flax", arranged[0].DisplayName);
            Assert.Equal(0, arranged[0].Count);
        }

        [Fact]
        public void SearchingDropsThePinning()
        {
            FavoriteList pinned = FavoriteList.Parse("$item_coal");

            List<ItemTotal> withoutQuery = ItemSearch.Arrange(Rows, "", BrowserSort.CountDescending, pinned);
            List<ItemTotal> withQuery = ItemSearch.Arrange(Rows, "o", BrowserSort.CountDescending, pinned);

            // Pinned, so Coal leads the plain list even though three rows hold more.
            Assert.Equal("Coal", withoutQuery[0].DisplayName);

            // Searching, it falls back to where the count puts it. Iron is last: it is matched
            // only through its raw name ($item_iron), which ranks below a match on the shown name.
            Assert.Equal(new[] { "Stone", "Wood", "Coal", "Iron" }, Names(withQuery));
        }

        [Fact]
        public void TheBestMatchStillWinsOverTheChosenOrder()
        {
            var rows = new[] { Row("Wood", 5), Row("Fine wood", 900) };

            List<ItemTotal> arranged = ItemSearch.Arrange(rows, "wood", BrowserSort.CountDescending, null);

            Assert.Equal(new[] { "Wood", "Fine wood" }, Names(arranged));
        }

        [Fact]
        public void TheChosenOrderDecidesAmongEquallyGoodMatches()
        {
            var rows = new[] { Row("Iron nails", 10), Row("Iron", 400), Row("Iron scrap", 90) };

            List<ItemTotal> byCount = ItemSearch.Arrange(rows, "iron", BrowserSort.CountDescending, null);
            List<ItemTotal> byName = ItemSearch.Arrange(rows, "iron", BrowserSort.NameAscending, null);

            Assert.Equal(new[] { "Iron", "Iron scrap", "Iron nails" }, Names(byCount));
            Assert.Equal(new[] { "Iron", "Iron nails", "Iron scrap" }, Names(byName));
        }

        [Fact]
        public void PinningNothingChangesNothing()
        {
            List<ItemTotal> withEmpty = ItemSearch.Arrange(Rows, "", BrowserSort.NameAscending, FavoriteList.Parse(""));
            List<ItemTotal> without = ItemSearch.Arrange(Rows, "", BrowserSort.NameAscending, null);

            Assert.Equal(Names(without), Names(withEmpty));
        }
    }
}
