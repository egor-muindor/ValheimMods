using TidyChests.Index;
using Xunit;

namespace TidyChests.Tests
{
    public class FavoriteListTests
    {
        [Fact]
        public void EmptyConfigLineMeansNothingPinned()
        {
            Assert.Equal(0, FavoriteList.Parse("").Count);
            Assert.Equal(0, FavoriteList.Parse(null).Count);
            Assert.Equal(0, FavoriteList.Parse("  ,  , ").Count);
        }

        [Fact]
        public void KeepsTheOrderItemsWerePinnedIn()
        {
            FavoriteList list = FavoriteList.Parse("$item_wood, $item_coal");
            list.Add("$item_iron");

            Assert.Equal(new[] { "$item_wood", "$item_coal", "$item_iron" }, list.Names);
            Assert.Equal("$item_wood, $item_coal, $item_iron", list.Format());
        }

        [Fact]
        public void TrimsAndDropsRepeats()
        {
            FavoriteList list = FavoriteList.Parse("  $item_wood ,$item_wood,  $item_coal  ");

            Assert.Equal(new[] { "$item_wood", "$item_coal" }, list.Names);
        }

        [Fact]
        public void TogglePinsThenUnpins()
        {
            var list = FavoriteList.Parse("");

            Assert.True(list.Toggle("$item_wood"));
            Assert.True(list.Contains("$item_wood"));

            Assert.False(list.Toggle("$item_wood"));
            Assert.False(list.Contains("$item_wood"));
            Assert.Equal("", list.Format());
        }

        [Fact]
        public void UnpinningLeavesTheOthersInOrder()
        {
            FavoriteList list = FavoriteList.Parse("a, b, c");

            Assert.True(list.Remove("b"));
            Assert.Equal(new[] { "a", "c" }, list.Names);
            Assert.False(list.Remove("b"));
        }

        [Fact]
        public void AHandEditedLineMayDifferInCase()
        {
            FavoriteList list = FavoriteList.Parse("$Item_Wood");

            Assert.True(list.Contains("$item_wood"));
            Assert.False(list.Add("$item_wood"));
            Assert.True(list.Remove("$item_wood"));
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void IgnoresEmptyNames()
        {
            var list = FavoriteList.Parse("");

            Assert.False(list.Add(""));
            Assert.False(list.Add(null));
            Assert.False(list.Contains(""));
            Assert.False(list.Contains(null));
            Assert.Equal(0, list.Count);
        }
    }
}
