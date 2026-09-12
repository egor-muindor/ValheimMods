using System;
using HarmonyLib;
using UnityEngine;

namespace DeathTweaks.Patches
{
    /// <summary>
    /// Overrides the respawn point after death. Logins and intro skips go through vanilla:
    /// the override only applies when the game itself flags the respawn as following a death.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
    internal static class Game_FindSpawnPoint_Patch
    {
        private static Vector3? _lastRejectedTarget;

        private static bool Prefix(Game __instance, ref Vector3 point, ref bool usedLogoutPoint, float dt, ref bool __result)
        {
            if (!Plugin.Enabled || !__instance.m_respawnAfterDeath)
            {
                return true;
            }

            try
            {
                return !TryResolveSpawnPoint(__instance, dt, ref point, ref usedLogoutPoint, ref __result);
            }
            catch (Exception exception)
            {
                Plugin.PatchFailed(nameof(Game_FindSpawnPoint_Patch), exception);
                return true;
            }
        }

        /// <summary>
        /// Mirrors the vanilla method: called every fixed update until it reports the area as
        /// ready. Returns false to hand the decision back to vanilla.
        /// </summary>
        private static bool TryResolveSpawnPoint(Game game, float dt, ref Vector3 point, ref bool usedLogoutPoint, ref bool ready)
        {
            ModConfig settings = Plugin.Settings;
            Vector3 target;
            bool waitForLoad;

            if (settings.SpawnAtStart.Value)
            {
                if (!ZoneSystem.instance.GetLocationIcon(game.m_StartLocation, out Vector3 start))
                {
                    WarnOnce(Vector3.zero, $"Start location '{game.m_StartLocation}' not found, using vanilla respawn");
                    return false;
                }

                // Vanilla spawns at the start location as soon as the area is ready.
                target = start + Vector3.up * 2f;
                waitForLoad = false;
            }
            else if (settings.UseFixedSpawnCoordinates.Value)
            {
                // Vanilla gives custom spawn points a load window before checking readiness.
                target = settings.FixedSpawnCoordinates.Value;
                waitForLoad = true;
            }
            else
            {
                return false;
            }

            // Vanilla accumulates the wait at the top of FindSpawnPoint; do the same since it is skipped.
            game.m_respawnWait += dt;
            usedLogoutPoint = false;
            ZNet.instance.SetReferencePosition(target);
            ready = (!waitForLoad || game.m_respawnWait > game.m_respawnLoadDuration) && ZNetScene.instance.IsAreaReady(target);

            if (ready)
            {
                if (!ZoneSystem.instance.GetGroundHeight(target, out float ground))
                {
                    // No terrain here (outside the world, for example). Vanilla treats this as an
                    // invalid point; falling back avoids a respawn-death loop.
                    WarnOnce(target, $"No ground at spawn point {target}, using vanilla respawn");
                    ready = false;
                    return false;
                }

                // Fixed coordinates are typed by hand: never respawn below the terrain or under water.
                float floor = Mathf.Max(ground, ZoneSystem.instance.m_waterLevel);
                if (target.y < floor)
                {
                    target.y = floor + 0.25f;
                }

                Plugin.Debug($"Respawning at {target}");
            }

            point = target;
            return true;
        }

        private static void WarnOnce(Vector3 target, string message)
        {
            if (_lastRejectedTarget == target)
            {
                return;
            }

            _lastRejectedTarget = target;
            Plugin.Log.LogWarning(message);
        }
    }
}
