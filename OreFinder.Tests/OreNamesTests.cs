using OreFinder.Detection;
using Xunit;

namespace OreFinder.Tests
{
    public class OreNamesTests
    {
        private const string Defaults = "CopperOre=C, TinOre=T, SilverOre=S, IronScrap=I, FlametalOre=F, FlametalOreNew=F, Obsidian=O";

        [Fact]
        public void Parse_ReadsTheDefaultPairs()
        {
            OreNames names = OreNames.Parse(Defaults);

            Assert.Equal(7, names.Count);
            Assert.True(names.TryGet("CopperOre", "rock4_copper", out string name));
            Assert.Equal("C", name);
            Assert.True(names.TryGet("FlametalOreNew", "MineRock_Meteorite", out name));
            Assert.Equal("F", name);
        }

        [Fact]
        public void EmptyOrBlank_HasNoNames()
        {
            Assert.True(OreNames.Parse(null).IsEmpty);
            Assert.True(OreNames.Parse("  ").IsEmpty);
            Assert.Equal("none", OreNames.Parse("").Describe());
        }

        [Fact]
        public void Keys_AreCaseInsensitive_AndAcceptLocalisationKeys()
        {
            OreNames names = OreNames.Parse("copperore=Cu; $item_tinore=Sn");

            Assert.True(names.TryGet("CopperOre", "rock4_copper", out string copper));
            Assert.Equal("Cu", copper);
            Assert.True(names.TryGet("$item_tinore", "MineRock_Tin", out string tin));
            Assert.Equal("Sn", tin);
        }

        [Fact]
        public void ObjectPrefabName_IsUsedWhenTheItemHasNoName()
        {
            OreNames names = OreNames.Parse("silvervein=Ag");

            Assert.True(names.TryGet("SilverOre", "silvervein", out string name));
            Assert.Equal("Ag", name);
            Assert.False(names.TryGet("SilverOre", "MineRock_Silver", out _));
        }

        [Fact]
        public void ItemName_WinsOverObjectName()
        {
            OreNames names = OreNames.Parse("SilverOre=S, silvervein=Vein");

            Assert.True(names.TryGet("SilverOre", "silvervein", out string name));
            Assert.Equal("S", name);
        }

        [Fact]
        public void Values_KeepSpaces_AndAcceptColons()
        {
            OreNames names = OreNames.Parse("CopperOre: Copper vein\nTinOre = Tin node ");

            Assert.True(names.TryGet("CopperOre", "", out string copper));
            Assert.Equal("Copper vein", copper);
            Assert.True(names.TryGet("TinOre", "", out string tin));
            Assert.Equal("Tin node", tin);
        }

        [Fact]
        public void BrokenEntries_AreIgnored()
        {
            OreNames names = OreNames.Parse("CopperOre, =X, TinOre=, SilverOre=S");

            Assert.Equal(1, names.Count);
            Assert.False(names.TryGet("CopperOre", "", out _));
            Assert.True(names.TryGet("SilverOre", "", out string name));
            Assert.Equal("S", name);
        }

        [Fact]
        public void DuplicateKey_KeepsTheLastName()
        {
            OreNames names = OreNames.Parse("CopperOre=A, CopperOre=B");

            Assert.True(names.TryGet("CopperOre", "", out string name));
            Assert.Equal("B", name);
            Assert.Equal("copperore=B", names.Describe());
        }
    }
}
