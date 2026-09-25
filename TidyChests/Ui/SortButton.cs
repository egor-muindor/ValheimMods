using System;
using TidyChests.Sort;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// The Sort button on the chest panel: a copy of the vanilla "stack all" button, placed
    /// next to it on the far side from "take all", so the row of chest buttons simply grows by
    /// one. Being part of the chest panel it is only visible while a chest is open.
    /// </summary>
    internal static class SortButton
    {
        private const string ButtonName = "TidyChests Sort";

        private const float Gap = 6f;

        private static Button? _button;

        private static TMP_Text? _label;

        private static bool _warned;

        /// <summary>Creates or shows the button on <paramref name="gui"/>, honouring the <c>ShowSortButton</c> setting.</summary>
        public static void Ensure(InventoryGui gui)
        {
            if (!Plugin.Enabled || !Plugin.Settings.ShowSortButton.Value)
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
                Plugin.Log.LogError($"Could not add the Sort button to the chest panel: {exception}");
            }
        }

        /// <summary>Re-reads the label text; called after a language change.</summary>
        public static void RefreshLabel()
        {
            if (_label != null)
            {
                _label.text = Translations.Get(Translations.SortLabel);
            }
        }

        private static void Create(InventoryGui gui)
        {
            Button template = gui.m_stackAllButton;
            if (template == null || gui.m_container == null)
            {
                if (!_warned)
                {
                    _warned = true;
                    Plugin.Log.LogWarning("The chest panel has no 'stack all' button to copy (another UI mod?); the Sort button is not shown. Use the console command 'tidychests sort'.");
                }

                return;
            }

            Button button = ButtonClone.Create(template, template.transform.parent, ButtonName, OnClick, out _label);

            var rect = (RectTransform)button.transform;
            var templateRect = (RectTransform)template.transform;
            Vector3 step = NextStep(gui.m_takeAllButton, template, templateRect);
            Vector2 offset = Plugin.Settings.SortButtonOffset.Value;
            button.transform.localPosition = template.transform.localPosition + step + new Vector3(offset.x, offset.y, 0f);

            _button = button;
            RefreshLabel();
            Plugin.Debug($"Sort button created at {button.transform.localPosition} ({rect.rect.width}x{rect.rect.height}), " +
                         $"stack all at {template.transform.localPosition}, take all at {(gui.m_takeAllButton != null ? gui.m_takeAllButton.transform.localPosition.ToString() : "none")}");
        }

        /// <summary>
        /// The distance from "stack all" to the new button: the same as from "take all" to
        /// "stack all" when both sit side by side, otherwise one button height further down.
        /// </summary>
        private static Vector3 NextStep(Button? takeAll, Button stackAll, RectTransform stackRect)
        {
            if (takeAll != null && takeAll.transform.parent == stackAll.transform.parent)
            {
                Vector3 step = stackAll.transform.localPosition - takeAll.transform.localPosition;
                if (step.magnitude > 1f)
                {
                    return step;
                }
            }

            return new Vector3(0f, -(stackRect.rect.height + Gap), 0f);
        }

        private static void OnClick()
        {
            try
            {
                ChestSorter.SortOpenChest();
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Sort failed: {exception}");
            }
        }
    }
}
