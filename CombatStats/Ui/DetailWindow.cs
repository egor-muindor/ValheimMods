using System;
using System.Collections.Generic;
using CombatStats.Model;
using CombatStats.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CombatStats.Ui
{
    /// <summary>
    /// The window with the whole picture: a tab per window length, a tab per meter that is turned
    /// on, and one row per player that opens into the damage kinds it is made of.
    /// </summary>
    internal sealed class DetailWindow : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;

        private const float Padding = 14f;

        private const float ScrollStep = 48f;

        private static readonly CombatChannel[] Channels =
        {
            CombatChannel.DamageDealt,
            CombatChannel.DamageTaken,
            CombatChannel.Healing,
            CombatChannel.ObjectDamage,
        };

        private readonly List<MeterRow> _rows = new List<MeterRow>();

        private readonly List<KindLine> _kindLines = new List<KindLine>();

        private readonly List<Button> _windowTabs = new List<Button>();

        private readonly List<Button> _channelTabs = new List<Button>();

        private readonly HashSet<long> _expanded = new HashSet<long>();

        private RectTransform? _panel;

        private RectTransform? _content;

        private RectTransform? _viewport;

        private TMP_Text? _summary;

        private TMP_Text? _empty;

        private List<int> _windows = new List<int>();

        private int _window = 30;

        private CombatChannel _channel = CombatChannel.DamageDealt;

        private float _nextRefresh;

        private float _scrolled;

        private bool _failed;

        private bool _open;

        /// <summary>True while the window has the screen, which is what stops the player moving.</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>The running window, once the plugin has loaded.</summary>
        public static DetailWindow? Instance { get; private set; }

        /// <summary>The mouse wheel, handed over by the patch that takes it from the game.</summary>
        public static void FeedScrollWheel(float amount)
        {
            DetailWindow? window = Instance;
            if (window == null || !window._open)
            {
                return;
            }

            window._scrolled = Mathf.Max(0f, window._scrolled - (Mathf.Sign(amount) * ScrollStep));
            window.Layout();
        }

        /// <summary>Opens the window, or closes it when it is already up.</summary>
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
            _scrolled = 0f;
            _panel.gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Closes the window and gives the player back their hands.</summary>
        public void Close()
        {
            _open = false;
            IsOpen = false;
            SettingsPanel.Instance?.Close();
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

        private void Update()
        {
            if (Plugin.Settings == null || !Plugin.Enabled)
            {
                return;
            }

            if (Shortcut.IsPressed(Plugin.Settings.DetailKey.Value))
            {
                Toggle();
            }

            if (_open && Time.time >= _nextRefresh)
            {
                _nextRefresh = Time.time + RefreshInterval;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_panel == null)
            {
                return;
            }

            Vector2 wanted = Plugin.Settings.DetailSize.Value;
            var size = new Vector2(Mathf.Max(420f, wanted.x), Mathf.Max(280f, wanted.y));
            if (_panel.sizeDelta != size)
            {
                _panel.sizeDelta = size;
            }

            RefreshTabs();
            Layout();
        }

        /// <summary>Rebuilds the two rows of tabs when the settings changed what is on offer.</summary>
        private void RefreshTabs()
        {
            List<int> windows = Plugin.Settings.ParseWindows();
            if (!SameWindows(windows))
            {
                _windows = windows;
                BuildWindowTabs();
            }

            if (!_windows.Contains(_window))
            {
                _window = _windows.Count > 0 ? _windows[0] : 30;
            }

            for (int index = 0; index < _windowTabs.Count && index < _windows.Count; index++)
            {
                UiStyle.LabelOf(_windowTabs[index]).color = _windows[index] == _window ? UiStyle.Mine : UiStyle.Dim;
            }

            for (int index = 0; index < _channelTabs.Count; index++)
            {
                CombatChannel channel = Channels[index];
                bool available = Available(channel);
                _channelTabs[index].gameObject.SetActive(available);
                UiStyle.LabelOf(_channelTabs[index]).color = channel == _channel ? UiStyle.Mine : UiStyle.Dim;
            }

            if (!Available(_channel))
            {
                _channel = CombatChannel.DamageDealt;
            }
        }

        private static bool Available(CombatChannel channel)
        {
            switch (channel)
            {
                case CombatChannel.DamageTaken: return Plugin.Settings.CountDamageTaken.Value;
                case CombatChannel.Healing: return Plugin.Settings.CountHealing.Value;
                case CombatChannel.ObjectDamage: return Plugin.Settings.CountObjectDamage.Value;
                default: return true;
            }
        }

        private bool SameWindows(List<int> windows)
        {
            if (_windows.Count != windows.Count)
            {
                return false;
            }

            for (int index = 0; index < windows.Count; index++)
            {
                if (_windows[index] != windows[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Fills the rows in and places everything the scroll area holds.</summary>
        private void Layout()
        {
            if (_content == null || _viewport == null || _summary == null)
            {
                return;
            }

            StatsRecorder? recorder = Plugin.Collector.RecorderIfAny(_channel);
            WindowSnapshot snapshot = recorder == null
                ? WindowSnapshot.Empty
                : recorder.Snapshot(Time.time, _window, Plugin.Collector.NameOf);

            _summary.text = snapshot.Hits == 0
                ? "nothing in this window"
                : $"total <color=#{ColorUtility.ToHtmlStringRGB(UiStyle.FontColor)}>{MeterRow.Format(snapshot.Total)}</color>" +
                  $"     hits <color=#{ColorUtility.ToHtmlStringRGB(UiStyle.FontColor)}>{snapshot.Hits}</color>" +
                  $"     average <color=#{ColorUtility.ToHtmlStringRGB(UiStyle.FontColor)}>{MeterRow.Format(snapshot.Average)}</color>" +
                  $"     largest <color=#{ColorUtility.ToHtmlStringRGB(UiStyle.FontColor)}>{MeterRow.Format(snapshot.Max)}</color>";

            float width = _viewport.rect.width;
            float leader = snapshot.Rows.Count > 0 ? snapshot.Rows[0].Total : 0f;
            long local = LocalId();

            if (_expanded.Count == 0 && local != 0L)
            {
                _expanded.Add(local);
            }

            int rowIndex = 0;
            int kindIndex = 0;
            float y = 0f;

            foreach (CombatantRow row in snapshot.Rows)
            {
                MeterRow meter = RowAt(rowIndex++);
                bool open = _expanded.Contains(row.Id);
                meter.SetActive(true);
                meter.Apply(row, leader, width, bars: true, showShare: false, local: row.Id == local, prefix: open ? "- " : "+ ");
                meter.Place(y);
                y += MeterRow.Height(bars: true);

                if (!open)
                {
                    continue;
                }

                for (int kind = 0; kind < DamageKinds.Count; kind++)
                {
                    float amount = row.ByKind[kind];
                    if (amount <= 0f)
                    {
                        continue;
                    }

                    KindLine line = KindAt(kindIndex++);
                    line.SetActive(true);
                    line.Apply((DamageKind)kind, amount, row.Total > 0f ? amount / row.Total : 0f);
                    line.Place(y);
                    y += KindLine.Height;
                }

                y += 4f;
            }

            for (int index = rowIndex; index < _rows.Count; index++)
            {
                _rows[index].SetActive(false);
            }

            for (int index = kindIndex; index < _kindLines.Count; index++)
            {
                _kindLines[index].SetActive(false);
            }

            if (_empty != null)
            {
                _empty.gameObject.SetActive(rowIndex == 0);
            }

            float height = Mathf.Max(y, _viewport.rect.height);
            _content.sizeDelta = new Vector2(0f, height);
            _scrolled = Mathf.Clamp(_scrolled, 0f, Mathf.Max(0f, height - _viewport.rect.height));
            _content.anchoredPosition = new Vector2(0f, _scrolled);
        }

        private static long LocalId()
        {
            Player local = Player.m_localPlayer;
            return local != null ? local.GetZDOID().UserID : 0L;
        }

        private void OnRowClicked(long id)
        {
            if (!_expanded.Remove(id))
            {
                _expanded.Add(id);
            }

            Layout();
        }

        private MeterRow RowAt(int index)
        {
            while (_rows.Count <= index)
            {
                _rows.Add(MeterRow.Create(_content!, detailed: true, OnRowClicked));
            }

            return _rows[index];
        }

        private KindLine KindAt(int index)
        {
            while (_kindLines.Count <= index)
            {
                _kindLines.Add(KindLine.Create(_content!));
            }

            return _kindLines[index];
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

                Plugin.Log.LogError($"Could not build the detail window; it stays closed until restart: {exception}");
                return false;
            }

            return _panel != null;
        }

        private void Build()
        {
            _rows.Clear();
            _kindLines.Clear();
            _windowTabs.Clear();
            _channelTabs.Clear();
            _windows = new List<int>();
            _content = null;
            _viewport = null;
            _summary = null;
            _empty = null;

            Transform? canvas = UiStyle.FindCanvas();
            if (canvas == null || !UiStyle.ReadStyle())
            {
                Plugin.Debug("The detail window waits for the game UI");
                return;
            }

            Vector2 size = Plugin.Settings.DetailSize.Value;
            var go = new GameObject("CombatStats Detail", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            _panel = (RectTransform)go.transform;
            _panel.SetParent(canvas, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(Mathf.Max(420f, size.x), Mathf.Max(280f, size.y));

            Canvas own = go.GetComponent<Canvas>();
            own.overrideSorting = true;
            own.sortingOrder = 100;

            UiStyle.StylePanel(go.GetComponent<Image>());

            TMP_Text title = UiStyle.Text("Title", _panel, 20f, TextAlignmentOptions.MidlineLeft, UiStyle.FontColor);
            UiStyle.PlaceTopLeft(title.rectTransform, Padding, 10f, 300f, 26f);
            title.text = "Combat Stats";

            Button settings = UiStyle.TextButton("Settings", _panel, "settings", 14f, UiStyle.Dim, OpenSettings);
            RectTransform settingsRect = (RectTransform)settings.transform;
            settingsRect.anchorMin = new Vector2(1f, 1f);
            settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.pivot = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = new Vector2(-Padding, -12f);
            settingsRect.sizeDelta = new Vector2(84f, 22f);

            BuildWindowTabs();
            BuildChannelTabs();

            _summary = UiStyle.Text("Summary", _panel, 13f, TextAlignmentOptions.MidlineLeft, UiStyle.Dim);
            UiStyle.PlaceTopStretch(_summary.rectTransform, Padding, Padding, 100f, 18f);

            TMP_Text header = UiStyle.Text("Header", _panel, 11f, TextAlignmentOptions.MidlineLeft, UiStyle.Dim);
            UiStyle.PlaceTopStretch(header.rectTransform, Padding, Padding, 122f, 14f);
            header.text = "player";

            TMP_Text columns = UiStyle.Text("Columns", _panel, 11f, TextAlignmentOptions.MidlineRight, UiStyle.Dim);
            UiStyle.PlaceTopStretch(columns.rectTransform, Padding, Padding, 122f, 14f);
            columns.text = "damage         share      hits        avg";

            Image separator = UiStyle.Box("Separator", _panel, new Color(0.45f, 0.38f, 0.25f, 0.5f));
            separator.sprite = UiStyle.Flat();
            UiStyle.PlaceTopStretch(separator.rectTransform, Padding, Padding, 138f, 1f);

            BuildScroll();

            _empty = UiStyle.Text("Empty", _viewport!, 14f, TextAlignmentOptions.Center, UiStyle.Dim);
            _empty.text = "Nothing recorded in this window yet.";
            _empty.gameObject.SetActive(false);

            go.SetActive(false);
            Plugin.Debug($"Detail window built ({_panel.sizeDelta.x}x{_panel.sizeDelta.y})");
        }

        private void BuildScroll()
        {
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            _viewport = (RectTransform)viewportGo.transform;
            _viewport.SetParent(_panel, false);
            UiStyle.Stretch(_viewport, Padding, Padding, 146f, Padding);

            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            _content = (RectTransform)contentGo.transform;
            _content.SetParent(_viewport, false);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 0f);
        }

        private void BuildWindowTabs()
        {
            foreach (Button tab in _windowTabs)
            {
                Destroy(tab.gameObject);
            }

            _windowTabs.Clear();
            if (_panel == null)
            {
                return;
            }

            if (_windows.Count == 0)
            {
                _windows = Plugin.Settings.ParseWindows();
                _window = _windows.Count > 0 ? _windows[0] : 30;
            }

            float x = Padding;
            foreach (int seconds in _windows)
            {
                int captured = seconds;
                Button tab = UiStyle.TextButton("Window" + seconds, _panel, Label(seconds), 14f, UiStyle.Dim, () =>
                {
                    _window = captured;
                    Refresh();
                });

                var rect = (RectTransform)tab.transform;
                UiStyle.PlaceTopLeft(rect, x, 42f, 72f, 22f);
                x += 74f;
                _windowTabs.Add(tab);
            }
        }

        private void BuildChannelTabs()
        {
            if (_panel == null)
            {
                return;
            }

            float x = Padding;
            foreach (CombatChannel channel in Channels)
            {
                CombatChannel captured = channel;
                Button tab = UiStyle.TextButton("Channel" + channel, _panel, Label(channel), 13f, UiStyle.Dim, () =>
                {
                    _channel = captured;
                    _scrolled = 0f;
                    Refresh();
                });

                var rect = (RectTransform)tab.transform;
                UiStyle.PlaceTopLeft(rect, x, 70f, 86f, 20f);
                x += 88f;
                _channelTabs.Add(tab);
            }
        }

        private void OpenSettings()
        {
            SettingsPanel panel = SettingsPanel.Instance ?? gameObject.AddComponent<SettingsPanel>();
            panel.Toggle();
        }

        private static string Label(int seconds)
        {
            if (seconds < 60)
            {
                return seconds + " s";
            }

            return seconds % 60 == 0 ? (seconds / 60) + " min" : (seconds / 60f).ToString("0.#") + " min";
        }

        private static string Label(CombatChannel channel)
        {
            switch (channel)
            {
                case CombatChannel.DamageTaken: return "taken";
                case CombatChannel.Healing: return "healing";
                case CombatChannel.ObjectDamage: return "objects";
                default: return "damage";
            }
        }

        /// <summary>One damage kind under an opened row.</summary>
        private sealed class KindLine
        {
            public const float Height = 15f;

            private readonly RectTransform _root;

            private readonly TMP_Text _name;

            private readonly TMP_Text _amount;

            private readonly TMP_Text _share;

            private KindLine(RectTransform root, TMP_Text name, TMP_Text amount, TMP_Text share)
            {
                _root = root;
                _name = name;
                _amount = amount;
                _share = share;
            }

            public static KindLine Create(RectTransform parent)
            {
                var go = new GameObject("Kind", typeof(RectTransform));
                var root = (RectTransform)go.transform;
                root.SetParent(parent, false);
                root.anchorMin = new Vector2(0f, 1f);
                root.anchorMax = new Vector2(1f, 1f);
                root.pivot = new Vector2(0f, 1f);
                root.sizeDelta = new Vector2(0f, Height);

                TMP_Text name = UiStyle.Text("Name", root, 12.5f, TextAlignmentOptions.MidlineLeft, UiStyle.Dim);
                name.rectTransform.offsetMin = new Vector2(22f, 0f);
                name.rectTransform.offsetMax = new Vector2(-250f, 0f);

                TMP_Text amount = UiStyle.Text("Amount", root, 12.5f, TextAlignmentOptions.MidlineRight, UiStyle.Dim);
                amount.rectTransform.anchorMin = new Vector2(1f, 0f);
                amount.rectTransform.anchorMax = new Vector2(1f, 1f);
                amount.rectTransform.offsetMin = new Vector2(-250f, 0f);
                amount.rectTransform.offsetMax = new Vector2(-152f, 0f);

                TMP_Text share = UiStyle.Text("Share", root, 12.5f, TextAlignmentOptions.MidlineRight, UiStyle.Dim);
                share.rectTransform.anchorMin = new Vector2(1f, 0f);
                share.rectTransform.anchorMax = new Vector2(1f, 1f);
                share.rectTransform.offsetMin = new Vector2(-148f, 0f);
                share.rectTransform.offsetMax = new Vector2(-104f, 0f);

                return new KindLine(root, name, amount, share);
            }

            public void Apply(DamageKind kind, float amount, float share)
            {
                _name.text = KindName(kind);
                _name.color = UiStyle.Colors[(int)kind];
                _amount.text = MeterRow.Format(amount);
                _share.text = (share * 100f).ToString("0") + "%";
            }

            public void SetActive(bool active)
            {
                if (_root.gameObject.activeSelf != active)
                {
                    _root.gameObject.SetActive(active);
                }
            }

            public void Place(float y)
            {
                _root.anchoredPosition = new Vector2(0f, -y);
            }

            /// <summary>The game's own word for the kind, or the English one when it has none.</summary>
            private static string KindName(DamageKind kind)
            {
                string key = DamageKinds.LocalizationKey(kind);
                Localization localization = Localization.instance;
                if (localization == null)
                {
                    return DamageKinds.EnglishName(kind);
                }

                string localized = localization.Localize(key);
                return string.IsNullOrEmpty(localized) || localized.StartsWith("[", StringComparison.Ordinal) || localized.Contains("$")
                    ? DamageKinds.EnglishName(kind)
                    : localized;
            }
        }
    }
}
