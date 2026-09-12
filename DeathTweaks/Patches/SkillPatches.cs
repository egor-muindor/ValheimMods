using System;
using HarmonyLib;

namespace DeathTweaks.Patches
{
    /// <summary>
    /// Skill loss on death. Vanilla computes <c>m_DeathLowerFactor * Game.m_skillReductionRate</c>
    /// inside <c>Skills.OnDeath</c>; the factor is swapped for the configured value around that
    /// call so the world modifier keeps applying.
    /// </summary>
    [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
    internal static class Skills_OnDeath_Patch
    {
        private static bool Prefix(Skills __instance, ref float __state)
        {
            if (!Plugin.Enabled)
            {
                return true;
            }

            if (!Plugin.Settings.ReduceSkills.Value)
            {
                Plugin.Debug("Skill loss skipped (ReduceSkills is off)");
                return false;
            }

            __state = __instance.m_DeathLowerFactor;
            __instance.m_DeathLowerFactor = Plugin.Settings.SkillReduceFactor.Value;
            Plugin.Debug($"Lowering skills by {__instance.m_DeathLowerFactor} x {Game.m_skillReductionRate} (world modifier)");
            return true;
        }

        // A finalizer restores the factor even when the original throws (a postfix would not run).
        private static Exception? Finalizer(Skills __instance, float __state, Exception? __exception)
        {
            if (Plugin.Enabled && Plugin.Settings.ReduceSkills.Value)
            {
                __instance.m_DeathLowerFactor = __state;
            }

            return __exception;
        }
    }

    /// <summary>
    /// The <c>DeathSkillsReset</c> world modifier wipes all skills through <c>Skills.Clear</c>.
    /// <c>ReduceSkills=false</c> means no skill loss at all, so the reset is skipped too.
    /// <c>Skills.Clear</c> is also used when loading a profile, hence the death-context check.
    /// </summary>
    [HarmonyPatch(typeof(Skills), nameof(Skills.Clear))]
    internal static class Skills_Clear_Patch
    {
        private static bool Prefix()
        {
            if (Plugin.Enabled && DeathContext.InOnDeath && !Plugin.Settings.ReduceSkills.Value)
            {
                Plugin.Debug("Skill reset skipped (ReduceSkills is off)");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// <c>NoSkillProtection</c>: every death counts as a hard death. This also stops the
    /// soft-death status effect from being applied after a recent death.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.HardDeath))]
    internal static class Player_HardDeath_Patch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!Plugin.Enabled || !Plugin.Settings.NoSkillProtection.Value)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }
}
