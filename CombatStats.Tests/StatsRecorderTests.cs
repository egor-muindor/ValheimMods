using System;
using System.Linq;
using CombatStats.Model;
using CombatStats.Stats;
using Xunit;

namespace CombatStats.Tests
{
    /// <summary>
    /// The ring of one-second buckets every window is summed from: what belongs in a window,
    /// what falls out of it, and what a bucket holds after the ring has lapped.
    /// </summary>
    public class StatsRecorderTests
    {
        private const long Me = 1;

        private const long Sigrun = 2;

        private static float[] Hit(DamageKind kind, float amount)
        {
            var byKind = new float[DamageKinds.Count];
            byKind[(int)kind] = amount;
            return byKind;
        }

        private static float[] Hit(DamageKind first, float firstAmount, DamageKind second, float secondAmount)
        {
            float[] byKind = Hit(first, firstAmount);
            byKind[(int)second] = secondAmount;
            return byKind;
        }

        private static string Named(long id)
        {
            return id == Me ? "Muindor" : "Sigrun";
        }

        [Fact]
        public void EventsInsideTheWindowAreSummed()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(10d, Me, Hit(DamageKind.Slash, 30f), estimated: false);
            recorder.Record(11d, Me, Hit(DamageKind.Slash, 20f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(12d, 30, Named);

            Assert.Equal(50f, snapshot.Total, 3);
            Assert.Equal(2, snapshot.Hits);
            Assert.Single(snapshot.Rows);
            Assert.Equal("Muindor", snapshot.Rows[0].Name);
        }

        [Fact]
        public void TheWindowEndsExactlyAfterItsSeconds()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(0.5d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            Assert.Equal(30f, recorder.Snapshot(29.9d, 30, Named).Total, 3);
            Assert.Equal(0f, recorder.Snapshot(30d, 30, Named).Total, 3);
        }

        [Fact]
        public void ABucketReusedAfterAFullLapStartsEmpty()
        {
            var recorder = new StatsRecorder(60);
            recorder.Record(0.5d, Me, Hit(DamageKind.Slash, 100f), estimated: false);
            recorder.Record(60.5d, Me, Hit(DamageKind.Slash, 7f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(60.9d, 60, Named);

            Assert.Equal(7f, snapshot.Total, 3);
            Assert.Equal(1, snapshot.Hits);
        }

        [Fact]
        public void AverageIsDamagePerHitNotPerSecond()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);
            recorder.Record(2d, Me, Hit(DamageKind.Slash, 30f), estimated: false);
            recorder.Record(3d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(299d, 300, Named);

            Assert.Equal(90f, snapshot.Total, 3);
            Assert.Equal(30f, snapshot.Average, 3);
            Assert.Equal(30f, snapshot.Rows[0].Average, 3);
        }

        [Fact]
        public void TheLargestSingleHitSurvivesAggregation()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f, DamageKind.Fire, 12f), estimated: false);
            recorder.Record(2d, Me, Hit(DamageKind.Slash, 20f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(3d, 30, Named);

            Assert.Equal(42f, snapshot.Max, 3);
            Assert.Equal(42f, snapshot.Rows[0].Max, 3);
        }

        [Fact]
        public void RowsAreSortedByDamageAndSharesAddUpToOne()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);
            recorder.Record(1d, Sigrun, Hit(DamageKind.Pierce, 70f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(2d, 30, Named);

            Assert.Equal(new[] { Sigrun, Me }, snapshot.Rows.Select(row => row.Id).ToArray());
            Assert.Equal(0.7f, snapshot.Rows[0].Share, 3);
            Assert.Equal(1f, snapshot.Rows.Sum(row => row.Share), 3);
        }

        [Fact]
        public void PerKindTotalsMatchTheRowTotals()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f, DamageKind.Fire, 10f), estimated: false);
            recorder.Record(1d, Sigrun, Hit(DamageKind.Slash, 5f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(2d, 30, Named);

            Assert.Equal(35f, snapshot.TotalByKind[(int)DamageKind.Slash], 3);
            Assert.Equal(10f, snapshot.TotalByKind[(int)DamageKind.Fire], 3);
            Assert.Equal(30f, snapshot.Rows.Single(row => row.Id == Me).ByKind[(int)DamageKind.Slash], 3);
            Assert.Equal(snapshot.Total, snapshot.TotalByKind.Sum(), 3);
        }

        [Fact]
        public void OneEstimatedEventMarksTheWholeRow()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);
            recorder.Record(2d, Me, Hit(DamageKind.Slash, 30f), estimated: true);
            recorder.Record(2d, Sigrun, Hit(DamageKind.Slash, 30f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(3d, 30, Named);

            Assert.True(snapshot.Rows.Single(row => row.Id == Me).Estimated);
            Assert.False(snapshot.Rows.Single(row => row.Id == Sigrun).Estimated);
        }

        [Fact]
        public void AnEmptyWindowHasNoRowsAndNoAverage()
        {
            var recorder = new StatsRecorder(1800);

            WindowSnapshot snapshot = recorder.Snapshot(100d, 30, Named);

            Assert.Empty(snapshot.Rows);
            Assert.Equal(0f, snapshot.Total, 3);
            Assert.Equal(0f, snapshot.Average, 3);
            Assert.Equal(0, snapshot.Hits);
        }

        [Fact]
        public void TheLastEventTimeIsWhatTheWindowFadesOn()
        {
            var recorder = new StatsRecorder(1800);
            Assert.Equal(double.NegativeInfinity, recorder.LastEventTime);

            recorder.Record(12d, Me, Hit(DamageKind.Slash, 1f), estimated: false);

            Assert.Equal(12d, recorder.LastEventTime, 3);
        }

        [Fact]
        public void ClearingDropsEverything()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            recorder.Clear();

            Assert.Equal(0f, recorder.Snapshot(2d, 30, Named).Total, 3);
        }

        [Fact]
        public void AWindowLongerThanTheRingIsCappedAtTheRing()
        {
            var recorder = new StatsRecorder(60);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(30d, 1800, Named);

            Assert.Equal(30f, snapshot.Total, 3);
            Assert.Equal(60, snapshot.WindowSeconds);
        }

        [Fact]
        public void EventsFromTheFutureDoNotThrow()
        {
            var recorder = new StatsRecorder(60);
            recorder.Record(100d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            // The clock is Time.time; it never goes backwards, but a snapshot taken in the same
            // frame as the event must still see it.
            Assert.Equal(30f, recorder.Snapshot(100d, 30, Named).Total, 3);
        }

        [Fact]
        public void ANameThatChangesIsTakenFromTheDirectoryEveryTime()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            Assert.Equal("first", recorder.Snapshot(2d, 30, _ => "first").Rows[0].Name);
            Assert.Equal("second", recorder.Snapshot(2d, 30, _ => "second").Rows[0].Name);
        }

        [Fact]
        public void ABucketTheRingLappedWithoutWritingToItIsNotRead()
        {
            var recorder = new StatsRecorder(60);
            recorder.Record(0.5d, Me, Hit(DamageKind.Slash, 100f), estimated: false);

            // A full lap later the second-zero bucket still holds its numbers, but it is stamped
            // with a second that is no longer in any window: it must be skipped, not counted.
            WindowSnapshot snapshot = recorder.Snapshot(90d, 60, Named);

            Assert.Equal(0f, snapshot.Total, 3);
            Assert.Empty(snapshot.Rows);
        }

        [Fact]
        public void AWindowReachingBeforeTheStartOfTheClockIsFine()
        {
            var recorder = new StatsRecorder(60);
            recorder.Record(1d, Me, Hit(DamageKind.Slash, 30f), estimated: false);

            // Half a minute into a session a 30 s window starts at a negative second.
            WindowSnapshot snapshot = recorder.Snapshot(5d, 30, Named);

            Assert.Equal(30f, snapshot.Total, 3);
        }

        [Fact]
        public void AnEventWithNoDamageIsNotAHit()
        {
            var recorder = new StatsRecorder(1800);
            recorder.Record(1d, Me, new float[DamageKinds.Count], estimated: false);

            WindowSnapshot snapshot = recorder.Snapshot(2d, 30, Named);

            Assert.Equal(0, snapshot.Hits);
            Assert.Empty(snapshot.Rows);
        }
    }
}
