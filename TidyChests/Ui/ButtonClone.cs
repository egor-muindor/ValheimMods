using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// Copies a vanilla inventory button (its sprite, font and hover effects) for one of the
    /// mod's own, without the template's gamepad hotkey or click listeners.
    /// </summary>
    internal static class ButtonClone
    {
        /// <summary>Clones <paramref name="template"/> under <paramref name="parent"/> and returns the copy and its label.</summary>
        public static Button Create(Button template, Transform parent, string name, UnityAction onClick, out TMP_Text? label)
        {
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
                button = Object.Instantiate(template, parent);
            }
            finally
            {
                if (gamepad != null)
                {
                    gamepad.enabled = gamepadEnabled;
                }
            }

            button.name = name;
            UIGamePad copiedGamepad = button.GetComponent<UIGamePad>();
            if (copiedGamepad != null)
            {
                if (copiedGamepad.m_hint != null)
                {
                    copiedGamepad.m_hint.SetActive(false);
                }

                Object.Destroy(copiedGamepad);
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = Mathf.Min(10f, label.fontSize);
            }

            return button;
        }
    }
}
