using System;
using TidyChests.Restock;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// The Restock button in the player's inventory panel: another copy of the vanilla "take all"
    /// button, the size of the Stash button and placed under it. Created the first time the
    /// inventory opens and again after a world change, like the Stash button.
    /// </summary>
    internal static class RestockButton
    {
        private const string ButtonName = "TidyChests Restock";

        private static Button? _button;

        private static TMP_Text? _label;

        private static bool _warned;

        /// <summary>Creates or shows the button on <paramref name="gui"/>, honouring the <c>ShowRestockButton</c> setting.</summary>
        public static void Ensure(InventoryGui gui)
        {
            if (!Plugin.Enabled || !Plugin.Settings.ShowRestockButton.Value)
            {
                if (_button != null)
                {
                    _button.gameObject.SetActive(false);
                }

                return;
            }

            if (_button != null)
            {
                _button.gameObject.SetActive(true);
                return;
            }

            try
            {
                Create(gui);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Could not add the Restock button to the inventory: {exception}");
            }
        }

        /// <summary>Re-reads the label text; called after a language change.</summary>
        public static void RefreshLabel()
        {
            if (_label != null)
            {
                _label.text = Translations.Get(Translations.RestockLabel);
            }
        }

        private static void Create(InventoryGui gui)
        {
            Button template = gui.m_takeAllButton;
            RectTransform panel = gui.m_player;
            if (template == null || panel == null)
            {
                if (!_warned)
                {
                    _warned = true;
                    Plugin.Log.LogWarning("The inventory has no 'take all' button to copy (another UI mod?); the Restock button is not shown. Use the console command 'tidychests restock'.");
                }

                return;
            }

            Button button = ButtonClone.Create(template, panel, ButtonName, OnClick, out _label);
            StashButton.Place(panel, button, Plugin.Settings.ButtonOffset.Value + Plugin.Settings.RestockButtonOffset.Value);

            _button = button;
            RefreshLabel();
            var rect = (RectTransform)button.transform;
            Plugin.Debug($"Restock button created at {button.transform.localPosition} ({rect.rect.width}x{rect.rect.height})");
        }

        private static void OnClick()
        {
            try
            {
                Restocker.Restock(Player.m_localPlayer);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Restock failed: {exception}");
            }
        }
    }
}
