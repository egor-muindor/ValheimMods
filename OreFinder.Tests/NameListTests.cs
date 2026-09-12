using OreFinder.Detection;
using Xunit;

namespace OreFinder.Tests
{
    public class NameListTests
    {
        [Fact]
        public void Parse_SplitsOnCommasSemicolonsAndWhitespace()
        {
            NameList list = NameList.Parse("DragonEgg, MushroomJotunPuffs;Fiddlehead\n VoltureEgg ");

            Assert.Equal(4, list.Count);
            Assert.True(list.Contains("DragonEgg"));
            Assert.True(list.Contains("VoltureEgg"));
            Assert.False(list.Contains("Thistle"));
        }

        [Fact]
        public void Contains_IsCaseInsensitive_AndAcceptsItemKeys()
        {
            NameList list = NameList.Parse("dragonegg");

            Assert.True(list.Contains("DragonEgg"));
            Assert.True(list.Contains("$item_dragonegg"));
            Assert.False(list.Contains(""));
        }

        [Fact]
        public void Empty_HasNoNames()
        {
            Assert.True(NameList.Parse(null).IsEmpty);
            Assert.True(NameList.Parse("  ").IsEmpty);
            Assert.Equal("none", NameList.Parse("").Describe());
            Assert.Equal("dragonegg", NameList.Parse("DragonEgg").Describe());
        }
    }
}
