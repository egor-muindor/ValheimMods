using System;
using HarmonyLib;

namespace CombatStats.Patches
{
    /// <summary>
    /// A patch must never take the game down with it: the first failure of each patch is logged
    /// and the rest are swallowed, so a game update that changes something under us costs the
    /// meter its numbers and nothing else.
    /// </summary>
    internal static class PatchGuard
    {
        private static readonly System.Collections.Generic.HashSet<string> Reported =
            new System.Collections.Generic.HashSet<string>();

        public static void Run(string patch, Action body)
        {
            if (!Plugin.Enabled)
            {
                return;
            }

            try
            {
                body();
            }
            catch (Exception exception)
            {
                if (Reported.Add(patch))
                {
                    Plugin.Log.LogError($"{patch} failed; those numbers will be missing from the meter: {exception}");
                }
            }
        }
    }

    /// <summary>
    /// The damage the target really took, after resistances, armour and the difficulty scaling.
    /// The game only runs this on the client that owns the target, so a hit is recorded once.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class Character_ApplyDamage_Patch
    {
        private static void Prefix(Character __instance, out bool __state)
        {
            // The game drops the whole call for a target in one of these states; so must we, or a
            // corpse would keep collecting damage.
            __state = __instance != null
                      && !__instance.IsDead()
                      && !__instance.IsTeleporting()
                      && !__instance.InCutscene();
        }

        private static void Postfix(Character __instance, HitData hit, bool __state)
        {
            if (!__state)
            {
                return;
            }

            PatchGuard.Run(nameof(Character.ApplyDamage), () => Plugin.Collector.OnApplied(__instance, hit));
        }
    }

    /// <summary>
    /// A blow arriving at the target's owner. Read only for who set the target alight: fire,
    /// poison and spirit are stripped out here and tick later without an attacker on them.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class Character_RPC_Damage_Patch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            PatchGuard.Run(nameof(Character.RPC_Damage), () => Plugin.Collector.OnBlow(__instance, hit));
        }
    }

    /// <summary>
    /// A blow leaving this client. Only used when the target belongs to a client without the mod,
    /// which is the one case where nobody will ever report what the target really took.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class Character_Damage_Patch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            PatchGuard.Run(nameof(Character.Damage), () => Plugin.Collector.OnSent(__instance, hit));
        }
    }

    /// <summary>Healing, measured as the health that was really regained.</summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Heal))]
    internal static class Character_Heal_Patch
    {
        private static void Prefix(Character __instance, out float __state)
        {
            __state = __instance != null ? __instance.GetHealth() : 0f;
        }

        private static void Postfix(Character __instance, float __state)
        {
            PatchGuard.Run(nameof(Character.Heal), () =>
            {
                float healed = __instance.GetHealth() - __state;
                if (healed > 0f)
                {
                    Plugin.Collector.OnHeal(__instance, healed);
                }
            });
        }
    }

    /// <summary>Buildings, ships and everything else held together by wear and tear.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
    internal static class WearNTear_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(WearNTear.Damage), () => Plugin.Collector.OnObject(hit));
        }
    }

    /// <summary>Crates, pots and the rest of the breakables.</summary>
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    internal static class Destructible_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(Destructible.Damage), () => Plugin.Collector.OnObject(hit));
        }
    }

    /// <summary>Standing trees.</summary>
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
    internal static class TreeBase_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(TreeBase.Damage), () => Plugin.Collector.OnObject(hit));
        }
    }

    /// <summary>Felled trunks.</summary>
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
    internal static class TreeLog_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(TreeLog.Damage), () => Plugin.Collector.OnObject(hit));
        }
    }

    /// <summary>Ore veins and rock.</summary>
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
    internal static class MineRock_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(MineRock.Damage), () => Plugin.Collector.OnObject(hit));
        }
    }

    /// <summary>The newer, chunked kind of ore vein.</summary>
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    internal static class MineRock5_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(MineRock5.Damage), () => Plugin.Collector.OnObject(hit));
        }
    }
}
