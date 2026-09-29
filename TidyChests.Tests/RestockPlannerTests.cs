using System.Collections.Generic;
using System.Linq;
using TidyChests.Restock;
using Xunit;

namespace TidyChests.Tests
{
    public class RestockPlannerTests
    {
        private const string Arrow = "$item_arrow_wood";

        private const string Meat = "$item_cookedmeat";

        private static RestockTarget Target(int index, string name = Arrow, int count = 10, int maxStack = 100, int percent = 100, int quality = 1, int worldLevel = 0)
        {
            return new RestockTarget(index, name, quality, worldLevel, count, maxStack, percent);
        }

        private static RestockStack Stack(int index, string name = Arrow, int count = 100, int maxStack = 100, int quality = 1, int worldLevel = 0)
        {
            return new RestockStack(index, name, quality, worldLevel, count, maxStack);
        }

        private static RestockSource Source(int index, float distance, params RestockStack[] stacks)
        {
            return new RestockSource(index, distance, stacks.ToList());
        }

        [Theory]
        [InlineData(100, 100, 100)]
        [InlineData(100, 50, 50)]
        [InlineData(25, 50, 13)]
        [InlineData(1, 100, 0)]
        [InlineData(20, 1, 1)]
        [InlineData(50, 0, 0)]
        public void WantedRoundsUpAndIgnoresUnstackable(int maxStack, int percent, int wanted)
        {
            Assert.Equal(wanted, RestockLevels.Wanted(maxStack, percent));
        }

        [Theory]
        [InlineData("Consumable", true, RestockKind.Food)]
        [InlineData("Consumable", false, RestockKind.Mead)]
        [InlineData("Ammo", false, RestockKind.Ammo)]
        [InlineData("AmmoNonEquipable", false, RestockKind.Ammo)]
        [InlineData("Material", false, RestockKind.Other)]
        public void KindsFollowTheItemType(string type, bool feeds, RestockKind kind)
        {
            Assert.Equal(kind, RestockLevels.KindOf(type, feeds));
        }

        [Fact]
        public void DefaultLevelsFillFoodHalfAndAmmoFull()
        {
            var levels = new RestockLevels(50, 0, 100, 0);
            Assert.Equal(50, levels.PercentFor(RestockKind.Food));
            Assert.Equal(0, levels.PercentFor(RestockKind.Mead));
            Assert.Equal(100, levels.PercentFor(RestockKind.Ammo));
            Assert.Equal(0, levels.PercentFor(RestockKind.Other));
        }

        [Fact]
        public void TopsUpToTheLevelFromTheNearestChest()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, count: 30) },
                new[] { Source(0, 8f, Stack(0)), Source(1, 2f, Stack(0)) });

            RestockMove move = Assert.Single(plan.Moves);
            Assert.Equal(1, move.Source);
            Assert.Equal(70, move.Amount);
            Assert.Equal(70, plan.Units);
            Assert.Equal(1, plan.SlotsFilled);
        }

        [Fact]
        public void HalfLevelTakesOnlyWhatIsMissing()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, Meat, count: 3, maxStack: 20, percent: 50) },
                new[] { Source(0, 1f, Stack(0, Meat, count: 20, maxStack: 20)) });

            Assert.Equal(7, Assert.Single(plan.Moves).Amount);
        }

        [Fact]
        public void SlotAtItsLevelIsCountedAsStocked()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, Meat, count: 12, maxStack: 20, percent: 50) },
                new[] { Source(0, 1f, Stack(0, Meat, maxStack: 20)) });

            Assert.Empty(plan.Moves);
            Assert.Equal(1, plan.AlreadyStocked);
            Assert.Equal(0, plan.NotFound);
        }

        [Fact]
        public void PrefersOneStackThatCoversTheShortfall()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, count: 0) },
                new[] { Source(0, 1f, Stack(0, count: 20)), Source(1, 5f, Stack(3, count: 100)) });

            Assert.Equal(new[] { (1, 3, 100) }, plan.Moves.Select(move => (move.Source, move.Stack, move.Amount)));
        }

        [Fact]
        public void CombinesSmallStacksNearestFirstWhenNoneCovers()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, count: 0) },
                new[] { Source(0, 9f, Stack(0, count: 40)), Source(1, 1f, Stack(0, count: 30), Stack(1, count: 20)) });

            Assert.Equal(new[] { (1, 0, 30), (1, 1, 20), (0, 0, 40) }, plan.Moves.Select(move => (move.Source, move.Stack, move.Amount)));
            Assert.Equal(90, plan.Units);
        }

        [Fact]
        public void OnlyTakesStacksThatStackWithTheSlot()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, count: 50, worldLevel: 1) },
                new[] { Source(0, 1f, Stack(0, worldLevel: 0), Stack(1, "$item_arrow_fire"), Stack(2, count: 10, worldLevel: 1)) });

            RestockMove move = Assert.Single(plan.Moves);
            Assert.Equal(2, move.Stack);
            Assert.Equal(10, move.Amount);
        }

        [Fact]
        public void EmptySlotTakesTheFirstStackFoundAndSticksToItsWorldLevel()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { RestockTarget.Empty(0, Meat, 50) },
                new[] { Source(0, 1f, Stack(0, Meat, count: 4, maxStack: 20, worldLevel: 2), Stack(1, Meat, count: 20, maxStack: 20, worldLevel: 0), Stack(2, Meat, count: 3, maxStack: 20, worldLevel: 2)) });

            Assert.Equal(new[] { (0, 4), (2, 3) }, plan.Moves.Select(move => (move.Stack, move.Amount)));
        }

        [Fact]
        public void TwoSlotsOfTheSameItemShareTheChests()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, count: 60), Target(1, count: 60) },
                new[] { Source(0, 1f, Stack(0, count: 50)) });

            Assert.Equal(new[] { (0, 40), (1, 10) }, plan.Moves.Select(move => (move.Target, move.Amount)));
            Assert.Equal(2, plan.SlotsFilled);
        }

        [Fact]
        public void ReportsSlotsNoChestCanFill()
        {
            RestockPlan plan = RestockPlanner.Plan(
                new[] { Target(0, count: 1), Target(1, Meat, count: 20, maxStack: 20) },
                new List<RestockSource> { Source(0, 1f, Stack(0, "$item_wood", maxStack: 50)) });

            Assert.Empty(plan.Moves);
            Assert.Equal(1, plan.NotFound);
            Assert.Equal(1, plan.AlreadyStocked);
        }
    }
}
