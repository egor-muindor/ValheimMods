using System;
using System.Globalization;
using CombatStats.Model;
using CombatStats.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CombatStats.Ui
{
    /// <summary>
    /// One combatant's line: the name on the left, the numbers on the right and, under them, a
    /// bar as long as their share of the leader's damage, split into the kinds it is made of.
    ///
    /// The objects are built once and reused; a refresh only writes text and resizes rectangles.
    /// </summary>
    internal sealed class MeterRow
    {
        public const float LineHeight = 17f;

        public const float BarHeight = 6f;

        private const float BarGap = 2f;

        private const float RowGap = 4f;

        private static readonly NumberFormatInfo Numbers = BuildNumberFormat();

        private readonly TMP_Text _name;

        private readonly TMP_Text _value;

        private readonly TMP_Text? _share;

        private readonly TMP_Text? _hits;

        private readonly TMP_Text? _average;

        private readonly RectTransform _bar;

        private readonly Image[] _segments = new Image[DamageKinds.Count];

        private readonly bool _detailed;

        private MeterRow(RectTransform root, bool detailed, TMP_Text name, TMP_Text value, TMP_Text? share, TMP_Text? hits, TMP_Text? average, RectTransform bar)
        {
            Root = root;
            _detailed = detailed;
            _name = name;
            _value = value;
            _share = share;
            _hits = hits;
            _average = average;
            _bar = bar;
        }

        /// <summary>The rectangle the row lives in; the owner places it.</summary>
        public RectTransform Root { get; }

        /// <summary>The combatant currently shown, so a click knows whose row it hit.</summary>
        public long Id { get; private set; }

        /// <summary>Height of one row, bar included.</summary>
        public static float Height(bool bars)
        {
            return bars ? LineHeight + BarGap + BarHeight + RowGap : LineHeight + RowGap;
        }

        /// <summary>
        /// Builds a row. A detailed row carries the share, the hits and the average in their own
        /// columns; a compact one puts the share next to the damage and leaves the rest out.
        /// </summary>
        public static MeterRow Create(RectTransform parent, bool detailed, Action<long>? onClick)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            var root = (RectTransform)go.transform;
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0f, 1f);

            if (onClick != null)
            {
                var background = go.AddComponent<Image>();
                background.color = new Color(1f, 1f, 1f, 0f);

                var button = go.AddComponent<Button>();
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
                colors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
                colors.selectedColor = new Color(1f, 1f, 1f, 0f);
                colors.disabledColor = new Color(1f, 1f, 1f, 0f);
                colors.fadeDuration = 0.05f;
                button.colors = colors;
                button.targetGraphic = background;

                MeterRow? created = null;
                button.onClick.AddListener(() =>
                {
                    if (created != null)
                    {
                        onClick(created.Id);
                    }
                });

                MeterRow row = Build(root, detailed);
                created = row;
                return row;
            }

            return Build(root, detailed);
        }

        /// <summary>Fills the row in and sizes its bar. <paramref name="width"/> is the row's own width.</summary>
        public void Apply(CombatantRow row, float leader, float width, bool bars, bool showShare, bool local, string prefix)
        {
            Id = row.Id;

            string name = string.IsNullOrEmpty(row.Name) ? "..." : row.Name;
            _name.text = prefix + (row.Estimated ? "~" + name : name);
            _name.color = local ? UiStyle.Mine : UiStyle.FontColor;

            string total = Format(row.Total);
            if (_detailed)
            {
                _value.text = total;
                if (_share != null)
                {
                    _share.text = Percent(row.Share);
                }

                if (_hits != null)
                {
                    _hits.text = row.Hits.ToString(CultureInfo.InvariantCulture);
                }

                if (_average != null)
                {
                    _average.text = Format(row.Average);
                }
            }
            else
            {
                _value.text = showShare
                    ? total + "  <color=#" + ColorUtility.ToHtmlStringRGB(UiStyle.Dim) + ">" + Percent(row.Share) + "</color>"
                    : total;
            }

            _bar.gameObject.SetActive(bars);
            if (bars)
            {
                Layout(row, leader, width);
            }

            Root.sizeDelta = new Vector2(0f, Height(bars));
        }

        /// <summary>Hides the row without destroying it.</summary>
        public void SetActive(bool active)
        {
            if (Root.gameObject.activeSelf != active)
            {
                Root.gameObject.SetActive(active);
            }
        }

        /// <summary>Puts the row <paramref name="y"/> pixels below the top of its parent.</summary>
        public void Place(float y)
        {
            Root.anchoredPosition = new Vector2(0f, -y);
        }

        private static MeterRow Build(RectTransform root, bool detailed)
        {
            TMP_Text name = UiStyle.Text("Name", root, 15f, TextAlignmentOptions.MidlineLeft, UiStyle.FontColor);
            Line(name.rectTransform);
            name.rectTransform.offsetMax = new Vector2(detailed ? -250f : -120f, name.rectTransform.offsetMax.y);

            TMP_Text value = UiStyle.Text("Value", root, 15f, TextAlignmentOptions.MidlineRight, UiStyle.FontColor);
            Line(value.rectTransform);

            TMP_Text? share = null;
            TMP_Text? hits = null;
            TMP_Text? average = null;

            if (detailed)
            {
                PinRight(value.rectTransform, 250f, 152f);

                share = UiStyle.Text("Share", root, 13f, TextAlignmentOptions.MidlineRight, UiStyle.Dim);
                Line(share.rectTransform);
                PinRight(share.rectTransform, 148f, 104f);

                hits = UiStyle.Text("Hits", root, 13f, TextAlignmentOptions.MidlineRight, UiStyle.Dim);
                Line(hits.rectTransform);
                PinRight(hits.rectTransform, 100f, 52f);

                average = UiStyle.Text("Average", root, 13f, TextAlignmentOptions.MidlineRight, UiStyle.Dim);
                Line(average.rectTransform);
                PinRight(average.rectTransform, 48f, 0f);
            }
            else
            {
                PinRight(value.rectTransform, 120f, 0f);
            }

            var barGo = new GameObject("Bar", typeof(RectTransform));
            var bar = (RectTransform)barGo.transform;
            bar.SetParent(root, false);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = new Vector2(0f, -(LineHeight + BarGap));
            bar.sizeDelta = new Vector2(0f, BarHeight);

            Image back = UiStyle.Box("Back", bar, new Color(0f, 0f, 0f, 0.45f));
            back.sprite = UiStyle.Flat();
            UiStyle.Stretch(back.rectTransform, 0f, 0f, 0f, 0f);

            var built = new MeterRow(root, detailed, name, value, share, hits, average, bar);
            for (int kind = 0; kind < DamageKinds.Count; kind++)
            {
                Image segment = UiStyle.Box("Kind" + kind, bar, Color.white);
                segment.sprite = UiStyle.Flat();
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(rect.offsetMin.x, 0f);
                rect.offsetMax = new Vector2(rect.offsetMax.x, 0f);
                segment.gameObject.SetActive(false);
                built._segments[kind] = segment;
            }

            return built;
        }

        private static void Line(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -LineHeight);
            rect.offsetMax = new Vector2(0f, 0f);
        }

        private static void PinRight(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(-from, -LineHeight);
            rect.offsetMax = new Vector2(-to, 0f);
        }

        private static NumberFormatInfo BuildNumberFormat()
        {
            var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            format.NumberGroupSeparator = " ";
            return format;
        }

        /// <summary>Damage as the meter writes it: no decimals above ten, one below.</summary>
        public static string Format(float value)
        {
            return value >= 10f
                ? value.ToString("#,0", Numbers)
                : value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string Percent(float share)
        {
            return (share * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        private void Layout(CombatantRow row, float leader, float width)
        {
            float full = leader > 0f ? Mathf.Clamp01(row.Total / leader) * width : 0f;
            Color[] colors = UiStyle.Colors;
            float offset = 0f;

            for (int kind = 0; kind < DamageKinds.Count; kind++)
            {
                Image segment = _segments[kind];
                float amount = row.ByKind[kind];
                if (amount <= 0f || row.Total <= 0f || full <= 0f)
                {
                    if (segment.gameObject.activeSelf)
                    {
                        segment.gameObject.SetActive(false);
                    }

                    continue;
                }

                float part = full * (amount / row.Total);
                RectTransform rect = segment.rectTransform;
                rect.anchoredPosition = new Vector2(offset, 0f);
                rect.sizeDelta = new Vector2(part, 0f);
                segment.color = colors[kind];
                if (!segment.gameObject.activeSelf)
                {
                    segment.gameObject.SetActive(true);
                }

                offset += part;
            }
        }
    }
}
