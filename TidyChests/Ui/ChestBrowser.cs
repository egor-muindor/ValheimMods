using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using GUIFramework;
using TidyChests.Find;
using TidyChests.Index;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// The chest list: everything the containers in range hold, one row per item kind, with a
    /// search box. Clicking a row highlights every chest that holds the item, the same way the
    /// find key does.
    ///
    /// Costs nothing on the wire: the contents come from <see cref="ChestIndex"/>, which reads
    /// what the game already replicated to this client.
    ///
    /// The panel is built from plain Unity UI objects and borrows the background sprite and the
    /// font from the inventory, instead of cloning a game prefab whose inner structure would
    /// have to be guessed. The search box is the one exception: it is a copy of the field the
    /// game uses for signs, because a working input field is not worth rebuilding. Every lookup
    /// is optional; a missing piece costs the panel a feature, not an exception per frame.
    /// </summary>
    public sealed class ChestBrowser : MonoBehaviour
    {
        private const float Padding = 16f;

        private const float Gap = 8f;

        private const float TitleHeight = 28f;

        private const float SearchHeight = 34f;

        private const float RowHeight = 40f;

        private const float IconSize = 32f;

        private const float ScrollbarWidth = 14f;

        /// <summary>The clickable strip on the left of a row that pins the item.</summary>
        private const float MarkColumn = 26f;

        /// <summary>Side of the diamond drawn in that strip.</summary>
        private const float MarkSize = 14f;

        private const float HeaderHeight = 22f;

        /// <summary>Where the name starts, past the pin and the icon.</summary>
        private const float NameColumn = MarkColumn + IconSize + Gap;

        /// <summary>Right edge of the count column, measured from the right edge of a row.</summary>
        private const float AmountColumn = 250f;

        /// <summary>Right edge of the "chests: n" column, measured from the right edge of a row.</summary>
        private const float ChestsColumn = 160f;

        /// <summary>The pin of an item the player marked, and of one they did not.</summary>
        private static readonly Color MarkOn = new Color(1f, 0.82f, 0.35f, 1f);

        private static readonly Color MarkOff = new Color(1f, 1f, 1f, 0.2f);

        /// <summary>Rows one wheel step moves the list.</summary>
        private const float ScrollStepRows = 3f;

        /// <summary>Seconds between two re-reads of the chests while the panel is open.</summary>
        private const float RefreshInterval = 0.4f;

        private static ChestBrowser? _instance;

        private readonly List<ItemTotal> _totals = new List<ItemTotal>();

        private readonly List<ItemTotal> _visible = new List<ItemTotal>();

        private readonly List<Row> _rows = new List<Row>();

        private RectTransform? _panel;

        private RectTransform? _content;

        private ScrollRect? _scroll;

        private GuiInputField? _search;

        private TMP_Text? _title;

        private TMP_Text? _status;

        private TMP_Text? _sortByName;

        private TMP_Text? _sortByCount;

        /// <summary>The pinned items, as read from the config file.</summary>
        private FavoriteList _favorites = FavoriteList.Parse("");

        /// <summary>The config line <see cref="_favorites"/> was parsed from, to notice an edit by hand.</summary>
        private string _favoritesSource = "";

        private TMP_FontAsset? _font;

        private Material? _fontMaterial;

        private Color _fontColor = Color.white;

        private string _query = "";

        private float _nextRefresh;

        /// <summary>The wheel value the input patch took away from the rest of the game this frame.</summary>
        private float _wheel;

        /// <summary>
        /// The inventory reports itself visible for another frame or two after it is told to
        /// hide (<c>m_hiddenFrames</c>), so the panel must not read that as "something else
        /// took the screen" right after it opened.
        /// </summary>
        private float _acceptScreenLossAfter;

        private bool _failed;

        private bool _warnedAboutSearch;

        /// <summary>True while the panel is on screen; the input patches ask this.</summary>
        public static bool IsPanelOpen => _instance != null && _instance.IsOpen;

        /// <summary>True while the player is typing in the search box.</summary>
        public static bool IsSearchFocused => _instance != null && _instance._search != null && _instance._search.isFocused;

        /// <summary>
        /// Hands the panel the mouse wheel the input patch took away from the camera zoom.
        /// The largest value of the frame wins, because several systems ask for it per frame
        /// and each call reports the same movement.
        /// </summary>
        public static void FeedScrollWheel(float delta)
        {
            if (_instance != null && Mathf.Abs(delta) > Mathf.Abs(_instance._wheel))
            {
                _instance._wheel = delta;
            }
        }

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        /// <summary>Rows currently listed, for the console command.</summary>
        public int RowCount => _visible.Count;

        /// <summary>Re-reads the title and the search hint after a language change.</summary>
        public static void RefreshLabels()
        {
            if (_instance == null)
            {
                return;
            }

            if (_instance._title != null)
            {
                _instance._title.text = Translations.Get(Translations.BrowserTitle);
            }

            if (_instance._search != null && _instance._search.placeholder is TMP_Text placeholder)
            {
                placeholder.text = Translations.Get(Translations.BrowserSearch);
            }

            _instance.UpdateHeader();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            // The inventory is closed to make room; anything else that owns the screen wins.
            if (Menu.IsVisible() || Minimap.IsOpen() || global::Console.IsVisible() || TextInput.IsVisible())
            {
                return;
            }

            if (!EnsureUi() || _panel == null)
            {
                return;
            }

            InventoryGui gui = InventoryGui.instance;
            if (gui != null && InventoryGui.IsVisible())
            {
                gui.Hide();
            }

            _query = "";
            _panel.gameObject.SetActive(true);
            if (_search != null)
            {
                _search.text = "";
                _search.Select();
                _search.ActivateInputField();
            }

            _nextRefresh = Time.time + RefreshInterval;
            _acceptScreenLossAfter = Time.time + 0.3f;
            UpdateHeader();
            Refresh();
            ScrollToTop();
        }

        public void Close()
        {
            if (_panel == null || !_panel.gameObject.activeSelf)
            {
                return;
            }

            if (_search != null)
            {
                _search.DeactivateInputField();
            }

            _panel.gameObject.SetActive(false);
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            Close();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (!Plugin.Enabled || player == null || player.IsDead())
                {
                    Close();
                    return;
                }

                if (IsOpen)
                {
                    // Anything that takes over the screen wins; the panel is not a modal window.
                    if (Time.time >= _acceptScreenLossAfter
                        && (InventoryGui.IsVisible() || Menu.IsVisible() || Minimap.IsOpen() || global::Console.IsVisible()))
                    {
                        Close();
                        return;
                    }

                    KeepSearchFocused();
                    ConsumeScrollWheel();

                    if (Time.time >= _nextRefresh)
                    {
                        _nextRefresh = Time.time + RefreshInterval;
                        Refresh();
                    }
                }

                KeyboardShortcut key = Plugin.Settings.BrowserKey.Value;
                bool pressed = IsOpen ? Shortcut.IsPressedWhileSearching(key) : Shortcut.IsPressed(key);
                if (pressed)
                {
                    Toggle();
                }
            }
            catch (Exception exception)
            {
                // Closing stops the error from repeating every frame; the next key press retries.
                Close();
                Plugin.Log.LogError($"The chest list was closed after an error: {exception}");
            }
        }

        /// <summary>
        /// Puts the keyboard back into the search box whenever something took it away without
        /// closing the panel: switching the keyboard layout drops the focus, and so does
        /// dragging the scrollbar. Skipped while a mouse button is held, so a drag in progress
        /// is not interrupted.
        /// </summary>
        private void KeepSearchFocused()
        {
            if (_search == null || _search.isFocused || ZInput.GetMouseButton(0) || ZInput.GetMouseButton(1))
            {
                return;
            }

            _search.ActivateInputField();
            _search.caretPosition = _search.text.Length;
            _search.selectionAnchorPosition = _search.text.Length;
        }

        /// <summary>
        /// Moves the list by the wheel. Only the sign is used: the value the game reports per
        /// wheel step is not a fixed scale, which is why vanilla clamps and accumulates it.
        /// </summary>
        private void ConsumeScrollWheel()
        {
            float wheel = _wheel;
            _wheel = 0f;
            if (wheel == 0f || _scroll == null || _content == null || _scroll.viewport == null)
            {
                return;
            }

            float hidden = _content.rect.height - _scroll.viewport.rect.height;
            if (hidden <= 1f)
            {
                return;
            }

            float step = Mathf.Sign(wheel) * ScrollStepRows * RowHeight / hidden;
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + step);
        }

        /// <summary>Re-reads the chests in range and rebuilds the rows.</summary>
        private void Refresh()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            ChestIndex index = Plugin.Index;
            index.Refresh(player.transform.position, Plugin.Settings.ScanRadius.Value);

            SyncFavorites();
            _totals.Clear();
            _totals.AddRange(ItemSearch.Summarize(index.Chests));
            AddMissingFavorites(index);
            ApplyQuery();
        }

        /// <summary>Applies the search text and the chosen order to the rows already read.</summary>
        private void ApplyQuery()
        {
            _visible.Clear();
            _visible.AddRange(ItemSearch.Arrange(_totals, _query, Plugin.Settings.Sort.Value, _favorites));
            FillRows();
            UpdateStatus();
        }

        /// <summary>
        /// Re-reads the pinned items when the config line changed, which covers both a click on a
        /// pin and an edit of the file followed by <c>tidychests reload</c>.
        /// </summary>
        private void SyncFavorites()
        {
            string source = Plugin.Settings.Favorites.Value ?? "";
            if (source == _favoritesSource)
            {
                return;
            }

            _favoritesSource = source;
            _favorites = FavoriteList.Parse(source);
        }

        /// <summary>
        /// A row with a count of zero for every pinned item no container in range holds. That is
        /// the point of pinning something: the list keeps saying it exists after the last of it
        /// was used up, instead of quietly dropping it.
        /// </summary>
        private void AddMissingFavorites(ChestIndex index)
        {
            if (_favorites.Count == 0)
            {
                return;
            }

            foreach (string name in _favorites.Names)
            {
                if (HasRow(name))
                {
                    continue;
                }

                _totals.Add(new ItemTotal(name, index.DisplayNameOf(name), 0, 0, 0f));
            }
        }

        private bool HasRow(string name)
        {
            foreach (ItemTotal total in _totals)
            {
                if (string.Equals(total.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnQueryChanged(string query)
        {
            _query = query ?? "";
            ApplyQuery();
        }

        private void UpdateStatus()
        {
            if (_status == null)
            {
                return;
            }

            CultureInfo culture = CultureInfo.InvariantCulture;
            if (_totals.Count == 0)
            {
                _status.text = Translations.Get(Translations.BrowserEmpty, Plugin.Settings.ScanRadius.Value.ToString("0.#", culture));
            }
            else if (_visible.Count == 0)
            {
                _status.text = Translations.Get(Translations.BrowserNoMatch, _query.Trim());
            }
            else
            {
                _status.text = "";
            }

            _status.gameObject.SetActive(_status.text.Length > 0);
        }

        private void FillRows()
        {
            if (_content == null)
            {
                return;
            }

            while (_rows.Count < _visible.Count)
            {
                _rows.Add(CreateRow(_content));
            }

            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (i >= _visible.Count)
                {
                    row.Rect.gameObject.SetActive(false);
                    row.Total = null;
                    continue;
                }

                ItemTotal total = _visible[i];

                // A pinned item no chest holds any more: listed, but there is nothing to find.
                bool ranOut = total.Count <= 0;

                row.Total = total;
                row.Rect.gameObject.SetActive(true);
                row.Rect.anchoredPosition = new Vector2(0f, -i * RowHeight);
                row.Button.interactable = !ranOut;
                row.Name.text = total.DisplayName;
                row.Name.color = Fade(_fontColor, ranOut ? 0.45f : 1f);
                row.Amount.text = "×" + total.Count.ToString(culture);
                row.Amount.color = Fade(_fontColor, ranOut ? 0.45f : 1f);
                row.Chests.text = ranOut
                    ? Translations.Get(Translations.BrowserMissing)
                    : Translations.Get(
                        Translations.BrowserChests,
                        total.ChestCount.ToString(culture),
                        total.NearestDistance.ToString("0.#", culture));

                row.Mark.color = _favorites.Contains(total.Name) ? MarkOn : MarkOff;

                Sprite? icon = Plugin.Index.TryGetKnownItem(total.Name, out ItemDrop.ItemData item) ? IconOf(item) : null;
                row.Icon.sprite = icon;
                row.Icon.enabled = icon != null;
                row.Icon.color = Fade(Color.white, ranOut ? 0.45f : 1f);
            }

            _content.sizeDelta = new Vector2(_content.sizeDelta.x, _visible.Count * RowHeight);
        }

        private void OnRowClicked(Row row)
        {
            ItemTotal? total = row.Total;
            if (total == null || total.Count <= 0)
            {
                // A pinned row that ran out: nothing to highlight, so the panel stays open.
                return;
            }

            float radius = Plugin.Settings.ScanRadius.Value;
            Close();

            ChestFinder? finder = Plugin.Finder;
            Player player = Player.m_localPlayer;
            if (finder == null || player == null)
            {
                return;
            }

            int found = finder.Find(total.Name, total.DisplayName, radius);
            string message = found > 0
                ? Translations.Get(Translations.Found, total.DisplayName, found)
                : Translations.Get(Translations.NotFound, total.DisplayName, radius.ToString("0.#", CultureInfo.InvariantCulture));
            player.Message(MessageHud.MessageType.Center, message);
        }

        /// <summary>
        /// Pins the item of a row, or unpins it, and writes the list to the config file straight
        /// away: the point of a pin is that it is still there tomorrow.
        /// </summary>
        private void OnMarkClicked(Row row)
        {
            ItemTotal? total = row.Total;
            if (total == null || string.IsNullOrEmpty(total.Name))
            {
                return;
            }

            bool pinned = _favorites.Toggle(total.Name);
            _favoritesSource = _favorites.Format();
            Plugin.Settings.Favorites.Value = _favoritesSource;
            Plugin.Debug($"{total.DisplayName} ({total.Name}) {(pinned ? "pinned" : "unpinned")}");

            // An item that is pinned while nothing holds it has no row yet; a rebuild makes one.
            Refresh();
        }

        /// <summary>
        /// The header of a column was clicked: sort by it, or flip the direction when it already
        /// decides the order. The choice is saved, like the pins.
        /// </summary>
        private void OnSortClicked(bool byName)
        {
            BrowserSort current = Plugin.Settings.Sort.Value;
            BrowserSort next;
            if (byName)
            {
                next = current == BrowserSort.NameAscending ? BrowserSort.NameDescending : BrowserSort.NameAscending;
            }
            else
            {
                next = current == BrowserSort.CountDescending ? BrowserSort.CountAscending : BrowserSort.CountDescending;
            }

            Plugin.Settings.Sort.Value = next;
            UpdateHeader();
            ApplyQuery();
            ScrollToTop();
        }

        /// <summary>The column that decides the order is spelled out with its direction and lit; the other is dim.</summary>
        private void UpdateHeader()
        {
            if (_sortByName == null || _sortByCount == null)
            {
                return;
            }

            BrowserSort sort = Plugin.Settings.Sort.Value;
            bool byName = sort == BrowserSort.NameAscending || sort == BrowserSort.NameDescending;

            _sortByName.text = Translations.Get(
                sort == BrowserSort.NameAscending ? Translations.SortNameUp
                : sort == BrowserSort.NameDescending ? Translations.SortNameDown
                : Translations.SortName);
            _sortByCount.text = Translations.Get(
                sort == BrowserSort.CountAscending ? Translations.SortCountUp
                : sort == BrowserSort.CountDescending ? Translations.SortCountDown
                : Translations.SortCount);

            _sortByName.color = Fade(_fontColor, byName ? 1f : 0.5f);
            _sortByCount.color = Fade(_fontColor, byName ? 0.5f : 1f);
        }

        private void ScrollToTop()
        {
            if (_scroll != null)
            {
                _scroll.verticalNormalizedPosition = 1f;
            }
        }

        private static Color Fade(Color color, float factor)
        {
            return new Color(color.r, color.g, color.b, color.a * factor);
        }

        private static Sprite? IconOf(ItemDrop.ItemData item)
        {
            Sprite[] icons = item.m_shared.m_icons;
            if (icons == null || icons.Length == 0)
            {
                return null;
            }

            return icons[Mathf.Clamp(item.m_variant, 0, icons.Length - 1)];
        }

        /// <summary>Builds the panel the first time it is needed. False while the game UI is not up yet.</summary>
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

                Plugin.Log.LogError($"Could not build the chest list; it stays disabled until restart: {exception}");
                return false;
            }

            return _panel != null;
        }

        private void Build()
        {
            // A world change destroys the canvas and everything under it, so anything kept
            // from the previous build points at destroyed objects by now.
            _rows.Clear();
            _content = null;
            _scroll = null;
            _search = null;
            _title = null;
            _status = null;
            _sortByName = null;
            _sortByCount = null;

            InventoryGui gui = InventoryGui.instance;
            Transform? canvas = FindCanvas(gui);
            if (gui == null || canvas == null)
            {
                // The game UI is not up yet; try again the next time the key is pressed.
                Plugin.Debug("The chest list waits for the game UI");
                return;
            }

            ReadStyle(gui);

            Vector2 size = Plugin.Settings.BrowserSize.Value;
            var panel = new GameObject("TidyChests Browser", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            _panel = (RectTransform)panel.transform;
            _panel.SetParent(canvas, false);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(Mathf.Max(320f, size.x), Mathf.Max(240f, size.y));

            // Above the HUD, and with its own raycaster so the rows can be clicked.
            var ownCanvas = panel.GetComponent<Canvas>();
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 100;

            StyleBackground(panel.GetComponent<Image>(), gui);

            _title = CreateText("Title", _panel, TitleHeight, 20f, TextAlignmentOptions.Center, 0f);
            _search = CreateSearch(_panel);
            CreateHeader(_panel);
            CreateList(_panel, gui);

            RefreshLabels();
            panel.SetActive(false);
            Plugin.Debug($"Chest list built ({_panel.sizeDelta.x}x{_panel.sizeDelta.y})");
        }

        /// <summary>The font and the colour the inventory uses, so the panel does not look pasted on.</summary>
        private void ReadStyle(InventoryGui gui)
        {
            TMP_Text? sample = gui.m_takeAllButton != null
                ? gui.m_takeAllButton.GetComponentInChildren<TMP_Text>(true)
                : null;

            if (sample == null)
            {
                return;
            }

            _font = sample.font;
            _fontMaterial = sample.fontSharedMaterial;
            _fontColor = sample.color;
        }

        private void StyleBackground(Image image, InventoryGui gui)
        {
            Image? source = gui.m_player != null ? gui.m_player.GetComponent<Image>() : null;
            if (source != null && source.sprite != null)
            {
                image.sprite = source.sprite;
                image.type = source.type;
                image.color = source.color;
                return;
            }

            // No panel sprite to borrow: a plain dark box still reads as a window.
            image.color = new Color(0.08f, 0.07f, 0.06f, 0.94f);
        }

        private static Transform? FindCanvas(InventoryGui? gui)
        {
            global::Hud hud = global::Hud.instance;
            if (hud != null)
            {
                Canvas canvas = hud.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    return canvas.transform;
                }
            }

            if (gui != null)
            {
                Canvas canvas = gui.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    return canvas.transform;
                }
            }

            return null;
        }

        /// <summary>A copy of the game's own text field, so typing, the caret and the gamepad keyboard work.</summary>
        private GuiInputField? CreateSearch(RectTransform panel)
        {
            TextInput input = TextInput.instance;
            GuiInputField? template = input != null ? input.m_inputField : null;
            if (template == null)
            {
                if (!_warnedAboutSearch)
                {
                    _warnedAboutSearch = true;
                    Plugin.Log.LogWarning("No text field to copy for the chest list; the list is shown without a search box.");
                }

                return null;
            }

            GuiInputField field = Instantiate(template, panel);
            field.name = "Search";
            field.gameObject.SetActive(true);
            field.onValueChanged.RemoveAllListeners();
            field.characterLimit = 0;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = TMP_InputField.ContentType.Standard;

            // The focus is given back whenever something takes it (see KeepSearchFocused), so
            // selecting everything on focus would let the next keystroke wipe what was typed.
            field.onFocusSelectAll = false;
            field.resetOnDeActivation = false;
            field.text = "";
            field.onValueChanged.AddListener(OnQueryChanged);

            var rect = (RectTransform)field.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-2f * Padding, SearchHeight);
            rect.anchoredPosition = new Vector2(0f, -(Padding + TitleHeight + Gap));
            return field;
        }

        /// <summary>
        /// The column headers that decide the order. They sit in the same horizontal band as a
        /// row, scrollbar included, so each label stands over the column it sorts.
        /// </summary>
        private void CreateHeader(RectTransform panel)
        {
            // The list is inset by the scrollbar on the right, so the header is too; otherwise
            // its labels would sit a few pixels to the right of the columns they sort.
            float left = Padding;
            float right = Padding + ScrollbarWidth + Gap * 0.5f;

            var headerGo = new GameObject("Header", typeof(RectTransform));
            var header = (RectTransform)headerGo.transform;
            header.SetParent(panel, false);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(-(left + right), HeaderHeight);
            header.anchoredPosition = new Vector2((left - right) * 0.5f, -(Padding + TitleHeight + Gap + SearchHeight + Gap));

            RectTransform nameHeader = CreateHeaderButton(header, "SortByName", TextAlignmentOptions.Left, out _sortByName, () => OnSortClicked(true));
            StretchHorizontally(nameHeader, NameColumn, AmountColumn);

            RectTransform countHeader = CreateHeaderButton(header, "SortByCount", TextAlignmentOptions.Right, out _sortByCount, () => OnSortClicked(false));
            PinRight(countHeader, AmountColumn, ChestsColumn);
        }

        /// <summary>
        /// A clickable header label. The click target is a transparent background, as in a row, so
        /// the label keeps the colour that says whether it is the active column.
        /// </summary>
        private RectTransform CreateHeaderButton(RectTransform parent, string name, TextAlignmentOptions alignment, out TMP_Text label, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            Image background = go.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.24f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            label = CreateText("Label", rect, 0f, 15f, alignment, 0f);
            return rect;
        }

        /// <summary>The scrolling area with the rows, plus the message shown when there is nothing to list.</summary>
        private void CreateList(RectTransform panel, InventoryGui gui)
        {
            float top = Padding + TitleHeight + Gap + SearchHeight + Gap + HeaderHeight + Gap * 0.5f;

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(panel, false);
            Stretch(scrollRect, Padding, Padding, top, Padding);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(scrollRect, false);
            Stretch(viewport, 0f, ScrollbarWidth + Gap * 0.5f, 0f, 0f);

            // Fully transparent, but still a raycast target so a click anywhere in the list works.
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            _content = (RectTransform)contentGo.transform;
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 0f);

            _scroll = scrollGo.GetComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.inertia = false;

            // The wheel is handled by ConsumeScrollWheel, which knows the row height; letting
            // the scroll rect add its own step on top would double the movement.
            _scroll.scrollSensitivity = 0f;

            Scrollbar? bar = CreateScrollbar(scrollRect, gui);
            if (bar != null)
            {
                _scroll.verticalScrollbar = bar;
                _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }

            _status = CreateText("Status", scrollRect, 0f, 17f, TextAlignmentOptions.Top, 0f);
            Stretch((RectTransform)_status.transform, 0f, 0f, Gap, 0f);
            _status.gameObject.SetActive(false);
        }

        /// <summary>A copy of the crafting panel's scrollbar, pinned to the right of the list.</summary>
        private Scrollbar? CreateScrollbar(RectTransform scrollRect, InventoryGui gui)
        {
            Scrollbar template = gui.m_recipeListScroll;
            if (template == null)
            {
                Plugin.Log.LogWarning("No scrollbar to copy for the chest list; use the mouse wheel to scroll it.");
                return null;
            }

            Scrollbar bar = Instantiate(template, scrollRect);
            bar.name = "Scrollbar";
            bar.gameObject.SetActive(true);
            bar.onValueChanged.RemoveAllListeners();

            // A listener wired in the prefab would still point at the crafting panel's list.
            for (int i = 0; i < bar.onValueChanged.GetPersistentEventCount(); i++)
            {
                bar.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
            }

            bar.direction = Scrollbar.Direction.BottomToTop;

            var rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-ScrollbarWidth, 0f);
            rect.offsetMax = Vector2.zero;
            return bar;
        }

        private Row CreateRow(RectTransform content)
        {
            var rowGo = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)rowGo.transform;
            rect.SetParent(content, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, RowHeight);

            Image background = rowGo.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);

            Button button = rowGo.GetComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.24f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;

            Image mark = CreateMark(rect, out Button markButton);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(MarkColumn, 0f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            Image icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            TMP_Text name = CreateText("Name", rect, 0f, 18f, TextAlignmentOptions.Left, 0f);
            StretchHorizontally((RectTransform)name.transform, NameColumn, AmountColumn);

            TMP_Text amount = CreateText("Amount", rect, 0f, 18f, TextAlignmentOptions.Right, 0f);
            PinRight((RectTransform)amount.transform, AmountColumn, ChestsColumn);

            TMP_Text chests = CreateText("Chests", rect, 0f, 15f, TextAlignmentOptions.Right, 0.65f);
            PinRight((RectTransform)chests.transform, ChestsColumn, Gap);

            var row = new Row(rect, button, mark, icon, name, amount, chests);
            button.onClick.AddListener(() => OnRowClicked(row));
            markButton.onClick.AddListener(() => OnMarkClicked(row));
            return row;
        }

        /// <summary>
        /// The pin on the left of a row: a diamond the player clicks to keep the item at the top
        /// of the list. Drawn rather than borrowed, because the game has no star sprite to copy
        /// and its font has no star glyph to print. The click target is a transparent square
        /// around it, so the diamond keeps the colour that says whether the item is pinned.
        /// </summary>
        private static Image CreateMark(RectTransform row, out Button button)
        {
            var holderGo = new GameObject("Mark", typeof(RectTransform), typeof(Image), typeof(Button));
            var holder = (RectTransform)holderGo.transform;
            holder.SetParent(row, false);
            holder.anchorMin = new Vector2(0f, 0f);
            holder.anchorMax = new Vector2(0f, 1f);
            holder.pivot = new Vector2(0f, 0.5f);
            holder.offsetMin = Vector2.zero;
            holder.offsetMax = new Vector2(MarkColumn, 0f);

            Image background = holderGo.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);

            button = holderGo.GetComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.32f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;

            var diamondGo = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
            var diamond = (RectTransform)diamondGo.transform;
            diamond.SetParent(holder, false);
            diamond.anchorMin = new Vector2(0.5f, 0.5f);
            diamond.anchorMax = new Vector2(0.5f, 0.5f);
            diamond.pivot = new Vector2(0.5f, 0.5f);
            diamond.anchoredPosition = Vector2.zero;
            diamond.sizeDelta = new Vector2(MarkSize, MarkSize);
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // No sprite: an Image without one draws a filled rectangle, which the turn makes a diamond.
            Image image = diamondGo.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = MarkOff;
            return image;
        }

        /// <summary>
        /// A label in the inventory's font. <paramref name="height"/> above zero pins it under
        /// the top edge of its parent; <paramref name="dim"/> above zero fades it.
        /// </summary>
        private TMP_Text CreateText(string name, RectTransform parent, float height, float fontSize, TextAlignmentOptions alignment, float dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                text.font = _font;
                if (_fontMaterial != null)
                {
                    text.fontSharedMaterial = _fontMaterial;
                }
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.color = dim > 0f ? new Color(_fontColor.r, _fontColor.g, _fontColor.b, _fontColor.a * (1f - dim)) : _fontColor;

            if (height > 0f)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-2f * Padding, height);
                rect.anchoredPosition = new Vector2(0f, -Padding);
            }
            else
            {
                Stretch(rect, 0f, 0f, 0f, 0f);
            }

            return text;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void StretchHorizontally(RectTransform rect, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
        }

        /// <summary>Pins a label to the right edge, between <paramref name="from"/> and <paramref name="to"/> pixels from it.</summary>
        private static void PinRight(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-from, 0f);
            rect.offsetMax = new Vector2(-to, 0f);
        }

        /// <summary>One listed item kind and the objects that show it.</summary>
        private sealed class Row
        {
            public Row(RectTransform rect, Button button, Image mark, Image icon, TMP_Text name, TMP_Text amount, TMP_Text chests)
            {
                Rect = rect;
                Button = button;
                Mark = mark;
                Icon = icon;
                Name = name;
                Amount = amount;
                Chests = chests;
            }

            public RectTransform Rect { get; }

            /// <summary>The whole row: clicking it highlights the chests holding the item.</summary>
            public Button Button { get; }

            /// <summary>The diamond that says whether the item is pinned.</summary>
            public Image Mark { get; }

            public Image Icon { get; }

            public TMP_Text Name { get; }

            public TMP_Text Amount { get; }

            public TMP_Text Chests { get; }

            public ItemTotal? Total { get; set; }
        }
    }
}
