using WorkshopRepair.Repair;
using Xunit;

namespace WorkshopRepair.Tests
{
    public class RepairRulesTests
    {
        private const string Workbench = "$piece_workbench";
        private const string Forge = "$piece_forge";

        [Fact]
        public void TheCraftingStationRepairsItsOwnItems()
        {
            Assert.True(RepairRules.StationRepairs(Forge, 1, Forge, null, 1));
        }

        [Fact]
        public void TheRepairStationRepairsWhatIsCraftedElsewhere()
        {
            Assert.True(RepairRules.StationRepairs(Forge, 1, "$piece_cauldron", Forge, 1));
        }

        [Fact]
        public void AnotherKindOfStationDoesNot()
        {
            Assert.False(RepairRules.StationRepairs(Workbench, 5, Forge, null, 1));
        }

        [Fact]
        public void AnItemWithoutAStationIsNeverRepaired()
        {
            Assert.False(RepairRules.StationRepairs(Workbench, 5, null, null, 1));
        }

        [Fact]
        public void TheStationMustBeUpgradedFarEnough()
        {
            Assert.False(RepairRules.StationRepairs(Forge, 2, Forge, null, 3));
            Assert.True(RepairRules.StationRepairs(Forge, 3, Forge, null, 3));
        }

        [Fact]
        public void LevelsAboveFourCountAsFour()
        {
            Assert.Equal(4, RepairRules.CountedLevel(7));
            Assert.False(RepairRules.StationRepairs(Forge, 7, Forge, null, 5));
        }

        [Fact]
        public void AStationWithoutANameRepairsNothing()
        {
            Assert.False(RepairRules.StationRepairs("", 1, null, null, 0));
        }
    }
}
