using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CombatStats.Patches
{
    /// <summary>
    /// A patch must never take the game down with it: the first failure of each patch is logged
    /// and the rest are swallowed, so a game update that changes something under us costs the
    /// meter its numbers and nothing else.
    ///
    /// The bodies are passed as arguments rather than captured, so the lambdas the callers write
    /// are cached by the compiler instead of allocating on every hit.
    /// </summary>
    internal static class PatchGuard
    {
        private static readonly HashSet<string> Reported = new HashSet<string>();

        public static void Run<T>(string patch, T argument, Action<T> body)
        {
            if (!Plugin.Enabled || Plugin.Headless)
            {
                return;
            }

            try
            {
                body(argument);
            }
            catch (Exception exception)
            {
                Report(patch, exception);
            }
        }

        public static void Run<T1, T2>(string patch, T1 first, T2 second, Action<T1, T2> body)
        {
            if (!Plugin.Enabled || Plugin.Headless)
            {
                return;
            }

            try
            {
                body(first, second);
            }
            catch (Exception exception)
            {
                Report(patch, exception);
            }
        }

        private static void Report(string patch, Exception exception)
        {
            if (Reported.Add(patch))
            {
                Plugin.Log.LogError($"{patch} failed; those numbers will be missing from the meter: {exception}");
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
                      && !__instance.InCutscene()
                      && !__instance.IsDebugFlying()
                      && !CinematicsManager.IsPlaying();
        }

        private static void Postfix(Character __instance, HitData hit, bool __state)
        {
            if (!__state)
            {
                return;
            }

            PatchGuard.Run(nameof(Character.ApplyDamage), __instance, hit,
                static (character, blow) => Plugin.Collector.OnApplied(character, blow));
        }
    }

    /// <summary>
    /// A blow arriving at the target's owner. Read only for who set the target alight: fire,
    /// poison and spirit are stripped out here and tick later without an attacker on them.
    ///
    /// The note is taken afterwards, and only when the game really did strip them out. Everything
    /// that makes the game discard the blow - a dodge, a corpse, PvP being off between two
    /// players - leaves those three where they were, and a discarded blow must not claim the burn
    /// a campfire gives the target later.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class Character_RPC_Damage_Patch
    {
        private static void Prefix(HitData hit, out bool __state)
        {
            __state = hit != null
                      && (hit.m_damage.m_fire > 0f || hit.m_damage.m_poison > 0f || hit.m_damage.m_spirit > 0f);
        }

        private static void Postfix(Character __instance, HitData hit, bool __state)
        {
            if (!__state)
            {
                return;
            }

            PatchGuard.Run(nameof(Character.RPC_Damage), __instance, hit,
                static (character, blow) => Plugin.Collector.OnBlow(character, blow));
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
            PatchGuard.Run(nameof(Character.Damage), __instance, hit,
                static (character, blow) => Plugin.Collector.OnSent(character, blow));
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
            PatchGuard.Run(nameof(Character.Heal), __instance, __state, static (character, before) =>
            {
                float healed = character.GetHealth() - before;
                if (healed > 0f)
                {
                    Plugin.Collector.OnHeal(character, healed);
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
            PatchGuard.Run(nameof(WearNTear.Damage), hit, static blow => Plugin.Collector.OnObject(blow));
        }
    }

    /// <summary>Crates, pots and the rest of the breakables.</summary>
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    internal static class Destructible_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(Destructible.Damage), hit, static blow => Plugin.Collector.OnObject(blow));
        }
    }

    /// <summary>Standing trees.</summary>
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
    internal static class TreeBase_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(TreeBase.Damage), hit, static blow => Plugin.Collector.OnObject(blow));
        }
    }

    /// <summary>Felled trunks.</summary>
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
    internal static class TreeLog_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(TreeLog.Damage), hit, static blow => Plugin.Collector.OnObject(blow));
        }
    }

    /// <summary>Ore veins and rock.</summary>
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
    internal static class MineRock_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(MineRock.Damage), hit, static blow => Plugin.Collector.OnObject(blow));
        }
    }

    /// <summary>The newer, chunked kind of ore vein.</summary>
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    internal static class MineRock5_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            PatchGuard.Run(nameof(MineRock5.Damage), hit, static blow => Plugin.Collector.OnObject(blow));
        }
    }
}
