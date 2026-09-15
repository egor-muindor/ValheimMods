using HarmonyLib;
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

    /// <summary>Keeps the mouse pointer on screen while the chest list is open.</summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class GameCamera_UpdateMouseCapture_Patch
    {
        private static void Postfix()
        {
            if (!ChestBrowser.IsPanelOpen)
            {
                return;
            }

            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
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
