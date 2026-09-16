using CombatStats.Collect;
using Xunit;

namespace CombatStats.Tests
{
    /// <summary>
    /// Fire, poison and spirit tick on the target long after the blow, and the game hands the
    /// tick over without an attacker. This table remembers who lit the target up.
    /// </summary>
    public class DotAttributionTests
    {
        private const long Target = 4242;

        private const long Sigrun = 11;

        private const long Bjorn = 22;

        [Fact]
        public void AnUnknownTargetResolvesToNothing()
        {
            var table = new DotAttribution(30d);

            Assert.False(table.TryResolve(0d, Target, out long attacker));
            Assert.Equal(0L, attacker);
        }

        [Fact]
        public void TheAttackerIsReturnedWhileTheEntryLives()
        {
            var table = new DotAttribution(30d);
            table.Remember(100d, Target, Sigrun);

            Assert.True(table.TryResolve(129.9d, Target, out long attacker));
            Assert.Equal(Sigrun, attacker);
        }

        [Fact]
        public void AnEntryExpiresAfterItsTimeToLive()
        {
            var table = new DotAttribution(30d);
            table.Remember(100d, Target, Sigrun);

            Assert.False(table.TryResolve(130.1d, Target, out _));
        }

        [Fact]
        public void TheLastAttackerWinsAndRefreshesTheDeadline()
        {
            var table = new DotAttribution(30d);
            table.Remember(100d, Target, Sigrun);
            table.Remember(120d, Target, Bjorn);

            Assert.True(table.TryResolve(149d, Target, out long attacker));
            Assert.Equal(Bjorn, attacker);
        }

        [Fact]
        public void PruneDropsOnlyExpiredEntries()
        {
            var table = new DotAttribution(30d);
            table.Remember(100d, Target, Sigrun);
            table.Remember(150d, Target + 1, Bjorn);

            table.Prune(151d);

            Assert.Equal(1, table.Count);
            Assert.True(table.TryResolve(151d, Target + 1, out long attacker));
            Assert.Equal(Bjorn, attacker);
        }

        [Fact]
        public void TargetsAreIndependentOfEachOther()
        {
            var table = new DotAttribution(30d);
            table.Remember(100d, Target, Sigrun);
            table.Remember(100d, Target + 1, Bjorn);

            Assert.True(table.TryResolve(110d, Target, out long first));
            Assert.True(table.TryResolve(110d, Target + 1, out long second));
            Assert.Equal(Sigrun, first);
            Assert.Equal(Bjorn, second);
        }
    }
}
