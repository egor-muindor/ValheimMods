using System;
using HarmonyLib;

namespace DeathTweaks.Patches
{
    /// <summary>
    /// Item keep/drop/destroy handling. See <see cref="DeathInventory"/> for the mechanics.
    /// <c>CreateTombStone</c> is also called by the <c>itemset</c> console command; only the
    /// call made from <c>Player.OnDeath</c> is handled, because the equip state of kept items
    /// is restored by the respawn that follows a death.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    internal static class Player_CreateTombStone_Patch
    {
        private static bool Prefix(Player __instance)
        {
            if (!Plugin.Enabled || !DeathContext.InOnDeath)
            {
                return true;
            }

            try
            {
                return DeathInventory.Prepare(__instance);
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Player_CreateTombStone_Patch), exception);
                TryRestoreHeld(__instance);
                return true;
            }
        }

        // A finalizer runs even when vanilla throws, so kept items can never stay detached.
        private static Exception? Finalizer(Player __instance, Exception? __exception)
        {
            TryRestoreHeld(__instance);
            return __exception;
        }

        private static void TryRestoreHeld(Player player)
        {
            try
            {
                DeathInventory.RestoreHeld(player);
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Player_CreateTombStone_Patch) + ".RestoreHeld", exception);
            }
        }
    }
}
