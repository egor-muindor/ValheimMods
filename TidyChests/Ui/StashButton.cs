using System;
using HarmonyLib;
using TidyChests.Stash;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// The Stash button in the player's inventory panel: a copy of the vanilla "take all"
    /// button, placed next to the weight display and labelled through the game's
    /// localization. Created the first time the inventory opens and again after a world
    /// change, when the game builds a new inventory GUI.
    /// </summary>
    internal static class StashButton
    {
        private const string ButtonName = "TidyChests Stash";

        private static Button? _button;

        private static TMP_Text? _label;

        private static bool _warned;

        /// <summary>Creates or shows the button on <paramref name="gui"/>, honouring the <c>ShowButton</c> setting.</summary>
        public static void Ensure(InventoryGui gui)
        {
            if (!Plugin.Enabled || !Plugin.Settings.ShowButton.Value)
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
                Plugin.Log.LogError($"Could not add the Stash button to the inventory: {exception}");
            }
        }

        /// <summary>Re-reads the label text; called after a language change.</summary>
        public static void RefreshLabel()
        {
            if (_label != null)
            {
                _label.text = Translations.Get(Translations.Button);
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
                    Plugin.Log.LogWarning("The inventory has no 'take all' button to copy (another UI mod?); the Stash button is not shown. Use the console command 'tidychests stash'.");
                }

                return;
            }

            // The gamepad hotkey of the template must not be copied; it is disabled while cloning.
            UIGamePad gamepad = template.GetComponent<UIGamePad>();
            bool gamepadEnabled = gamepad != null && gamepad.enabled;
            if (gamepad != null)
            {
                gamepad.enabled = false;
            }

            Button button;
            try
            {
                button = UnityEngine.Object.Instantiate(template, panel);
            }
            finally
            {
                if (gamepad != null)
                {
                    gamepad.enabled = gamepadEnabled;
                }
            }

            button.name = ButtonName;
            UIGamePad copiedGamepad = button.GetComponent<UIGamePad>();
            if (copiedGamepad != null)
            {
                if (copiedGamepad.m_hint != null)
                {
                    copiedGamepad.m_hint.SetActive(false);
                }

                UnityEngine.Object.Destroy(copiedGamepad);
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);

            var rect = (RectTransform)button.transform;
            Vector2 size = Plugin.Settings.ButtonSize.Value;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(20f, size.x));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(16f, size.y));

            Vector2 offset = Plugin.Settings.ButtonOffset.Value;
            Transform weight = panel.Find("Weight");
            if (weight != null)
            {
                button.transform.localPosition = weight.localPosition + new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-16f, -16f) + offset;
                Plugin.Debug("Weight display not found in the inventory panel; the Stash button is anchored to the top-right corner");
            }

            _label = button.GetComponentInChildren<TMP_Text>(true);
            if (_label != null)
            {
                _label.enableAutoSizing = true;
                _label.fontSizeMax = _label.fontSize;
                _label.fontSizeMin = Mathf.Min(10f, _label.fontSize);
            }

            _button = button;
            RefreshLabel();
            Plugin.Debug($"Stash button created at {button.transform.localPosition} ({rect.rect.width}x{rect.rect.height})");
        }

        private static void OnClick()
        {
            try
            {
                Stasher.Stash(Player.m_localPlayer);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Stash failed: {exception}");
            }
        }
    }

    /// <summary>Adds the button whenever the inventory opens; the game builds a new GUI per world.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    internal static class InventoryGui_Show_Patch
    {
        private static void Postfix(InventoryGui __instance)
        {
            StashButton.Ensure(__instance);
        }
    }
}
