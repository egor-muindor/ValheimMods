using CombatStats.Ui;
using HarmonyLib;
using Muindor.Windows;
using UnityEngine;

namespace CombatStats.Patches
{
    /// <summary>
    /// The detail window and its settings are windows of their own, so while one is open the
    /// player must not walk, swing or look around, and the cursor must stay free. Vanilla decides
    /// both from a fixed list of open screens, which a plugin cannot join; these patches append to
    /// the answer instead of changing the list.
    /// </summary>
    internal static class Windows
    {
        /// <summary>True while anything this mod owns has the screen.</summary>
        public static bool AnyOpen => DetailWindow.IsOpen || SettingsPanel.IsOpen;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
    internal static class Player_TakeInput_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (Windows.AnyOpen)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Walking, the mouse look and attacking are gated by a second, separate check in
    /// <see cref="PlayerController"/>; <c>Player.TakeInput</c> only covers interacting, the
    /// hotbar and the build menu. Both have to say no.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    internal static class PlayerController_TakeInput_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (Windows.AnyOpen)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// While the detail window is open the mouse wheel belongs to it, not to the camera zoom or
    /// the hotbar. Only its sign is used: the raw value's scale is not something a plugin can
    /// rely on.
    /// </summary>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    internal static class ZInput_GetMouseScrollWheel_Patch
    {
        private static void Postfix(ref float __result)
        {
            if (__result == 0f || !DetailWindow.IsOpen)
            {
                return;
            }

            DetailWindow.FeedScrollWheel(__result);
            __result = 0f;
        }
    }

    /// <summary>
    /// Keeps the mouse pointer free while a window of the mod is open. A postfix cannot do this:
    /// vanilla's capture branch runs whenever no vanilla screen is open and locks the cursor,
    /// which warps it to the centre; on Linux the warp reaches the hardware pointer at once, so
    /// undoing it after the fact pins the cursor to the centre of the screen. The rule lives in
    /// <see cref="CursorRelease"/>: vanilla is skipped while a window is open, and the lock state
    /// is written only when it differs, so the release happens once and not every frame.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class GameCamera_UpdateMouseCapture_Patch
    {
        private static bool Prefix()
        {
            switch (CursorRelease.Decide(Windows.AnyOpen, Cursor.lockState == CursorLockMode.None))
            {
                case CursorRelease.Step.RunVanilla:
                    return true;
                case CursorRelease.Step.Release:
                    ZCursor.LockState = CursorLockMode.None;
                    break;
            }

            ZCursor.Show();
            return false;
        }
    }

    /// <summary>
    /// Escape closes the settings, then the detail window, instead of opening the game menu
    /// behind them. Handled here rather than in the windows' own Update because the order of the
    /// two Update calls is not fixed: whichever runs first, the menu only ever sees the key once
    /// both windows are closed.
    /// </summary>
    [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
    internal static class Menu_Update_Patch
    {
        private static bool Prefix()
        {
            if (!Windows.AnyOpen || Menu.IsVisible())
            {
                return true;
            }

            if (!ZInput.GetKeyDown(KeyCode.Escape, logWarning: false))
            {
                return true;
            }

            if (SettingsPanel.IsOpen)
            {
                SettingsPanel.Instance?.Close();
                return false;
            }

            DetailWindow.Instance?.Close();
            return false;
        }
    }
}
