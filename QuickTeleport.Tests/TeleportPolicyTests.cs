using System;
using QuickTeleport.Teleport;
using Xunit;

namespace QuickTeleport.Tests
{
    public class TeleportPolicyTests
    {
        private static TeleportPolicy Policy(bool distant = true, Action<TeleportSettings>? configure = null)
        {
            var settings = new TeleportSettings();
            configure?.Invoke(settings);
            return new TeleportPolicy(settings, distant);
        }

        /// <summary>
        /// Advances the clock in fixed steps up to <paramref name="seconds"/> and returns the
        /// last virtual timer. The step is an exact binary fraction so that sums stay exact and
        /// boundary checks are not subject to float drift; targets must be multiples of it.
        /// </summary>
        private static float AdvanceTo(TeleportPolicy policy, float seconds, float step = 0.125f, bool hudBlack = true)
        {
            float timer = policy.VirtualTimer;
            while (policy.Elapsed + step <= seconds)
            {
                timer = policy.Advance(step, hudBlack);
            }
            Assert.Equal(seconds, policy.Elapsed);
            return timer;
        }

        [Fact]
        public void Auto_TimerIsZeroUntilTheScreenIsBlack()
        {
            var policy = Policy();

            Assert.Equal(0f, AdvanceTo(policy, 0.875f));
            Assert.False(policy.ScreenBlack);
            Assert.Null(policy.BlackAt);
        }

        [Fact]
        public void Auto_Portal_TimerPassesTheDistantMinimumRightAfterTheFade()
        {
            var policy = Policy(distant: true);

            float timer = AdvanceTo(policy, 1.0f);

            Assert.True(policy.ScreenBlack);
            Assert.True(timer > TeleportPolicy.VanillaDistantDelay, $"timer {timer}");
            Assert.True(timer < TeleportPolicy.VanillaFloorTimeout, $"timer {timer}");
            Assert.Equal(1.0f, policy.BlackAt);
        }

        [Fact]
        public void Auto_Dungeon_TimerPassesTheMoveDelayRightAfterTheFade()
        {
            var policy = Policy(distant: false);

            float timer = AdvanceTo(policy, 1.0f);

            Assert.True(timer > TeleportPolicy.VanillaMoveDelay, $"timer {timer}");
            Assert.True(timer < TeleportPolicy.VanillaDistantDelay, $"timer {timer}");
        }

        [Fact]
        public void Auto_Portal_FloorTimeoutKeepsTheVanillaFifteenSeconds()
        {
            var policy = Policy(distant: true);

            // The settle cap can hold a portal teleport until 8 s; vanilla's floor retry window (8 s to 15 s) must survive.
            Assert.False(AdvanceTo(policy, 8.0f) > TeleportPolicy.VanillaFloorTimeout);
            Assert.False(AdvanceTo(policy, 14.875f) > TeleportPolicy.VanillaFloorTimeout);
            Assert.True(AdvanceTo(policy, 15.125f) > TeleportPolicy.VanillaFloorTimeout);
        }

        [Fact]
        public void Auto_WaitsForTheHudToBeBlack_ButNotLongerThanVanillaWouldMove()
        {
            var policy = Policy();

            AdvanceTo(policy, 1.0f, hudBlack: false);
            Assert.False(policy.ScreenBlack);
            Assert.Equal(0f, policy.VirtualTimer);
            AdvanceTo(policy, 1.875f, hudBlack: false);
            Assert.False(policy.ScreenBlack);
            AdvanceTo(policy, 2.0f, hudBlack: false);
            Assert.True(policy.ScreenBlack);
            Assert.Equal(2.0f, policy.BlackAt);
        }

        [Fact]
        public void Auto_HudBlackBeforeTheFadeTimeDoesNotMoveEarly()
        {
            var policy = Policy();

            AdvanceTo(policy, 0.875f, hudBlack: true);
            Assert.False(policy.ScreenBlack);
        }

        [Fact]
        public void Multiplier_WaitsForTheHudToBeBlack_ButNotLongerThanScaledVanillaWouldMove()
        {
            var policy = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 4f;
            });

            Assert.Equal(0.5f, policy.MoveFallback);
            AdvanceTo(policy, 0.375f, hudBlack: false);
            Assert.False(policy.ScreenBlack);
            AdvanceTo(policy, 0.5f, hudBlack: false);
            Assert.True(policy.ScreenBlack);
        }

        [Fact]
        public void ScreenBlack_StaysBlackOnceReached()
        {
            var policy = Policy();

            AdvanceTo(policy, 1.0f);
            AdvanceTo(policy, 1.125f, hudBlack: false);

            Assert.True(policy.ScreenBlack);
            Assert.True(policy.VirtualTimer > TeleportPolicy.VanillaDistantDelay);
        }

        [Fact]
        public void Auto_FadeDurationIsTheConfiguredValue()
        {
            var policy = Policy(configure: s => s.FadeDuration = 0.25f);

            Assert.Equal(0.25f, policy.FadeDuration);
            Assert.Equal(0f, AdvanceTo(policy, 0.125f));
            Assert.True(AdvanceTo(policy, 0.25f) > TeleportPolicy.VanillaDistantDelay);
        }

        [Fact]
        public void Multiplier_TimerRunsNTimesFasterAfterTheFade()
        {
            var policy = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 4f;
            });

            Assert.Equal(0.25f, policy.FadeDuration);
            Assert.Equal(0f, AdvanceTo(policy, 0.125f));
            Assert.Equal(2.0f, AdvanceTo(policy, 0.5f));
            Assert.Equal(8.0f, AdvanceTo(policy, 2.0f));
        }

        [Fact]
        public void Multiplier_One_ReproducesTheVanillaTimeline()
        {
            var policy = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 1f;
            });

            Assert.Equal(1f, policy.FadeDuration);
            Assert.Equal(0f, AdvanceTo(policy, 0.875f));
            Assert.Equal(2.0f, AdvanceTo(policy, 2.0f));
            Assert.Equal(8.0f, AdvanceTo(policy, 8.0f));
        }

        [Fact]
        public void FadeIsNeverShorterThanTheMinimum()
        {
            var auto = Policy(configure: s => s.FadeDuration = 0f);
            var multiplied = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 100f;
                s.FadeDuration = 1f;
            });

            Assert.Equal(TeleportSettings.MinFadeDuration, auto.FadeDuration);
            Assert.Equal(TeleportSettings.MinFadeDuration, multiplied.FadeDuration);
        }

        [Fact]
        public void MultiplierBelowOneIsClampedToOne()
        {
            var policy = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 0.25f;
            });

            Assert.Equal(1f, policy.SpeedMultiplier);
            Assert.Equal(2.0f, AdvanceTo(policy, 2.0f));
        }

        [Fact]
        public void Advance_IgnoresNegativeSteps()
        {
            var policy = Policy();

            policy.Advance(-1f);

            Assert.Equal(0f, policy.Elapsed);
        }

        [Fact]
        public void VanillaMinimum_DependsOnTheTeleportKind()
        {
            Assert.Equal(TeleportPolicy.VanillaDistantDelay, Policy(distant: true).VanillaMinimum);
            Assert.Equal(TeleportPolicy.VanillaMoveDelay, Policy(distant: false).VanillaMinimum);
        }

        [Fact]
        public void AreaCheck_FollowsTheLoadingOptions()
        {
            Assert.Equal(AreaCheck.Full, Policy().AreaCheck);
            Assert.Equal(AreaCheck.TerrainOnly, Policy(configure: s => s.WaitForObjects = false).AreaCheck);
            Assert.Equal(AreaCheck.Skip, Policy(configure: s => s.WaitForAreaLoad = false).AreaCheck);
            Assert.Equal(AreaCheck.Skip, Policy(configure: s =>
            {
                s.WaitForAreaLoad = false;
                s.WaitForObjects = false;
            }).AreaCheck);
        }

        [Fact]
        public void SkipAreaLoad_TimerPassesTheFloorTimeoutRightAfterTheFade()
        {
            var auto = Policy(configure: s => s.WaitForAreaLoad = false);
            var multiplied = Policy(configure: s =>
            {
                s.WaitForAreaLoad = false;
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 2f;
            });

            Assert.Equal(0f, AdvanceTo(auto, 0.875f));
            Assert.True(AdvanceTo(auto, 1.0f) > TeleportPolicy.VanillaFloorTimeout);
            Assert.True(AdvanceTo(multiplied, 0.5f) > TeleportPolicy.VanillaFloorTimeout);
        }

        [Fact]
        public void Settle_NotReadyWhileTheAreaIsNotReady()
        {
            var policy = Policy();
            AdvanceTo(policy, 1.0f);

            Assert.False(policy.ApplySettle(false, 0));
            Assert.Null(policy.AreaReadyAt);
        }

        [Fact]
        public void Settle_ReadyAfterTheObjectCountStaysStableForSettleTime()
        {
            var policy = Policy(configure: s => s.SettleTime = 0.5f);
            AdvanceTo(policy, 1.0f);

            Assert.False(policy.ApplySettle(true, 100));
            Assert.Equal(1.0f, policy.AreaReadyAt);
            AdvanceTo(policy, 1.375f);
            Assert.False(policy.ApplySettle(true, 100));
            AdvanceTo(policy, 1.5f);
            Assert.True(policy.ApplySettle(true, 100));
            Assert.Equal(1.5f, policy.SettledAt);
            Assert.Equal(100, policy.ObjectCount);
        }

        [Fact]
        public void Settle_ObjectCountChangeRestartsTheWait()
        {
            var policy = Policy(configure: s => s.SettleTime = 0.5f);
            AdvanceTo(policy, 1.0f);

            policy.ApplySettle(true, 100);
            AdvanceTo(policy, 1.375f);
            Assert.False(policy.ApplySettle(true, 120));
            AdvanceTo(policy, 1.75f);
            Assert.False(policy.ApplySettle(true, 120));
            AdvanceTo(policy, 1.875f);
            Assert.True(policy.ApplySettle(true, 120));
        }

        [Fact]
        public void Settle_AreaBecomingNotReadyRestartsTheWait()
        {
            var policy = Policy(configure: s => s.SettleTime = 0.5f);
            AdvanceTo(policy, 1.0f);

            policy.ApplySettle(true, 100);
            AdvanceTo(policy, 1.375f);
            Assert.False(policy.ApplySettle(false, 0));
            AdvanceTo(policy, 1.5f);
            Assert.False(policy.ApplySettle(true, 100));
            AdvanceTo(policy, 2.0f);
            Assert.True(policy.ApplySettle(true, 100));
        }

        [Fact]
        public void Settle_NeverWaitsLongerThanTheVanillaMinimum()
        {
            var portal = Policy(distant: true, configure: s => s.SettleTime = 5f);
            AdvanceTo(portal, 1.0f);
            portal.ApplySettle(true, 100);
            AdvanceTo(portal, 7.875f);
            Assert.False(portal.ApplySettle(true, 101));
            AdvanceTo(portal, 8.0f);
            Assert.True(portal.ApplySettle(true, 102));

            var dungeon = Policy(distant: false, configure: s => s.SettleTime = 5f);
            AdvanceTo(dungeon, 1.0f);
            dungeon.ApplySettle(true, 100);
            AdvanceTo(dungeon, 1.875f);
            Assert.False(dungeon.ApplySettle(true, 101));
            AdvanceTo(dungeon, 2.0f);
            Assert.True(dungeon.ApplySettle(true, 102));
        }

        [Fact]
        public void Settle_ZeroSettleTimeIsImmediate()
        {
            var policy = Policy(configure: s => s.SettleTime = 0f);
            AdvanceTo(policy, 1.0f);

            Assert.True(policy.ApplySettle(true, 100));
        }

        [Fact]
        public void Settle_MultiplierModePassesReadinessThrough()
        {
            var policy = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SettleTime = 5f;
            });
            AdvanceTo(policy, 1.0f);

            Assert.False(policy.ApplySettle(false, 0));
            Assert.True(policy.ApplySettle(true, 100));
            Assert.Equal(1.0f, policy.SettledAt);
        }

        [Fact]
        public void Settle_TerrainOnlyPassesReadinessThrough()
        {
            var policy = Policy(configure: s => s.WaitForObjects = false);
            AdvanceTo(policy, 1.0f);

            Assert.True(policy.ApplySettle(true, 0));
        }

        [Fact]
        public void MarkFinished_RecordsTheTimeOnce()
        {
            var policy = Policy();
            AdvanceTo(policy, 1.5f);

            policy.MarkFinished();
            AdvanceTo(policy, 2.0f);
            policy.MarkFinished();

            Assert.True(policy.Finished);
            Assert.Equal(1.5f, policy.FinishedAt);
        }

        [Fact]
        public void Describe_ListsTheTimeline()
        {
            var policy = Policy(distant: true);
            AdvanceTo(policy, 1.0f);
            policy.ApplySettle(true, 312);
            AdvanceTo(policy, 1.5f);
            policy.ApplySettle(true, 312);
            AdvanceTo(policy, 1.75f);
            policy.MarkFinished();

            Assert.Equal(
                "Portal teleport, Auto: screen black at 1.00 s, moved at 1.00 s, area loaded at 1.00 s (312 objects), settled at 1.50 s, finished at 1.75 s; vanilla waits at least 8 s",
                policy.Describe());
        }

        [Fact]
        public void Describe_MentionsSkippedStagesAndTheMultiplier()
        {
            var skipped = Policy(distant: false, configure: s => s.WaitForAreaLoad = false);
            Assert.Equal(
                "Dungeon teleport, Auto: screen black at never, moved at never, area load skipped, finished at never; vanilla waits at least 2 s",
                skipped.Describe());

            var terrain = Policy(distant: true, configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 2f;
                s.WaitForObjects = false;
            });
            AdvanceTo(terrain, 1.25f, step: 0.25f);
            terrain.ApplySettle(true, 0);
            terrain.MarkFinished();
            Assert.Equal(
                "Portal teleport, Multiplier x2: screen black at 0.50 s, moved at 1.25 s, terrain loaded at 1.25 s, finished at 1.25 s; vanilla waits at least 8 s",
                terrain.Describe());
        }

        [Fact]
        public void Auto_MovedAtIsTheMomentTheScreenIsBlack()
        {
            var policy = Policy();

            AdvanceTo(policy, 0.875f);
            Assert.Null(policy.MovedAt);
            AdvanceTo(policy, 1.0f);
            Assert.Equal(1.0f, policy.MovedAt);
        }

        [Fact]
        public void Multiplier_MovedAtIsWhenTheTimerPassesTheMoveDelay()
        {
            var policy = Policy(configure: s =>
            {
                s.Mode = TeleportMode.Multiplier;
                s.SpeedMultiplier = 4f;
            });

            AdvanceTo(policy, 0.5f);
            Assert.Null(policy.MovedAt);
            AdvanceTo(policy, 0.625f);
            Assert.Equal(0.625f, policy.MovedAt);
        }
    }
}
