using System;
using System.Collections.Generic;
using System.Globalization;
using CombatStats.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CombatStats.Ui
{
    /// <summary>
    /// The settings that get changed often, in game: the two windows, what they show and where,
    /// and the meters that are off by default. Keys, colours, the share radius and the rest stay
    /// in <c>muindor.CombatStats.cfg</c>, where they are changed once and forgotten.
    ///
    /// Every control writes its config entry straight away, so what is on screen and what is in
    /// the file never disagree.
    /// </summary>
    internal sealed class SettingsPanel : MonoBehaviour
    {
        private const float Padding = 16f;

        private const float RowHeight = 24f;

        private const float ColumnWidth = 300f;

        private readonly List<Control> _rows = new List<Control>();

        private RectTransform? _panel;

        private bool _failed;

        private bool _open;

        /// <summary>The running panel, once it has been opened for the first time.</summary>
        public static SettingsPanel? Instance { get; private set; }

        /// <summary>True while the panel has the screen.</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>Opens the panel, or closes it when it is already up.</summary>
        public void Toggle()
        {
            if (_open)
            {
                Close();
                return;
            }

            if (!EnsureUi() || _panel == null)
            {
                return;
            }

            _open = true;
            IsOpen = true;
            _panel.gameObject.SetActive(true);
            RefreshValues();
        }

        /// <summary>Closes the panel.</summary>
        public void Close()
        {
            _open = false;
            IsOpen = false;
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsOpen = false;
            }
        }

        private void RefreshValues()
        {
            foreach (Control row in _rows)
            {
                row.Refresh();
            }
        }

        private bool EnsureUi()
        {
            if (_failed)
            {
                return false;
            }

            if (_panel != null)
            {
                return true;
            }

            try
            {
                Build();
            }
            catch (Exception exception)
            {
                _failed = true;
                if (_panel != null)
                {
                    Destroy(_panel.gameObject);
                    _panel = null;
                }

                Plugin.Log.LogError($"Could not build the settings panel; the config file still works: {exception}");
                return false;
            }

            return _panel != null;
        }

        private void Build()
        {
            _rows.Clear();

            Transform? canvas = UiStyle.FindCanvas();
            if (canvas == null || !UiStyle.ReadStyle())
            {
                return;
            }

            var go = new GameObject("CombatStats Settings", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            _panel = (RectTransform)go.transform;
            _panel.SetParent(canvas, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(680f, 470f);

            Canvas own = go.GetComponent<Canvas>();
            own.overrideSorting = true;
            own.sortingOrder = 110;

            UiStyle.StylePanel(go.GetComponent<Image>());

            TMP_Text title = UiStyle.Text("Title", _panel, 19f, TextAlignmentOptions.MidlineLeft, UiStyle.FontColor);
            UiStyle.PlaceTopLeft(title.rectTransform, Padding, 10f, 400f, 24f);
            title.text = "Combat Stats - settings";

            Button close = UiStyle.TextButton("Close", _panel, "close", 14f, UiStyle.Dim, Close);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-Padding, -12f);
            closeRect.sizeDelta = new Vector2(70f, 22f);

            ModConfig settings = Plugin.Settings;

            float left = Padding;
            float right = Padding + ColumnWidth + 40f;
            float y = 46f;

            y = Heading("Compact window", left, y);
            y = Choice(left, y, "When to show", () => Word(settings.CompactMode.Value), step =>
                settings.CompactMode.Value = (MeterMode)Wrap((int)settings.CompactMode.Value + step, 3));
            y = Number(left, y, "Window", () => settings.CompactWindow.Value + " s", step =>
                settings.CompactWindow.Value = Mathf.Clamp(settings.CompactWindow.Value + (step * 5), 5, 600));
            y = Number(left, y, "Rows", () => settings.CompactRows.Value.ToString(CultureInfo.InvariantCulture), step =>
                settings.CompactRows.Value = Mathf.Clamp(settings.CompactRows.Value + step, 1, 12));
            y = Number(left, y, "Hide after", () => settings.CompactHideAfter.Value.ToString("0", CultureInfo.InvariantCulture) + " s", step =>
                settings.CompactHideAfter.Value = Mathf.Clamp(settings.CompactHideAfter.Value + step, 1f, 120f));
            y = Choice(left, y, "Corner", () => Word(settings.CompactAnchor.Value), step =>
                settings.CompactAnchor.Value = (ScreenAnchor)Wrap((int)settings.CompactAnchor.Value + step, 6));
            y = Number(left, y, "Offset across", () => settings.CompactOffset.Value.x.ToString("0", CultureInfo.InvariantCulture), step =>
                settings.CompactOffset.Value = new Vector2(settings.CompactOffset.Value.x + (step * 10f), settings.CompactOffset.Value.y));
            y = Number(left, y, "Offset down", () => settings.CompactOffset.Value.y.ToString("0", CultureInfo.InvariantCulture), step =>
                settings.CompactOffset.Value = new Vector2(settings.CompactOffset.Value.x, settings.CompactOffset.Value.y + (step * 10f)));
            y = Number(left, y, "Width", () => settings.CompactWidth.Value.ToString("0", CultureInfo.InvariantCulture), step =>
                settings.CompactWidth.Value = Mathf.Clamp(settings.CompactWidth.Value + (step * 20f), 160f, 700f));
            y = Number(left, y, "Size", () => settings.CompactScale.Value.ToString("0.00", CultureInfo.InvariantCulture), step =>
                settings.CompactScale.Value = Mathf.Clamp(settings.CompactScale.Value + (step * 0.05f), 0.5f, 2f));
            y = Number(left, y, "Backing", () => settings.CompactOpacity.Value.ToString("0.00", CultureInfo.InvariantCulture), step =>
                settings.CompactOpacity.Value = Mathf.Clamp(settings.CompactOpacity.Value + (step * 0.05f), 0.1f, 1f));
            y = Toggle(left, y, "Caption line", () => settings.CompactCaption.Value, value => settings.CompactCaption.Value = value);
            y = Toggle(left, y, "Percentages", () => settings.CompactShare.Value, value => settings.CompactShare.Value = value);
            Toggle(left, y, "Bars", () => settings.CompactBars.Value, value => settings.CompactBars.Value = value);

            y = 46f;
            y = Heading("Detail window", right, y);
            y = Number(right, y, "Width", () => settings.DetailSize.Value.x.ToString("0", CultureInfo.InvariantCulture), step =>
                settings.DetailSize.Value = new Vector2(Mathf.Clamp(settings.DetailSize.Value.x + (step * 20f), 420f, 1400f), settings.DetailSize.Value.y));
            y = Number(right, y, "Height", () => settings.DetailSize.Value.y.ToString("0", CultureInfo.InvariantCulture), step =>
                settings.DetailSize.Value = new Vector2(settings.DetailSize.Value.x, Mathf.Clamp(settings.DetailSize.Value.y + (step * 20f), 280f, 1000f)));

            y += 10f;
            y = Heading("Also count", right, y);
            y = Toggle(right, y, "Damage taken", () => settings.CountDamageTaken.Value, value => settings.CountDamageTaken.Value = value);
            y = Toggle(right, y, "Healing", () => settings.CountHealing.Value, value => settings.CountHealing.Value = value);
            y = Toggle(right, y, "Trees, ore, buildings", () => settings.CountObjectDamage.Value, value => settings.CountObjectDamage.Value = value);
            y = Toggle(right, y, "Pets and summons", () => settings.CountPets.Value, value => settings.CountPets.Value = value);

            y += 10f;
            y = Heading("Sharing", right, y);
            Toggle(right, y, "Send my numbers", () => settings.ShareDamage.Value, value => settings.ShareDamage.LocalValue = value, () => settings.ShareDamage.IsLocked);

            TMP_Text hint = UiStyle.Text("Hint", _panel, 12f, TextAlignmentOptions.MidlineLeft, UiStyle.Dim);
            UiStyle.PlaceTopLeft(hint.rectTransform, Padding, _panel.sizeDelta.y - 30f, _panel.sizeDelta.x - (2f * Padding), 18f);
            hint.text = "Keys, colours and the share radius live in the config file. A setting a server decides cannot be changed here.";

            go.SetActive(false);
            Plugin.Debug("Settings panel built");
        }

        private float Heading(string text, float x, float y)
        {
            TMP_Text label = UiStyle.Text("Heading", _panel!, 13f, TextAlignmentOptions.MidlineLeft, UiStyle.Mine);
            UiStyle.PlaceTopLeft(label.rectTransform, x, y, ColumnWidth, 20f);
            label.text = text;

            Image line = UiStyle.Box("HeadingLine", _panel!, new Color(0.45f, 0.38f, 0.25f, 0.4f));
            line.sprite = UiStyle.Flat();
            UiStyle.PlaceTopLeft(line.rectTransform, x, y + 19f, ColumnWidth, 1f);

            return y + 26f;
        }

        private float Toggle(float x, float y, string label, Func<bool> read, Action<bool> write, Func<bool>? locked = null)
        {
            return Row(x, y, label, () => read() ? "on" : "off", step =>
            {
                if (step != 0)
                {
                    write(!read());
                }
            }, locked);
        }

        private float Choice(float x, float y, string label, Func<string> read, Action<int> step)
        {
            return Row(x, y, label, read, step);
        }

        private float Number(float x, float y, string label, Func<string> read, Action<int> step)
        {
            return Row(x, y, label, read, step);
        }

        private float Row(float x, float y, string label, Func<string> read, Action<int> step, Func<bool>? locked = null)
        {
            var go = new GameObject("Setting", typeof(RectTransform));
            var root = (RectTransform)go.transform;
            root.SetParent(_panel, false);
            UiStyle.PlaceTopLeft(root, x, y, ColumnWidth, RowHeight);

            TMP_Text caption = UiStyle.Text("Label", root, 13.5f, TextAlignmentOptions.MidlineLeft, UiStyle.FontColor);
            caption.rectTransform.offsetMax = new Vector2(-120f, 0f);
            caption.text = label;

            TMP_Text value = UiStyle.Text("Value", root, 13.5f, TextAlignmentOptions.Center, UiStyle.Dim);
            value.rectTransform.anchorMin = new Vector2(1f, 0f);
            value.rectTransform.anchorMax = new Vector2(1f, 1f);
            value.rectTransform.offsetMin = new Vector2(-92f, 0f);
            value.rectTransform.offsetMax = new Vector2(-24f, 0f);

            Func<string> reading = locked == null
                ? read
                : () => locked() ? read() + " (server)" : read();

            Action<int> stepping = locked == null
                ? step
                : amount =>
                {
                    if (locked())
                    {
                        Plugin.Log.LogInfo("That setting is decided by the server while you are connected to it.");
                        return;
                    }

                    step(amount);
                };

            var row = new Control(value, reading);

            Button less = UiStyle.TextButton("Less", root, "<", 15f, UiStyle.Dim, () =>
            {
                stepping(-1);
                Apply(row);
            });
            Place(less, -116f, -94f);

            Button more = UiStyle.TextButton("More", root, ">", 15f, UiStyle.Dim, () =>
            {
                stepping(1);
                Apply(row);
            });
            Place(more, -22f, 0f);

            row.Refresh();
            _rows.Add(row);
            return y + RowHeight;
        }

        private void Apply(Control row)
        {
            row.Refresh();
            RefreshValues();
        }

        private static void Place(Button button, float from, float to)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(from, 2f);
            rect.offsetMax = new Vector2(to, -2f);
        }

        private static int Wrap(int value, int count)
        {
            return ((value % count) + count) % count;
        }

        private static string Word(MeterMode mode)
        {
            switch (mode)
            {
                case MeterMode.Always: return "always";
                case MeterMode.Off: return "off";
                default: return "fighting";
            }
        }

        private static string Word(ScreenAnchor anchor)
        {
            switch (anchor)
            {
                case ScreenAnchor.TopRight: return "top right";
                case ScreenAnchor.MiddleLeft: return "left";
                case ScreenAnchor.MiddleRight: return "right";
                case ScreenAnchor.BottomLeft: return "bottom left";
                case ScreenAnchor.BottomRight: return "bottom right";
                default: return "top left";
            }
        }

        /// <summary>One setting on screen, and how to read it back out of the config.</summary>
        private sealed class Control
        {
            private readonly TMP_Text _value;

            private readonly Func<string> _read;

            public Control(TMP_Text value, Func<string> read)
            {
                _value = value;
                _read = read;
            }

            public void Refresh()
            {
                _value.text = _read();
            }
        }
    }
}
