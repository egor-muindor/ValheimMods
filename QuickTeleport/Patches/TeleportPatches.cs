using System;
using HarmonyLib;
using QuickTeleport.Teleport;
using UnityEngine;

namespace QuickTeleport.Patches
{
    /// <summary>The teleport in progress for the local player, shared by the patches.</summary>
    internal static class ActiveTeleport
    {
        public static Player? Player { get; private set; }

        public static TeleportPolicy? Policy { get; private set; }

        /// <summary>
        /// Set while <c>Player.UpdateTeleport</c> runs for a tracked teleport. The area gate only
        /// acts while this is set, so other callers of <c>ZNetScene.IsAreaReady</c> (respawn)
        /// keep vanilla behaviour.
        /// </summary>
        public static TeleportPolicy? Updating { get; set; }

        /// <summary>Effective fade of the last teleport, kept until the loading screen has faded out.</summary>
        public static float LastFade { get; private set; } = 1f;

        /// <summary>True from the start of a teleport until the loading screen has faded back out.</summary>
        public static bool FadingFromTeleport { get; set; }

        /// <summary>The player whose loading screen <see cref="FadingFromTeleport"/> refers to.</summary>
        public static Player? FadingPlayer { get; private set; }

        /// <summary>True once the server has been told about the new position of this teleport.</summary>
        public static bool PositionAnnounced { get; set; }

        public static TeleportPolicy Start(Player player, bool distant, float elapsed = 0f)
        {
            var policy = new TeleportPolicy(Plugin.Settings.ToSettings(), distant);
            if (elapsed > 0f)
            {
                policy.Advance(elapsed);
            }

            Player = player;
            Policy = policy;
            LastFade = policy.FadeDuration;
            FadingFromTeleport = true;
            FadingPlayer = player;
            PositionAnnounced = false;
            return policy;
        }

        /// <summary>Whether the loading screen is fully opaque. Without a HUD there is nothing to hide.</summary>
        public static bool HudBlack()
        {
            Hud hud = Hud.instance;
            return hud == null || hud.m_loadingScreen == null || hud.m_loadingScreen.alpha >= 1f;
        }

        public static TeleportPolicy? For(Player player)
        {
            return Player == player ? Policy : null;
        }

        public static void Clear()
        {
            Player = null;
            Policy = null;
            Updating = null;
        }
    }

    /// <summary>Starts tracking a teleport the moment vanilla accepts it.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    internal static class Player_TeleportTo_Patch
    {
        private static void Postfix(Player __instance, Vector3 pos, bool distantTeleport, bool __result)
        {
            if (!__result)
            {
                return;
            }

            // A policy left over from an earlier teleport must never be applied to this one.
            ActiveTeleport.Clear();
            if (!Plugin.Enabled)
            {
                return;
            }

            try
            {
                TeleportPolicy policy = ActiveTeleport.Start(__instance, distantTeleport);
                Plugin.Debug($"{(distantTeleport ? "Portal" : "Dungeon")} teleport to {pos} started: {policy.Mode}, fade {policy.FadeDuration:0.00} s, area check {policy.AreaCheck}");
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Player_TeleportTo_Patch), exception);
            }
        }
    }

    /// <summary>
    /// Drives the vanilla teleport clock. Vanilla adds <c>dt</c> to <c>m_teleportTimer</c> and
    /// compares it with 2 s, 8 s and 15 s; the prefix writes the policy's virtual timer into the
    /// field and zeroes <c>dt</c>, so vanilla runs its own logic at the moments the policy picks.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
    internal static class Player_UpdateTeleport_Patch
    {
        private static void Prefix(Player __instance, ref float dt)
        {
            if (!Plugin.Enabled || !__instance.m_teleporting)
            {
                return;
            }

            try
            {
                // A teleport that started while the mod was disabled has no policy yet.
                TeleportPolicy policy = ActiveTeleport.For(__instance)
                    ?? ActiveTeleport.Start(__instance, __instance.m_distantTeleport, __instance.m_teleportTimer);

                __instance.m_teleportTimer = policy.Advance(dt, ActiveTeleport.HudBlack());
                dt = 0f;
                ActiveTeleport.Updating = policy;
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Player_UpdateTeleport_Patch), exception);
            }
        }

        private static void Postfix(Player __instance)
        {
            TeleportPolicy? policy = ActiveTeleport.Updating;
            ActiveTeleport.Updating = null;
            if (policy == null)
            {
                return;
            }

            if (policy.MovedAt.HasValue && !ActiveTeleport.PositionAnnounced)
            {
                AnnouncePosition(__instance);
            }

            if (__instance.m_teleporting)
            {
                return;
            }

            policy.MarkFinished();
            Plugin.Debug($"Teleport finished: {policy.Describe()}");
            ActiveTeleport.Clear();
        }

        /// <summary>
        /// The server streams objects around the position the client reports, and vanilla only
        /// reports it every 2 s (<c>ZNet.SendPeriodicData</c>). Vanilla's fixed 8 s hide that
        /// delay; this mod does not, so report the new position right after the move. The
        /// reference position is set here because vanilla updates it in <c>Player.LateUpdate</c>,
        /// after <c>ZNet.Update</c> would already have sent the old one.
        ///
        /// The player's ZDO is moved to the target first. <c>ZNetScene.RemoveObjects</c> destroys
        /// every instance whose ZDO lies outside the area around the reference position, and
        /// vanilla only moves the player's ZDO in <c>LateUpdate</c> (<c>ZSyncTransform.OwnerSync</c>).
        /// This runs in <c>FixedUpdate</c>: with the reference position already at the target and
        /// the ZDO still at the origin, the next <c>ZNetScene.Update</c> destroyed the local player
        /// ("Local player destroyed" in the log) and the screen stayed black for good.
        /// </summary>
        private static void AnnouncePosition(Player player)
        {
            ActiveTeleport.PositionAnnounced = true;
            try
            {
                ZNet net = ZNet.instance;
                ZDO? zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
                if (net == null || zdo == null)
                {
                    return;
                }

                Vector3 position = player.transform.position;
                zdo.SetPosition(position);
                net.SetReferencePosition(position);
                net.m_periodicSendTimer = 2f;
                Plugin.Debug($"Reported the new position {position} to the server");
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Player_UpdateTeleport_Patch), exception);
            }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            ActiveTeleport.Updating = null;
            return __exception;
        }
    }
}
