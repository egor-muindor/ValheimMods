using OreFinder.Detection;
using Xunit;

namespace OreFinder.Tests
{
    public class OreFilterTests
    {
        private static readonly string[] CopperDeposit = { "CopperOre", "$item_copperore", "Stone", "$item_stone" };

        private static readonly string[] PlainRock = { "Stone", "$item_stone" };

        [Theory]
        [InlineData("CopperOre")]
        [InlineData("TinOre")]
        [InlineData("SilverOre")]
        [InlineData("IronScrap")]
        [InlineData("FlametalOre")]
        [InlineData("FlametalOreNew")]
        [InlineData("copper_ore")]
        [InlineData("$item_iron_scrap")]
        public void IsOreItemName_AcceptsOreAndScrapWords(string name)
        {
            Assert.True(OreFilter.IsOreItemName(name));
        }

        [Theory]
        [InlineData("Stone")]
        [InlineData("Obsidian")]
        [InlineData("BlackMarble")]
        [InlineData("LeatherScraps")]
        [InlineData("ShieldCore")]
        [InlineData("$item_copperore")]
        [InlineData("Softtissue")]
        [InlineData("")]
        public void IsOreItemName_RejectsOtherNames(string name)
        {
            Assert.False(OreFilter.IsOreItemName(name));
        }

        [Fact]
        public void EmptyList_MeansAllOres()
        {
            OreFilter filter = OreFilter.Parse("  ");

            Assert.True(filter.IsAllOres);
            Assert.Equal("all ores", filter.Describe());
        }

        [Fact]
        public void AllOres_MatchesByOreDrop()
        {
            OreFilter filter = OreFilter.Parse(null);

            Assert.True(filter.TryMatch("rock4_copper", CopperDeposit, out string oreItem));
            Assert.Equal("CopperOre", oreItem);
        }

        [Fact]
        public void AllOres_IgnoresPlainRocks()
        {
            OreFilter filter = OreFilter.Parse("");

            Assert.False(filter.TryMatch("rock_destructible", PlainRock, out string oreItem));
            Assert.Equal("", oreItem);
        }

        [Fact]
        public void AllOres_IgnoresObsidianAndLeatherScraps()
        {
            OreFilter filter = OreFilter.Parse("");

            Assert.False(filter.TryMatch("MineRock_Obsidian", new[] { "Obsidian", "$item_obsidian" }, out _));
            Assert.False(filter.TryMatch("mudpile_old", new[] { "LeatherScraps", "$item_leatherscraps" }, out _));
        }

        [Fact]
        public void AllOres_MatchesScrapPiles()
        {
            OreFilter filter = OreFilter.Parse("");

            Assert.True(filter.TryMatch("mudpile2", new[] { "IronScrap", "$item_ironscrap", "WitheredBone", "$item_witheredbone" }, out string oreItem));
            Assert.Equal("IronScrap", oreItem);
        }

        [Fact]
        public void List_MatchesItemNamesCaseInsensitive()
        {
            OreFilter filter = OreFilter.Parse("copperore, TinOre");

            Assert.False(filter.IsAllOres);
            Assert.True(filter.TryMatch("rock4_copper", CopperDeposit, out string oreItem));
            Assert.Equal("CopperOre", oreItem);
            Assert.False(filter.TryMatch("silvervein", new[] { "SilverOre", "$item_silverore", "Stone" }, out _));
        }

        [Fact]
        public void List_AcceptsLocalisationKeys()
        {
            OreFilter filter = OreFilter.Parse("$item_silverore");

            Assert.True(filter.TryMatch("silvervein", new[] { "SilverOre", "$item_silverore" }, out string oreItem));
            Assert.Equal("SilverOre", oreItem);
        }

        [Fact]
        public void List_MatchesObjectPrefabName_AndNamesItAfterItsDrop()
        {
            OreFilter filter = OreFilter.Parse("MineRock_Obsidian");

            Assert.True(filter.TryMatch("MineRock_Obsidian", new[] { "Obsidian", "$item_obsidian" }, out string oreItem));
            Assert.Equal("Obsidian", oreItem);
        }

        [Fact]
        public void List_ObjectWithoutDrops_IsNamedAfterItself()
        {
            OreFilter filter = OreFilter.Parse("giant_brain");

            Assert.True(filter.TryMatch("giant_brain", new string[0], out string oreItem));
            Assert.Equal("giant_brain", oreItem);
        }

        [Fact]
        public void List_DoesNotFallBackToTheOreRule()
        {
            OreFilter filter = OreFilter.Parse("TinOre");

            Assert.False(filter.TryMatch("rock4_copper", CopperDeposit, out _));
        }

        [Fact]
        public void Parse_AcceptsSeveralSeparators()
        {
            OreFilter filter = OreFilter.Parse("CopperOre;TinOre SilverOre,\n IronScrap ");

            Assert.Equal("copperore, tinore, silverore, ironscrap", filter.Describe());
        }
    }
}
