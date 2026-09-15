using System.Collections.Generic;
using TidyChests.Knowledge;
using Xunit;

namespace TidyChests.Tests
{
    public class KnowledgePlanTests
    {
        private static HashSet<string> Set(params string[] names)
        {
            return new HashSet<string>(names);
        }

        [Fact]
        public void KnownItemsAreLeftOut()
        {
            List<string> batch = KnowledgePlan.NextBatch(new[] { "wood", "iron" }, Set("wood"), Set(), 10);

            Assert.Equal(new[] { "iron" }, batch);
        }

        [Fact]
        public void SkippedItemsAreLeftOut()
        {
            List<string> batch = KnowledgePlan.NextBatch(new[] { "wood", "iron" }, Set(), Set("iron"), 10);

            Assert.Equal(new[] { "wood" }, batch);
        }

        [Fact]
        public void DuplicatesAppearOnce()
        {
            List<string> batch = KnowledgePlan.NextBatch(new[] { "wood", "wood" }, Set(), Set(), 10);

            Assert.Equal(new[] { "wood" }, batch);
        }

        [Fact]
        public void TheBatchIsSortedSoUnlockMessagesKeepTheirOrder()
        {
            List<string> batch = KnowledgePlan.NextBatch(new[] { "wood", "coal", "iron" }, Set(), Set(), 10);

            Assert.Equal(new[] { "coal", "iron", "wood" }, batch);
        }

        [Fact]
        public void TheBatchIsCapped()
        {
            List<string> batch = KnowledgePlan.NextBatch(new[] { "wood", "coal", "iron" }, Set(), Set(), 2);

            Assert.Equal(new[] { "coal", "iron" }, batch);
        }

        [Fact]
        public void ACapOfZeroLearnsNothing()
        {
            Assert.Empty(KnowledgePlan.NextBatch(new[] { "wood" }, Set(), Set(), 0));
        }

        [Fact]
        public void EmptyAndNullNamesAreIgnored()
        {
            List<string> batch = KnowledgePlan.NextBatch(new[] { "", null!, "wood" }, Set(), Set(), 10);

            Assert.Equal(new[] { "wood" }, batch);
        }

        [Fact]
        public void NothingNearbyMeansNothingToLearn()
        {
            Assert.Empty(KnowledgePlan.NextBatch(new string[0], Set(), Set(), 10));
        }
    }
}
