using System;
using HarmonyLib;
using QuickTeleport.Teleport;

namespace QuickTeleport.Patches
{
    /// <summary>
    /// Vanilla fades the loading screen at <c>dt / GetFadeDuration(player)</c>, 1 s for
    /// teleports. Returns the policy's fade while teleporting and while the screen is still
    /// fading back out afterwards. Death and sleep fades are untouched.
    /// </summary>
    [HarmonyPatch(typeof(Hud), nameof(Hud.GetFadeDuration))]
    internal static class Hud_GetFadeDuration_Patch
    {
        private static void Postfix(Hud __instance, Player player, ref float __result)
        {
            if (!Plugin.Enabled || player == null)
            {
                return;
            }

            try
            {
                if (player.IsDead() || player.IsSleeping())
                {
                    ActiveTeleport.FadingFromTeleport = false;
                    return;
                }

                if (player.IsTeleporting())
                {
                    TeleportPolicy? policy = ActiveTeleport.For(player);
                    if (policy != null)
                    {
                        __result = policy.FadeDuration;
                    }
                    return;
                }

                if (!ActiveTeleport.FadingFromTeleport)
                {
                    return;
                }

                if (ActiveTeleport.FadingPlayer != player)
                {
                    // A new player object (relog) has its own loading screen; that one is vanilla.
                    ActiveTeleport.FadingFromTeleport = false;
                    return;
                }

                if (__instance.m_loadingScreen.alpha > 0f)
                {
                    __result = ActiveTeleport.LastFade;
                }
                else
                {
                    ActiveTeleport.FadingFromTeleport = false;
                }
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Hud_GetFadeDuration_Patch), exception);
            }
        }
    }
}
