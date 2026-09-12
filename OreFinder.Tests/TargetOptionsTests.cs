using OreFinder.Detection;
using Xunit;

namespace OreFinder.Tests
{
    public class TargetOptionsTests
    {
        [Fact]
        public void Defaults_LookForOresDungeonsRootsAndSpawners()
        {
            var options = new TargetOptions();

            Assert.True(options.Ores);
            Assert.True(options.Dungeons);
            Assert.True(options.Roots);
            Assert.True(options.Spawners);
            Assert.True(options.Pickables.IsEmpty);
            Assert.True(options.Trees.IsEmpty);
        }

        [Fact]
        public void Describe_ListsTheGroupsThatAreOn()
        {
            var options = new TargetOptions
            {
                Ores = true,
                Dungeons = true,
                Roots = false,
                Spawners = true,
                Pickables = NameList.Parse("Pickable_DragonEgg"),
                Trees = NameList.Parse(string.Empty),
            };

            Assert.Equal("ores, dungeon entrances, spawners, pickables (pickable_dragonegg)", options.Describe());
        }

        [Fact]
        public void Describe_WithOresOffOnly_SaysSo()
        {
            var options = new TargetOptions
            {
                Ores = false,
                Dungeons = true,
                Roots = false,
                Spawners = false,
            };

            Assert.Equal("dungeon entrances", options.Describe());
        }

        [Fact]
        public void Describe_WithEverythingOff_SaysNone()
        {
            var options = new TargetOptions
            {
                Ores = false,
                Dungeons = false,
                Roots = false,
                Spawners = false,
            };

            Assert.Equal("none", options.Describe());
        }
    }
}
