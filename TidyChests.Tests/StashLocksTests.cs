using TidyChests.Stash;
using Xunit;

namespace TidyChests.Tests
{
    public class StashLocksTests
    {
        [Fact]
        public void EmptyConfigMeansNothingLocked()
        {
            StashLocks locks = StashLocks.Parse(null, "");
            Assert.Empty(locks.Items);
            Assert.Empty(locks.Slots);
            Assert.False(locks.IsItemLocked("$item_wood"));
            Assert.False(locks.IsSlotLocked(0, 0));
        }

        [Fact]
        public void ParsesItemsAndSlotsKeepingTheOrder()
        {
            StashLocks locks = StashLocks.Parse(" $item_wood ,$item_coal", "3:1, 0:0");

            Assert.Equal(new[] { "$item_wood", "$item_coal" }, locks.Items);
            Assert.Equal(new[] { new StashLocks.SlotRef(3, 1), new StashLocks.SlotRef(0, 0) }, locks.Slots);
            Assert.True(locks.IsItemLocked("$item_coal"));
            Assert.True(locks.IsItemLocked("$ITEM_COAL"));
            Assert.True(locks.IsSlotLocked(3, 1));
            Assert.False(locks.IsSlotLocked(1, 3));
        }

        [Fact]
        public void DropsRepeatsAndMalformedSlots()
        {
            StashLocks locks = StashLocks.Parse("$item_wood, $item_wood, ,", "1:1, 1:1, x:2, 3, -1:0, 2:");

            Assert.Equal(new[] { "$item_wood" }, locks.Items);
            Assert.Equal(new[] { new StashLocks.SlotRef(1, 1) }, locks.Slots);
        }

        [Fact]
        public void ToggleFlipsTheStateAndReportsIt()
        {
            StashLocks locks = StashLocks.Parse("", "");

            Assert.True(locks.ToggleItem("$item_wood"));
            Assert.True(locks.IsItemLocked("$item_wood"));
            Assert.False(locks.ToggleItem("$item_wood"));
            Assert.False(locks.IsItemLocked("$item_wood"));

            Assert.True(locks.ToggleSlot(2, 3));
            Assert.True(locks.IsSlotLocked(2, 3));
            Assert.False(locks.ToggleSlot(2, 3));
            Assert.False(locks.IsSlotLocked(2, 3));
        }

        [Fact]
        public void FormatsBackIntoConfigLines()
        {
            StashLocks locks = StashLocks.Parse("$item_wood, $item_coal", "3:1, 0:0");
            locks.UnlockItem("$item_wood");
            locks.LockSlot(7, 2);

            Assert.Equal("$item_coal", locks.FormatItems());
            Assert.Equal("3:1, 0:0, 7:2", locks.FormatSlots());
            Assert.Equal("", StashLocks.Parse("", "").FormatSlots());
        }

        [Fact]
        public void EmptyNameAndNegativeSlotAreRefused()
        {
            StashLocks locks = StashLocks.Parse("", "");

            Assert.False(locks.ToggleItem(""));
            Assert.False(locks.ToggleItem(null));
            Assert.False(locks.LockSlot(-1, 0));
            Assert.Empty(locks.Items);
            Assert.Empty(locks.Slots);
        }
    }
}
