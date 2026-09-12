using OreFinder.Detection;
using Xunit;

namespace OreFinder.Tests
{
    public class TargetGroupsTests
    {
        [Theory]
        [InlineData("ores", TargetGroup.Ore)]
        [InlineData("Ore", TargetGroup.Ore)]
        [InlineData("dungeons", TargetGroup.Dungeon)]
        [InlineData(" DUNGEON ", TargetGroup.Dungeon)]
        [InlineData("roots", TargetGroup.Root)]
        [InlineData("spawners", TargetGroup.Spawner)]
        [InlineData("spawner", TargetGroup.Spawner)]
        public void TryParse_AcceptsTheConsoleWords(string word, TargetGroup expected)
        {
            Assert.True(TargetGroups.TryParse(word, out TargetGroup group));
            Assert.Equal(expected, group);
        }

        [Theory]
        [InlineData("pickables")]
        [InlineData("trees")]
        [InlineData("on")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_RejectsListsAndOtherWords(string? word)
        {
            Assert.False(TargetGroups.TryParse(word, out _));
        }

        [Fact]
        public void SwitchWords_AllParse()
        {
            foreach (string word in TargetGroups.SwitchWords)
            {
                Assert.True(TargetGroups.TryParse(word, out _), word);
            }
        }

        [Fact]
        public void Label_NamesEveryGroup()
        {
            Assert.Equal("ores", TargetGroups.Label(TargetGroup.Ore));
            Assert.Equal("dungeon entrances", TargetGroups.Label(TargetGroup.Dungeon));
            Assert.Equal("spawners", TargetGroups.Label(TargetGroup.Spawner));
        }
    }
}
