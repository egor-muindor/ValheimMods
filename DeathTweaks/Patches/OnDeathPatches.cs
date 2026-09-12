using System;
using System.Collections.Generic;
using HarmonyLib;

namespace DeathTweaks.Patches
{
    /// <summary>
    /// Wraps <c>Player.OnDeath</c> without replacing it: marks the death context for other
    /// patches and preserves active food when <c>KeepFoodLevels</c> is on.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    internal static class Player_OnDeath_Patch
    {
        private static void Prefix(Player __instance, ref List<Player.Food>? __state)
        {
            DeathContext.Enter();

            if (!Plugin.Enabled || !Plugin.Settings.KeepFoodLevels.Value || !__instance.IsOwner())
            {
                return;
            }

            // GetFoods() returns the live list that vanilla clears; copy it first.
            __state = new List<Player.Food>(__instance.GetFoods());
        }

        private static void Postfix(Player __instance, List<Player.Food>? __state)
        {
            if (__state == null || __state.Count == 0)
            {
                return;
            }

            List<Player.Food> foods = __instance.GetFoods();
            foreach (Player.Food food in __state)
            {
                if (!foods.Contains(food))
                {
                    foods.Add(food);
                }
            }

            Plugin.Debug($"Kept {__state.Count} food items through death");
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            DeathContext.Exit();
            return __exception;
        }
    }
}
