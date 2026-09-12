using System;
using System.Globalization;
using System.Text;

namespace QuickTeleport.Teleport
{
    /// <summary>
    /// Timing and readiness decisions for one teleport. Pure: no Unity or game references.
    ///
    /// Vanilla <c>Player.UpdateTeleport</c> is driven by <c>m_teleportTimer</c>: above 2 s the
    /// player is moved to the target, above 8 s a portal teleport may finish, above 15 s the
    /// floor search gives up. The policy owns a real clock (<see cref="Elapsed"/>) and computes
    /// the value the vanilla timer should have so that vanilla does the right thing at the right
    /// moment. The player is never moved before the screen is black.
    /// </summary>
    public sealed class TeleportPolicy
    {
        /// <summary>Vanilla moves the player once <c>m_teleportTimer</c> passes this.</summary>
        public const float VanillaMoveDelay = 2f;

        /// <summary>Vanilla lets a portal (distant) teleport finish once <c>m_teleportTimer</c> passes this.</summary>
        public const float VanillaDistantDelay = 8f;

        /// <summary>Vanilla stops looking for a floor once <c>m_teleportTimer</c> passes this.</summary>
        public const float VanillaFloorTimeout = 15f;

        private const float Epsilon = 0.001f;

        private int? _stableCount;
        private float _stableSince;

        public TeleportPolicy(TeleportSettings settings, bool distant)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Distant = distant;
            Mode = settings.Mode;
            SpeedMultiplier = Math.Max(1f, settings.SpeedMultiplier);
            SettleTime = Math.Max(0f, settings.SettleTime);

            float fade = settings.FadeDuration;
            if (Mode == TeleportMode.Multiplier)
            {
                fade /= SpeedMultiplier;
            }
            FadeDuration = Math.Max(TeleportSettings.MinFadeDuration, fade);

            AreaCheck = !settings.WaitForAreaLoad ? AreaCheck.Skip
                : !settings.WaitForObjects ? AreaCheck.TerrainOnly
                : AreaCheck.Full;
        }

        /// <summary>True for portals, false for dungeon entrances.</summary>
        public bool Distant { get; }

        public TeleportMode Mode { get; }

        public float SpeedMultiplier { get; }

        /// <summary>Effective fade in seconds (already divided by the multiplier and clamped).</summary>
        public float FadeDuration { get; }

        public float SettleTime { get; }

        public AreaCheck AreaCheck { get; }

        /// <summary>The fixed wait vanilla applies to this kind of teleport.</summary>
        public float VanillaMinimum => Distant ? VanillaDistantDelay : VanillaMoveDelay;

        /// <summary>
        /// Latest moment the player is moved even if the HUD never reports a black screen (a mod
        /// that hides the loading screen): when vanilla, scaled by the multiplier, would move.
        /// </summary>
        public float MoveFallback => Mode == TeleportMode.Multiplier ? VanillaMoveDelay / SpeedMultiplier : VanillaMoveDelay;

        /// <summary>Real seconds since the teleport started.</summary>
        public float Elapsed { get; private set; }

        /// <summary>The value last written into the vanilla timer.</summary>
        public float VirtualTimer { get; private set; }

        /// <summary>True once the player may be moved: the fade time has passed and the HUD is black. Stays true.</summary>
        public bool ScreenBlack { get; private set; }

        public float? BlackAt { get; private set; }

        /// <summary>When the virtual timer first passed <see cref="VanillaMoveDelay"/>, so vanilla moved the player.</summary>
        public float? MovedAt { get; private set; }

        public float? AreaReadyAt { get; private set; }

        public int? ObjectCount { get; private set; }

        public float? SettledAt { get; private set; }

        public float? FinishedAt { get; private set; }

        public bool Finished => FinishedAt.HasValue;

        /// <summary>
        /// Advances the clock and returns the value the vanilla timer should have.
        /// <paramref name="hudBlack"/> is whether the loading screen is fully opaque right now;
        /// the fade runs on frame time while this clock runs on fixed time, so time alone could
        /// move the player one frame early.
        /// </summary>
        public float Advance(float dt, bool hudBlack = true)
        {
            Elapsed += Math.Max(0f, dt);

            if (!ScreenBlack)
            {
                ScreenBlack = Elapsed >= FadeDuration && (hudBlack || Elapsed >= MoveFallback);
                if (!ScreenBlack)
                {
                    VirtualTimer = 0f;
                    return 0f;
                }

                BlackAt = Elapsed;
            }

            float timer;
            if (Mode == TeleportMode.Multiplier)
            {
                timer = Elapsed * SpeedMultiplier;
            }
            else
            {
                // Past the 2 s and 8 s waits at once, but keep vanilla's floor retry window:
                // the "no floor, drop to terrain height" fallback still comes at 15 s of real time.
                timer = Math.Min(Elapsed + VanillaMinimum, VanillaFloorTimeout);
                if (Elapsed > VanillaFloorTimeout)
                {
                    timer = Elapsed;
                }
            }

            if (AreaCheck == AreaCheck.Skip)
            {
                // Nothing to wait for: let vanilla give up on the floor search at once.
                timer = Math.Max(timer, VanillaFloorTimeout + Epsilon);
            }

            if (timer > VanillaMoveDelay && !MovedAt.HasValue)
            {
                MovedAt = Elapsed;
            }

            VirtualTimer = timer;
            return timer;
        }

        /// <summary>
        /// Readiness gate. <paramref name="areaReady"/> is the vanilla answer (or the terrain-only
        /// answer); <paramref name="objectCount"/> the number of known objects in the destination
        /// zones. In Auto mode with the full check a portal teleport also waits until that count
        /// has been stable for <see cref="SettleTime"/>, capped at <see cref="VanillaMinimum"/>.
        /// Dungeon teleports never wait: the interior lies in the same zone as the entrance, so
        /// its objects are already loaded and nothing is left to arrive from the server.
        /// </summary>
        public bool ApplySettle(bool areaReady, int objectCount)
        {
            if (!areaReady)
            {
                _stableCount = null;
                return false;
            }

            if (!AreaReadyAt.HasValue)
            {
                AreaReadyAt = Elapsed;
            }
            ObjectCount = objectCount;

            bool settled;
            if (Mode != TeleportMode.Auto || AreaCheck != AreaCheck.Full || !Distant)
            {
                settled = true;
            }
            else
            {
                if (_stableCount != objectCount)
                {
                    _stableCount = objectCount;
                    _stableSince = Elapsed;
                }

                settled = SettleTime <= 0f
                    || Elapsed - _stableSince >= SettleTime
                    || Elapsed >= VanillaMinimum;
            }

            if (settled && !SettledAt.HasValue)
            {
                SettledAt = Elapsed;
            }
            return settled;
        }

        public void MarkFinished()
        {
            if (!FinishedAt.HasValue)
            {
                FinishedAt = Elapsed;
            }
        }

        /// <summary>One line with the timeline of this teleport, for the debug log.</summary>
        public string Describe()
        {
            var text = new StringBuilder();
            text.Append(Distant ? "Portal" : "Dungeon").Append(" teleport, ").Append(Mode);
            if (Mode == TeleportMode.Multiplier)
            {
                text.Append(" x").Append(SpeedMultiplier.ToString("0.##", CultureInfo.InvariantCulture));
            }

            text.Append(": screen black at ").Append(Seconds(BlackAt));
            text.Append(", moved at ").Append(Seconds(MovedAt));
            switch (AreaCheck)
            {
                case AreaCheck.Skip:
                    text.Append(", area load skipped");
                    break;
                case AreaCheck.TerrainOnly:
                    text.Append(", terrain loaded at ").Append(Seconds(AreaReadyAt));
                    break;
                default:
                    text.Append(", area loaded at ").Append(Seconds(AreaReadyAt));
                    if (ObjectCount.HasValue)
                    {
                        text.Append(" (").Append(ObjectCount.Value).Append(" objects)");
                    }
                    if (Mode == TeleportMode.Auto && Distant)
                    {
                        text.Append(", settled at ").Append(Seconds(SettledAt));
                    }
                    break;
            }

            text.Append(", finished at ").Append(Seconds(FinishedAt));
            text.Append("; vanilla waits at least ").Append(VanillaMinimum.ToString("0", CultureInfo.InvariantCulture)).Append(" s");
            return text.ToString();
        }

        private static string Seconds(float? time)
        {
            return time.HasValue ? time.Value.ToString("0.00", CultureInfo.InvariantCulture) + " s" : "never";
        }
    }
}
