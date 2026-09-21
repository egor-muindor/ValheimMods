using System.Collections.Generic;
using HarmonyLib;
using TidyChests.Ui;

namespace TidyChests
{
    /// <summary>
    /// The mod's own words in the game's localization table, so the button label and the
    /// messages follow the language selected in the game. English is the fallback for every
    /// language without its own entry. Words are (re)added whenever the game loads a
    /// language, which also covers a language change in the settings menu.
    /// </summary>
    internal static class Translations
    {
        public const string Button = "tidychests_button";

        public const string Stashed = "tidychests_stashed";

        public const string Nothing = "tidychests_nothing";

        public const string NoChests = "tidychests_no_chests";

        public const string Found = "tidychests_found";

        public const string NotFound = "tidychests_not_found";

        public const string Hover = "tidychests_hover";

        public const string LockTopic = "tidychests_lock_topic";

        public const string LockHint = "tidychests_lock_hint";

        public const string LockStateOn = "tidychests_lock_on";

        public const string LockStateOff = "tidychests_lock_off";

        public const string ItemLocked = "tidychests_item_locked";

        public const string ItemUnlocked = "tidychests_item_unlocked";

        public const string SlotLocked = "tidychests_slot_locked";

        public const string SlotUnlocked = "tidychests_slot_unlocked";

        public const string BrowserTitle = "tidychests_browser_title";

        public const string BrowserSearch = "tidychests_browser_search";

        public const string BrowserChests = "tidychests_browser_chests";

        public const string BrowserEmpty = "tidychests_browser_empty";

        public const string BrowserNoMatch = "tidychests_browser_no_match";

        public const string BrowserMissing = "tidychests_browser_missing";

        public const string SortName = "tidychests_sort_name";

        public const string SortNameUp = "tidychests_sort_name_up";

        public const string SortNameDown = "tidychests_sort_name_down";

        public const string SortCount = "tidychests_sort_count";

        public const string SortCountUp = "tidychests_sort_count_up";

        public const string SortCountDown = "tidychests_sort_count_down";

        private const string DefaultLanguage = "English";

        private static readonly Dictionary<string, Dictionary<string, string>> Words = new Dictionary<string, Dictionary<string, string>>
        {
            ["English"] = new Dictionary<string, string>
            {
                [Button] = "Stash",
                [Stashed] = "Stashed {0} items into {1} chests",
                [Nothing] = "Nothing to stash: no chest nearby holds these items",
                [NoChests] = "No usable chest within {0} m",
                [Found] = "{0}: found in {1} chests",
                [NotFound] = "{0}: not in any chest within {1} m",
                [Hover] = "Point at an item in the inventory or the crafting panel and press {0} to find it in nearby chests",
                [LockTopic] = "Stash locks",
                [LockHint] = "Item: {0} [{1}]\nSlot: {2} [{3}]",
                [LockStateOn] = "locked",
                [LockStateOff] = "not locked",
                [ItemLocked] = "{0}: locked, the Stash button leaves it",
                [ItemUnlocked] = "{0}: unlocked",
                [SlotLocked] = "Slot locked: whatever lies here stays",
                [SlotUnlocked] = "Slot unlocked",
                [BrowserTitle] = "Chests nearby",
                [BrowserSearch] = "Search by name",
                [BrowserChests] = "chests: {0} · {1} m",
                [BrowserEmpty] = "No chest within {0} m holds anything",
                [BrowserNoMatch] = "Nothing matches \"{0}\"",
                [BrowserMissing] = "none nearby",
                // The direction is spelled out rather than drawn with an arrow glyph: the game's
                // font has no guaranteed arrow, and "9-1" says which end is on top without one.
                [SortName] = "Name",
                [SortNameUp] = "Name A-Z",
                [SortNameDown] = "Name Z-A",
                [SortCount] = "Count",
                [SortCountUp] = "Count 1-9",
                [SortCountDown] = "Count 9-1",
            },
            ["Russian"] = new Dictionary<string, string>
            {
                [Button] = "Убрать",
                [Stashed] = "Убрано предметов: {0}, в сундуков: {1}",
                [Nothing] = "Нечего убирать: поблизости нет сундуков с такими предметами",
                [NoChests] = "В радиусе {0} м нет подходящих сундуков",
                [Found] = "{0}: есть в сундуках ({1})",
                [NotFound] = "{0}: нет ни в одном сундуке в радиусе {1} м",
                [Hover] = "Наведите курсор на предмет в инвентаре или в крафте и нажмите {0}, чтобы найти его в сундуках",
                [LockTopic] = "Замки",
                [LockHint] = "Предмет: {0} [{1}]\nЯчейка: {2} [{3}]",
                [LockStateOn] = "под замком",
                [LockStateOff] = "без замка",
                [ItemLocked] = "{0}: заперт, кнопка «Убрать» его не тронет",
                [ItemUnlocked] = "{0}: отперт",
                [SlotLocked] = "Ячейка заперта: всё, что в ней лежит, остаётся",
                [SlotUnlocked] = "Ячейка отперта",
                [BrowserTitle] = "Сундуки рядом",
                [BrowserSearch] = "Поиск по названию",
                [BrowserChests] = "сундуков: {0} · {1} м",
                [BrowserEmpty] = "В радиусе {0} м нет сундуков с предметами",
                [BrowserNoMatch] = "Ничего не найдено по запросу «{0}»",
                [BrowserMissing] = "рядом нет",
                [SortName] = "Название",
                [SortNameUp] = "Название А-Я",
                [SortNameDown] = "Название Я-А",
                [SortCount] = "Количество",
                [SortCountUp] = "Количество 1-9",
                [SortCountDown] = "Количество 9-1",
            },
        };

        /// <summary>Localized text of a key, formatted with <paramref name="args"/>.</summary>
        public static string Get(string key, params object[] args)
        {
            string text = Localization.instance.Localize("$" + key);
            return args.Length == 0 ? text : string.Format(text, args);
        }

        /// <summary>Adds the words when the game's localization is already up (plugins can load after it).</summary>
        public static void ApplyIfLoaded()
        {
            Localization? localization = Localization.m_instance;
            if (localization != null)
            {
                Apply(localization, localization.GetSelectedLanguage());
            }
        }

        public static void Apply(Localization localization, string language)
        {
            Dictionary<string, string> english = Words[DefaultLanguage];
            Words.TryGetValue(language ?? "", out Dictionary<string, string>? selected);
            foreach (KeyValuePair<string, string> word in english)
            {
                string text = selected != null && selected.TryGetValue(word.Key, out string? translated) ? translated : word.Value;
                localization.AddWord(word.Key, text);
            }
        }
    }

    /// <summary>Adds the mod's words after every language load, then refreshes the button label.</summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class Localization_SetupLanguage_Patch
    {
        private static void Postfix(Localization __instance, string language)
        {
            Translations.Apply(__instance, language);
            StashButton.RefreshLabel();
            ChestBrowser.RefreshLabels();

            // The index keeps the localized item names; they belong to the old language now.
            Plugin.Index.Clear();
        }
    }
}
