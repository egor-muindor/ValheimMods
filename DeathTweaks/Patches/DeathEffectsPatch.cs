using HarmonyLib;

namespace DeathTweaks.Patches
{
    /// <summary>Skips the ragdoll and particle effects when <c>CreateDeathEffects</c> is off.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CreateDeathEffects))]
    internal static class Player_CreateDeathEffects_Patch
    {
        private static bool Prefix()
        {
            return !Plugin.Enabled || Plugin.Settings.CreateDeathEffects.Value;
        }
    }
}
