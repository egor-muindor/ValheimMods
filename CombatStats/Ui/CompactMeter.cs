using System;
using System.Collections.Generic;
using CombatStats.Collect;
using CombatStats.Model;
using CombatStats.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CombatStats.Ui
{
    /// <summary>
    /// The small window that shows the fight as it happens.
    ///
    /// It is a HUD element rather than a panel: a dark backing that thins out towards the bottom,
    /// a hairline above it and text with a shadow, so it reads on snow and stays quiet in a dark
    /// forest. It appears with the first hit and fades out once the fight goes quiet.
    /// </summary>
    internal sealed class CompactMeter : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;

        private const float FadeSeconds = 0.4f;

        private const float PadLeft = 11f;

        private const float PadTop = 7f;

        private const float PadBottom = 9f;

        private const float CaptionHeight = 14f;

        private readonly List<MeterRow> _rows = new List<MeterRow>();

        private RectTransform? _root;

        private RectTransform? _list;

        private CanvasGroup? _group;

        private Image? _background;

        private TMP_Text? _caption;

        private float _nextRefresh;

        private float _alpha;

        private bool _failed;

        /// <summary>True while the window is on screen, however faint.</summary>
        public bool IsVisible => _alpha > 0.01f;

        private void Update()
        {
            if (Plugin.Settings == null)
            {
                return;
            }

            if (Shortcut.IsPressed(Plugin.Settings.CompactKey.Value))
            {
                Cycle();
            }

            bool wanted = Wanted();
            if (wanted && Time.time >= _nextRefresh)
            {
                _nextRefresh = Time.time + RefreshInterval;
                Refresh();
            }

            Fade(wanted);
        }

        /// <summary>Shown while fighting, always shown, off - and back round again.</summary>
        private void Cycle()
        {
            ConfigEntryCycle();
            MeterMode mode = Plugin.Settings.CompactMode.Value;
            string message = mode == MeterMode.Auto
                ? "Combat meter: shown while fighting"
                : mode == MeterMode.Always ? "Combat meter: always shown" : "Combat meter: off";

            MessageHud hud = MessageHud.instance;
            if (hud != null)
            {
                hud.ShowMessage(MessageHud.MessageType.TopLeft, message);
            }

            Plugin.Debug(message);
        }

        private void ConfigEntryCycle()
        {
            switch (Plugin.Settings.CompactMode.Value)
            {
                case MeterMode.Auto:
                    Plugin.Settings.CompactMode.Value = MeterMode.Always;
                    break;
                case MeterMode.Always:
                    Plugin.Settings.CompactMode.Value = MeterMode.Off;
                    break;
                default:
                    Plugin.Settings.CompactMode.Value = MeterMode.Auto;
                    break;
            }
        }

        private bool Wanted()
        {
            if (!Plugin.Enabled || Plugin.Settings.CompactMode.Value == MeterMode.Off)
            {
                return false;
            }

            if (Player.m_localPlayer == null || global::Hud.IsUserHidden() || Menu.IsVisible() || Minimap.IsOpen())
            {
                return false;
            }

            if (Plugin.Settings.CompactMode.Value == MeterMode.Always)
            {
                return true;
            }

            StatsRecorder? recorder = Plugin.Collector.RecorderIfAny(CombatChannel.DamageDealt);
            return recorder != null && Time.time - recorder.LastEventTime <= Plugin.Settings.CompactHideAfter.Value;
        }

        private void Fade(bool wanted)
        {
            float target = wanted ? 1f : 0f;
            if (Mathf.Approximately(_alpha, target))
            {
                if (_group != null && _group.gameObject.activeSelf != (_alpha > 0.01f))
                {
                    _group.gameObject.SetActive(_alpha > 0.01f);
                }

                return;
            }

            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime / FadeSeconds);

            if (_group == null)
            {
                return;
            }

            _group.alpha = _alpha;
            bool active = _alpha > 0.01f;
            if (_group.gameObject.activeSelf != active)
            {
                _group.gameObject.SetActive(active);
            }
        }

        private void Refresh()
        {
            if (!EnsureUi() || _root == null)
            {
                return;
            }

            ModConfig settings = Plugin.Settings;
            float width = Mathf.Max(160f, settings.CompactWidth.Value);
            bool bars = settings.CompactBars.Value;
            float rowWidth = width - (2f * PadLeft);

            WindowSnapshot snapshot = Snapshot();
            List<CombatantRow> shown = Choose(snapshot, settings.CompactRows.Value);
            float leader = shown.Count > 0 ? shown[0].Total : 0f;
            long local = DamageCollector.LocalId();

            float y = 0f;
            if (_caption != null)
            {
                bool caption = settings.CompactCaption.Value;
                _caption.gameObject.SetActive(caption);
                if (caption)
                {
                    _caption.text = $"damage - {Seconds(snapshot.WindowSeconds)}";
                    UiStyle.PlaceTopStretch(_caption.rectTransform, 0f, 0f, 0f, CaptionHeight);
                    y += CaptionHeight;
                }
            }

            for (int index = 0; index < shown.Count; index++)
            {
                MeterRow row = RowAt(index);
                row.SetActive(true);
                row.Apply(shown[index], leader, rowWidth, bars, settings.CompactShare.Value, shown[index].Id == local, string.Empty);
                row.Place(y);
                y += MeterRow.Height(bars);
            }

            for (int index = shown.Count; index < _rows.Count; index++)
            {
                _rows[index].SetActive(false);
            }

            if (_list != null)
            {
                _list.sizeDelta = new Vector2(_list.sizeDelta.x, y);
            }

            _root.sizeDelta = new Vector2(width, PadTop + y + PadBottom);
            _root.localScale = Vector3.one * Mathf.Clamp(settings.CompactScale.Value, 0.5f, 2f);
            UiStyle.Anchor(_root, settings.CompactAnchor.Value, settings.CompactOffset.Value);

            if (_background != null)
            {
                Color colour = _background.color;
                _background.color = new Color(colour.r, colour.g, colour.b, settings.CompactOpacity.Value);
            }
        }

        private WindowSnapshot Snapshot()
        {
            StatsRecorder? recorder = Plugin.Collector.RecorderIfAny(CombatChannel.DamageDealt);
            return recorder == null
                ? WindowSnapshot.Empty
                : recorder.Snapshot(Time.time, Plugin.Settings.CompactWindow.Value, Plugin.Collector.NameOf);
        }

        /// <summary>The rows that fit, with the player's own always among them.</summary>
        private List<CombatantRow> Choose(WindowSnapshot snapshot, int limit)
        {
            var chosen = new List<CombatantRow>();
            long local = DamageCollector.LocalId();
            bool hasLocal = false;

            for (int index = 0; index < snapshot.Rows.Count && chosen.Count < limit; index++)
            {
                chosen.Add(snapshot.Rows[index]);
                hasLocal |= snapshot.Rows[index].Id == local;
            }

            if (hasLocal || chosen.Count < limit)
            {
                return chosen;
            }

            for (int index = limit; index < snapshot.Rows.Count; index++)
            {
                if (snapshot.Rows[index].Id == local)
                {
                    chosen[chosen.Count - 1] = snapshot.Rows[index];
                    break;
                }
            }

            return chosen;
        }

        private static string Seconds(int seconds)
        {
            if (seconds < 60)
            {
                return seconds + " s";
            }

            return seconds % 60 == 0 ? (seconds / 60) + " min" : (seconds / 60) + " min " + (seconds % 60) + " s";
        }

        private MeterRow RowAt(int index)
        {
            while (_rows.Count <= index)
            {
                _rows.Add(MeterRow.Create(_list!, detailed: false, onClick: null));
            }

            return _rows[index];
        }

        private bool EnsureUi()
        {
            if (_failed)
            {
                return false;
            }

            if (_root != null)
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
                if (_root != null)
                {
                    Destroy(_root.gameObject);
                    _root = null;
                }

                Plugin.Log.LogError($"Could not build the compact meter; it stays hidden until restart: {exception}");
                return false;
            }

            return _root != null;
        }

        private void Build()
        {
            _rows.Clear();
            _list = null;
            _caption = null;
            _background = null;
            _group = null;

            Transform? canvas = UiStyle.FindCanvas();
            if (canvas == null || !UiStyle.ReadStyle())
            {
                return;
            }

            var go = new GameObject("CombatStats Compact", typeof(RectTransform), typeof(CanvasGroup));
            _root = (RectTransform)go.transform;
            _root.SetParent(canvas, false);
            _root.sizeDelta = new Vector2(Plugin.Settings.CompactWidth.Value, 80f);

            _group = go.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            go.SetActive(false);

            _background = UiStyle.Box("Background", _root, new Color(0.03f, 0.028f, 0.024f, Plugin.Settings.CompactOpacity.Value));
            _background.sprite = UiStyle.Fade();
            UiStyle.Stretch(_background.rectTransform, 0f, 0f, 0f, 0f);

            Image hairline = UiStyle.Box("Hairline", _root, UiStyle.Hairline);
            hairline.sprite = UiStyle.Flat();
            hairline.rectTransform.anchorMin = new Vector2(0f, 1f);
            hairline.rectTransform.anchorMax = new Vector2(1f, 1f);
            hairline.rectTransform.pivot = new Vector2(0.5f, 1f);
            hairline.rectTransform.sizeDelta = new Vector2(0f, 1f);
            hairline.rectTransform.anchoredPosition = Vector2.zero;

            var listGo = new GameObject("List", typeof(RectTransform));
            _list = (RectTransform)listGo.transform;
            _list.SetParent(_root, false);
            UiStyle.PlaceTopStretch(_list, PadLeft, PadLeft, PadTop, 40f);

            _caption = UiStyle.Text("Caption", _list, 12f, TextAlignmentOptions.MidlineLeft, UiStyle.Dim);

            Plugin.Debug("Compact meter built");
        }
    }
}
