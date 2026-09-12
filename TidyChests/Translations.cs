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
        }
    }
}
