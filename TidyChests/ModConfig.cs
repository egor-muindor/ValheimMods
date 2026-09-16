using System;
using System.Globalization;
using BepInEx.Configuration;
using Muindor.ServerConfig;
using TidyChests.Find;
using TidyChests.Index;
using TidyChests.Stash;
using UnityEngine;

namespace TidyChests
{
    /// <summary>
    /// Typed access to <c>muindor.TidyChests.cfg</c>.
    ///
    /// What the mod may do with the chests - how far it reaches, which items it moves, whether it
    /// unlocks recipes from what the chests hold - is bound through <see cref="Sync"/>, so a server
    /// that also runs TidyChests with <c>ConfigPriority</c> on decides it for everyone. Keys, the
    /// Stash button, the panels and the highlights stay each player's own.
    /// </summary>
    public sealed class ModConfig
    {
        private static readonly string[] ItemTypeNames = Enum.GetNames(typeof(ItemDrop.ItemData.ItemType));

        /// <summary>The item kinds that count as "regular items": everything that is neither equipment nor a tool or weapon.</summary>
        public const string DefaultItemTypes = "Material, Consumable, Ammo, AmmoNonEquipable, Trophy, Misc, Fish";

        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;
            string typeList = string.Join(", ", ItemTypeNames);

            Sync = new ConfigSync(config, MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, Plugin.Log);

            Enabled = Sync.Bind("General", "Enabled", true, "Enable this mod: the Stash button and the find key.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log every item decision and every chest that is considered when stashing.");

            Radius = Sync.Bind("Stash", "Radius", 10f,
                new ConfigDescription("Chests within this many metres of the player are used, both by the Stash button and by the find key.",
                    new AcceptableValueRange<float>(1f, 20f)));
            IncludeHotbar = Sync.Bind("Stash", "IncludeHotbar", false,
                "Also stash items from the first inventory row (the hotbar).");
            ItemTypes = Sync.Bind("Stash", "ItemTypes", DefaultItemTypes,
                $"Item types that may be stashed (comma-separated). Equipment, tools and weapons are left out by default. Valid types: {typeList}");
            Blacklist = Sync.Bind("Stash", "Blacklist", "",
                "Items that are never stashed (comma-separated). Use prefab names or item names, for example: Wood, $item_coal, Resin");
            ShowMessage = config.Bind("Stash", "ShowMessage", true,
                "Show a message with the result after stashing.");

            FindKey = config.Bind("Find", "FindKey", new KeyboardShortcut(KeyCode.T),
                "With the inventory open, point at an item (in the inventory, or an ingredient or recipe in the crafting panel) and press this key: the inventory closes and every chest in range that holds the item is highlighted. Modifiers are allowed, e.g. \"T + LeftControl\".");
            HighlightDuration = config.Bind("Find", "HighlightDuration", 8f,
                new ConfigDescription("Seconds the found chests stay highlighted.",
                    new AcceptableValueRange<float>(1f, 60f)));
            ScreenMarker = config.Bind("Find", "ScreenMarker", true,
                "Draw an arrow with the item count and the distance over each found chest (at the screen edge when the chest is out of view).");
            Light = config.Bind("Find", "Light", true,
                "Light up each found chest with a pulsing light.");
            Glow = config.Bind("Find", "Glow", true,
                "Tint each found chest with an emissive glow. Only works for materials whose shader has an emission colour.");
            CloseInventory = config.Bind("Find", "CloseInventory", true,
                "Close the inventory when chests are found, so the highlights are visible right away. When nothing is found the inventory stays open.");

            ShowButton = config.Bind("Button", "ShowButton", true,
                "Show the Stash button in the inventory. The console command 'tidychests stash' works without it.");
            ButtonOffset = config.Bind("Button", "ButtonOffset", new Vector2(41f, -56f),
                "Position of the Stash button relative to the weight display of the inventory panel, in UI pixels (x right, y up).");
            ButtonSize = config.Bind("Button", "ButtonSize", new Vector2(120f, 38f),
                "Width and height of the Stash button in UI pixels.");

            ScanRadius = Sync.Bind("Scan", "ScanRadius", 50f,
                new ConfigDescription("Chests within this many metres are read by the chest list and by the knowledge scan. Reading costs nothing on the network: the game already keeps the contents of every loaded chest on your client. Above roughly 90 m the chests are no longer loaded, so nothing more is found.",
                    new AcceptableValueRange<float>(5f, 90f)));
            ScanInterval = Sync.Bind("Scan", "ScanInterval", 5f,
                new ConfigDescription("Seconds between two scans of the chests in range. Chests whose contents did not change are skipped, so a short interval is cheap.",
                    new AcceptableValueRange<float>(1f, 60f)));

            LearnFromChests = Sync.Bind("Knowledge", "LearnFromChests", true,
                "Count the items lying in the chests in range as found, so their recipes unlock without carrying every stack yourself. Meant for co-op, where a team mate gathers a material you have never held. Trophies count too. This cannot be undone: turning the option off later does not lock a recipe again.");

            BrowserKey = config.Bind("Browser", "BrowserKey", new KeyboardShortcut(KeyCode.O, KeyCode.LeftControl),
                "Opens and closes the list of everything in the chests in range, with a search box. Modifiers are allowed; set it to \"None\" to disable the panel.");
            BrowserSize = config.Bind("Browser", "BrowserSize", new Vector2(520f, 560f),
                "Width and height of the chest list panel in UI pixels.");
            Sort = config.Bind("Browser", "Sort", BrowserSort.CountDescending,
                "Order of the chest list while its search box is empty. Click the Name or Count header in the list to change it, and click the active one again to flip it; the choice is saved here. " +
                "While something is typed in the search box the best matches come first and this decides the order among equally good ones.");
            Favorites = config.Bind("Browser", "Favorites", "",
                "Items pinned to the top of the chest list, comma-separated. Click the diamond on the left of a row to pin or unpin it; the list is saved here as you click. " +
                "A pinned item stays in the list even when no chest in range holds it any more, shown with a count of 0, so you can see that it ran out. Pinning is ignored while you are searching. " +
                "The names are the game's own item names: $item_wood, $item_coal, $item_ironscrap");
        }

        /// <summary>The settings a server may decide, and where the current ones come from.</summary>
        public ConfigSync Sync { get; }

        public SyncedEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public SyncedEntry<float> Radius { get; }

        public SyncedEntry<bool> IncludeHotbar { get; }

        public SyncedEntry<string> ItemTypes { get; }

        public SyncedEntry<string> Blacklist { get; }

        public ConfigEntry<bool> ShowMessage { get; }

        public ConfigEntry<KeyboardShortcut> FindKey { get; }

        public ConfigEntry<float> HighlightDuration { get; }

        public ConfigEntry<bool> ScreenMarker { get; }

        public ConfigEntry<bool> Light { get; }

        public ConfigEntry<bool> Glow { get; }

        public ConfigEntry<bool> CloseInventory { get; }

        public ConfigEntry<bool> ShowButton { get; }

        public ConfigEntry<Vector2> ButtonOffset { get; }

        public ConfigEntry<Vector2> ButtonSize { get; }

        public SyncedEntry<float> ScanRadius { get; }

        public SyncedEntry<float> ScanInterval { get; }

        public SyncedEntry<bool> LearnFromChests { get; }

        public ConfigEntry<KeyboardShortcut> BrowserKey { get; }

        public ConfigEntry<Vector2> BrowserSize { get; }

        public ConfigEntry<BrowserSort> Sort { get; }

        public ConfigEntry<string> Favorites { get; }

        /// <summary>Re-reads the config file from disk.</summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>Builds a fresh, validated rule set from the current config values.</summary>
        public StashRules BuildRules()
        {
            var settings = new StashRuleSettings
            {
                IncludeHotbar = IncludeHotbar.Value,
                ItemTypes = ItemTypes.Value,
                Blacklist = Blacklist.Value,
            };

            return StashRules.Parse(settings, ItemTypeNames, warning => Plugin.Log.LogWarning(warning));
        }

        /// <summary>Snapshot of the highlight options for one search.</summary>
        public HighlightOptions ToHighlightOptions()
        {
            return new HighlightOptions(HighlightDuration.Value, Light.Value, Glow.Value);
        }

        /// <summary>One line with the active settings, for the log and the console command.</summary>
        public string Describe()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return $"{(Enabled.Value ? "enabled" : "disabled")}, radius {Radius.Value.ToString("0.#", culture)} m, " +
                   $"hotbar {(IncludeHotbar.Value ? "included" : "excluded")}, {BuildRules().Describe()}, " +
                   $"find key {FindKey.Value}, highlight {HighlightDuration.Value.ToString("0.#", culture)} s, " +
                   $"button {(ShowButton.Value ? "shown" : "hidden")}, " +
                   $"scan {ScanRadius.Value.ToString("0.#", culture)} m every {ScanInterval.Value.ToString("0.#", culture)} s, " +
                   $"learning from chests {(LearnFromChests.Value ? "on" : "off")}, browser key {BrowserKey.Value}, " +
                   $"list sorted by {Sort.Value}, {FavoriteList.Parse(Favorites.Value).Count} pinned, {Sync.Describe()}";
        }
    }
}
