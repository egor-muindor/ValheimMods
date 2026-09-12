using System.Collections.Generic;
using System.Linq;
using TidyChests.Stash;
using Xunit;

namespace TidyChests.Tests
{
    public class StashPlannerTests
    {
        private static ItemSnapshot Item(int index, string name, int count, int maxStack = 50, int quality = 1, int worldLevel = 0)
        {
            return new ItemSnapshot(index, name, quality, worldLevel, count, maxStack);
        }

        private static StackSnapshot Stack(string name, int count, int maxStack = 50, int quality = 1, int worldLevel = 0)
        {
            return new StackSnapshot(name, quality, worldLevel, count, maxStack);
        }

        private static ContainerSnapshot Chest(int index, float distance, int emptySlots, params StackSnapshot[] stacks)
        {
            return new ContainerSnapshot(index, $"chest {index}", distance, stacks.ToList(), emptySlots);
        }

        [Fact]
        public void ItemGoesOnlyToChestsThatAlreadyHoldIt()
        {
            var chests = new[]
            {
                Chest(0, 1f, 10, Stack("stone", 10)),
                Chest(1, 5f, 10, Stack("wood", 10)),
            };

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 30) }, chests);

            StashMove move = Assert.Single(plan.Moves);
            Assert.Equal(1, move.Container);
            Assert.Equal(30, move.Amount);
            Assert.Equal(30, plan.Units);
            Assert.Equal(1, plan.ItemsTouched);
            Assert.Equal(1, plan.ContainersUsed);
        }

        [Fact]
        public void NothingHappensWhenNoChestHoldsTheItem()
        {
            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 30) }, new[] { Chest(0, 1f, 10, Stack("stone", 10)) });

            Assert.Empty(plan.Moves);
            Assert.Equal(0, plan.Units);
        }

        [Fact]
        public void NearestChestIsFilledFirst_ThenTheNext()
        {
            var chests = new[]
            {
                Chest(0, 8f, 0, Stack("wood", 45)),
                Chest(1, 2f, 0, Stack("wood", 40)),
            };

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 12) }, chests);

            Assert.Equal(2, plan.Moves.Count);
            Assert.Equal(1, plan.Moves[0].Container);
            Assert.Equal(10, plan.Moves[0].Amount);
            Assert.Equal(0, plan.Moves[1].Container);
            Assert.Equal(2, plan.Moves[1].Amount);
            Assert.Equal(12, plan.Units);
            Assert.Equal(2, plan.ContainersUsed);
        }

        [Fact]
        public void ExistingStacksAreToppedUpBeforeEmptySlots()
        {
            ContainerSnapshot chest = Chest(0, 1f, 1, Stack("wood", 30));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 50) }, new[] { chest });

            Assert.Equal(50, Assert.Single(plan.Moves).Amount);
            Assert.Equal(50, chest.Stacks[0].Count);
            Assert.Equal(30, chest.Stacks[1].Count);
            Assert.Equal(0, chest.EmptySlots);
        }

        [Fact]
        public void LeftoverStaysWhenTheChestIsFull()
        {
            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 50) }, new[] { Chest(0, 1f, 0, Stack("wood", 45)) });

            Assert.Equal(5, Assert.Single(plan.Moves).Amount);
            Assert.Equal(5, plan.Units);
        }

        [Fact]
        public void EmptySlotsAreSharedBetweenItems()
        {
            ContainerSnapshot chest = Chest(0, 1f, 1, Stack("wood", 50), Stack("stone", 50));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 20), Item(1, "stone", 20) }, new[] { chest });

            StashMove move = Assert.Single(plan.Moves);
            Assert.Equal(0, move.Item);
            Assert.Equal(20, move.Amount);
            Assert.Equal(0, chest.EmptySlots);
        }

        [Fact]
        public void ABigStackSpansSeveralEmptySlots()
        {
            ContainerSnapshot chest = Chest(0, 1f, 3, Stack("wood", 50));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 120) }, new[] { chest });

            Assert.Equal(120, Assert.Single(plan.Moves).Amount);
            Assert.Equal(0, chest.EmptySlots);
            Assert.Equal(new[] { 50, 50, 50, 20 }, chest.Stacks.Select(stack => stack.Count));
        }

        [Fact]
        public void DifferentQualityNeverMergesIntoAStack_ButTheNameStillQualifiesTheChest()
        {
            ContainerSnapshot chest = Chest(0, 1f, 1, Stack("mead", 5, maxStack: 10, quality: 1));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "mead", 8, maxStack: 10, quality: 2) }, new[] { chest });

            Assert.Equal(8, Assert.Single(plan.Moves).Amount);
            Assert.Equal(5, chest.Stacks[0].Count);
            Assert.Equal(8, chest.Stacks[1].Count);
            Assert.Equal(2, chest.Stacks[1].Quality);
        }

        [Fact]
        public void DifferentWorldLevelNeverMergesIntoAStack()
        {
            ContainerSnapshot chest = Chest(0, 1f, 0, Stack("wood", 10, worldLevel: 1));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 10, worldLevel: 0) }, new[] { chest });

            Assert.Empty(plan.Moves);
        }

        [Fact]
        public void ANewStackPlacedByThePlannerAcceptsLaterItemsOfTheSameKind()
        {
            ContainerSnapshot chest = Chest(0, 1f, 1, Stack("wood", 50));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 10), Item(1, "wood", 10) }, new[] { chest });

            Assert.Equal(2, plan.Moves.Count);
            Assert.Equal(20, plan.Units);
            Assert.Equal(2, plan.ItemsTouched);
            Assert.Equal(20, chest.Stacks[1].Count);
            Assert.Equal(0, chest.EmptySlots);
        }

        [Fact]
        public void ItemsAreHandledInTheOrderGiven()
        {
            ContainerSnapshot chest = Chest(0, 1f, 0, Stack("wood", 40));

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 8), Item(1, "wood", 8) }, new[] { chest });

            Assert.Equal(2, plan.Moves.Count);
            Assert.Equal(8, plan.Moves[0].Amount);
            Assert.Equal(0, plan.Moves[0].Item);
            Assert.Equal(2, plan.Moves[1].Amount);
            Assert.Equal(1, plan.Moves[1].Item);
        }

        [Fact]
        public void ChestOrderIsByDistance_NotByIndex()
        {
            var chests = new[]
            {
                Chest(0, 9f, 5, Stack("wood", 1)),
                Chest(1, 3f, 5, Stack("wood", 1)),
                Chest(2, 6f, 5, Stack("wood", 1)),
            };

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 10) }, chests);

            Assert.Equal(1, Assert.Single(plan.Moves).Container);
        }

        [Fact]
        public void ChestWithNoSpaceIsSkippedWithoutAMove()
        {
            var chests = new[]
            {
                Chest(0, 1f, 0, Stack("wood", 50)),
                Chest(1, 2f, 1, Stack("wood", 50)),
            };

            StashPlan plan = StashPlanner.Plan(new[] { Item(0, "wood", 10) }, chests);

            StashMove move = Assert.Single(plan.Moves);
            Assert.Equal(1, move.Container);
            Assert.Equal(10, move.Amount);
        }

        [Fact]
        public void NoItemsOrNoChestsGiveAnEmptyPlan()
        {
            Assert.Empty(StashPlanner.Plan(new List<ItemSnapshot>(), new[] { Chest(0, 1f, 5, Stack("wood", 1)) }).Moves);
            Assert.Empty(StashPlanner.Plan(new[] { Item(0, "wood", 10) }, new List<ContainerSnapshot>()).Moves);
        }
    }
}
