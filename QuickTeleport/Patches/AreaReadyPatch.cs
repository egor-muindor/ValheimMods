using System;
using HarmonyLib;
using QuickTeleport.Teleport;
using UnityEngine;

namespace QuickTeleport.Patches
{
    /// <summary>
    /// Decides when the destination counts as loaded, only for the call made from
    /// <c>Player.UpdateTeleport</c> of a tracked teleport. Skip: always ready. TerrainOnly: the
    /// zone terrain is loaded. Full: vanilla's answer, then the policy's settle wait.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.IsAreaReady))]
    internal static class ZNetScene_IsAreaReady_Patch
    {
        private static bool Prefix(Vector3 point, ref bool __result)
        {
            TeleportPolicy? policy = ActiveTeleport.Updating;
            if (policy == null)
            {
                return true;
            }

            try
            {
                switch (policy.AreaCheck)
                {
                    case AreaCheck.Skip:
                        __result = true;
                        return false;
                    case AreaCheck.TerrainOnly:
                        __result = ZoneSystem.instance.IsZoneLoaded(point);
                        return false;
                    default:
                        return true;
                }
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(ZNetScene_IsAreaReady_Patch), exception);
                return true;
            }
        }

        private static void Postfix(ZNetScene __instance, ref bool __result)
        {
            TeleportPolicy? policy = ActiveTeleport.Updating;
            if (policy == null)
            {
                return;
            }

            try
            {
                // m_tempCurrentObjects holds the objects vanilla just checked (Full only).
                int objectCount = policy.AreaCheck == AreaCheck.Full ? __instance.m_tempCurrentObjects.Count : 0;
                __result = policy.ApplySettle(__result, objectCount);
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(ZNetScene_IsAreaReady_Patch), exception);
            }
        }
    }
}
