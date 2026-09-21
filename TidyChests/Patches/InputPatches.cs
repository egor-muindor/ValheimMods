using HarmonyLib;
using Muindor.Windows;
using TidyChests.Ui;
using UnityEngine;

namespace TidyChests.Patches
{
    /// <summary>
    /// The chest list is a window of its own, so while it is open the player must not walk,
    /// swing or look around, and the cursor must stay free. Vanilla decides both from a fixed
    /// list of open screens, which a plugin cannot join; these patches append to the answer
    /// instead of changing the list.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
    internal static class Player_TakeInput_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (ChestBrowser.IsPanelOpen)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Walking, the mouse look and attacking are gated by a second, separate check in
    /// <see cref="PlayerController"/>; <c>Player.TakeInput</c> only covers interacting, the
    /// hotbar and the build menu. Both have to say no, or typing a name into the search box
    /// walks the character around.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
    internal static class PlayerController_TakeInput_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (ChestBrowser.IsPanelOpen)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// While the chest list is open the mouse wheel belongs to it, not to the camera zoom or
    /// the hotbar. The value is handed to the panel and then hidden from everything else; the
    /// panel only reads its sign, because the raw value's scale is not something a plugin can
    /// rely on (the camera clamps it to 0.05, the placement ghost accumulates it).
    /// </summary>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    internal static class ZInput_GetMouseScrollWheel_Patch
    {
        private static void Postfix(ref float __result)
        {
            if (__result == 0f || !ChestBrowser.IsPanelOpen)
            {
                return;
            }

            ChestBrowser.FeedScrollWheel(__result);
            __result = 0f;
        }
    }

    /// <summary>
    /// Keeps the mouse pointer free while the chest list is open. A postfix cannot do this:
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
            bool cursorFree = Cursor.lockState == CursorLockMode.None;
            switch (CursorRelease.Decide(ChestBrowser.IsPanelOpen, cursorFree))
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
    /// Escape closes the chest list instead of opening the game menu behind it. Handled here
    /// rather than in the panel's own Update because the order of the two Update calls is not
    /// fixed: whichever runs first, the menu only ever sees the key when the list is closed.
    /// </summary>
    [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
    internal static class Menu_Update_Patch
    {
        private static bool Prefix()
        {
            ChestBrowser? browser = Plugin.Browser;
            if (browser == null || !browser.IsOpen || Menu.IsVisible())
            {
                return true;
            }

            if (!ZInput.GetKeyDown(KeyCode.Escape, logWarning: false))
            {
                return true;
            }

            browser.Close();
            return false;
        }
    }
}
