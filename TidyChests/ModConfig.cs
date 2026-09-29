using System;
using System.Globalization;
using BepInEx.Configuration;
using Muindor.ServerConfig;
using TidyChests.Find;
using TidyChests.Index;
using TidyChests.Restock;
using TidyChests.Sort;
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
            PlaceInSortOrder = config.Bind("Stash", "PlaceInSortOrder", true,
                "When the Stash button needs an empty cell in a chest, pick the one where the item fits [Sort] SortOrder and SortLayout best, next to the same item when there is room. " +
                "Nothing already in the chest is moved; only the Sort button does that. Off: the first empty cell, like the game's own buttons.");

            RevealKey = config.Bind("Locks", "RevealKey", KeyCode.LeftShift,
                "Hold this key with the inventory open to see the locks: a grey padlock on a locked item, a red one on a locked slot. Pointing at a slot while holding it shows the lock keys.");
            LockItemKey = config.Bind("Locks", "LockItemKey", new KeyboardShortcut(KeyCode.L, KeyCode.LeftShift),
                "With the inventory open, point at an item and press this to lock or unlock it: a locked item is never stashed, wherever it lies. Modifiers are allowed.");
            LockSlotKey = config.Bind("Locks", "LockSlotKey", new KeyboardShortcut(KeyCode.K, KeyCode.LeftShift),
                "With the inventory open, point at a slot and press this to lock or unlock it: whatever lies in a locked slot is never stashed. Modifiers are allowed.");
            LockedItems = config.Bind("Locks", "LockedItems", "",
                "Items locked with the lock key, comma-separated, saved here as you press it. The names are the game's own item names: $item_wood, $item_coal");
            LockedSlots = config.Bind("Locks", "LockedSlots", "",
                "Inventory slots locked with the lock key, comma-separated as column:row counted from 0, the top row (the hotbar) being row 0: 0:1, 7:3. Saved here as you press the key.");

            ShowRestockButton = config.Bind("Restock", "ShowRestockButton", true,
                "Show the Restock button in the inventory, under the Stash button. It fills the marked slots and the slot mods' slots from the chests within [Stash] Radius. The console command 'tidychests restock' works without it.");
            RestockButtonOffset = config.Bind("Restock", "RestockButtonOffset", new Vector2(0f, -42f),
                "Position of the Restock button relative to the Stash button, in UI pixels (x right, y up). It has the Stash button's size.");
            RestockSlotKey = config.Bind("Restock", "RestockSlotKey", new KeyboardShortcut(KeyCode.R, KeyCode.LeftShift),
                "With the inventory open, point at a slot and press this to mark it for the Restock button: the first press keeps a full stack there, the second HalfPercent of a stack, the third removes the mark. " +
                "Hold [Locks] RevealKey to see the marks: a green arrow for a full stack, a red one for less. Modifiers are allowed.");
            RestockSlots = config.Bind("Restock", "RestockSlots", "",
                "Slots marked with the restock key, comma-separated as column:row=percent, then the item last seen there so an emptied slot is refilled with it: 0:1=100:$item_arrow_wood, 3:2=50:$item_cookedmeat. " +
                "Rows are counted from 0, the top row (the hotbar) being row 0. Saved here as you press the key. What lies in a marked slot is never stashed.");
            HalfPercent = config.Bind("Restock", "HalfPercent", 50,
                new ConfigDescription("The second press of the restock key keeps this many percent of a stack in the slot.",
                    new AcceptableValueRange<int>(1, 99)));
            ModSlotFood = config.Bind("Restock", "ModSlotFood", 50,
                new ConfigDescription("Food lying in a slot mod's slot (Extra Slots quick and food slots, EquipmentAndQuickSlots quick slots) is kept at this many percent of a stack. 0 leaves it alone.",
                    new AcceptableValueRange<int>(0, 100)));
            ModSlotMeads = config.Bind("Restock", "ModSlotMeads", 0,
                new ConfigDescription("Meads and other consumables that do not feed, lying in a slot mod's slot, are kept at this many percent of a stack. 0 leaves them alone.",
                    new AcceptableValueRange<int>(0, 100)));
            ModSlotAmmo = config.Bind("Restock", "ModSlotAmmo", 100,
                new ConfigDescription("Arrows, bolts and other ammo lying in a slot mod's slot (Extra Slots ammo and quick slots, the Better Archery quiver) are kept at this many percent of a stack. 0 leaves them alone.",
                    new AcceptableValueRange<int>(0, 100)));
            ModSlotOther = config.Bind("Restock", "ModSlotOther", 0,
                new ConfigDescription("Any other stackable item lying in a slot mod's slot is kept at this many percent of a stack. 0 leaves it alone.",
                    new AcceptableValueRange<int>(0, 100)));

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

            ShowSortButton = config.Bind("Sort", "ShowSortButton", true,
                "Show the Sort button on the chest panel. It sorts the open chest only, and only when pressed. The console command 'tidychests sort' works without it.");
            SortOrder = config.Bind("Sort", "SortOrder", ChestSortOrder.Id,
                "Order of the items in a sorted chest. Id: by the item's prefab name (Coal, Stone, Wood), the same in every language. " +
                "Name: by the name shown in the current language. Type: weapons, shields, tools, armour, ammo, food, materials, trophies, then the rest, each group by Id. " +
                "The better quality and the higher world level come first within the same item.");
            SortLayout = config.Bind("Sort", "SortLayout", ChestSortLayout.Columns,
                "How a sorted chest is laid out. Columns: every item starts a new column, filled top to bottom. Rows: every item starts a new row, filled left to right. " +
                "Sequential: everything packed left to right with no gaps. When there are more items than columns (or rows), the rest is packed without gaps.");
            SortButtonOffset = config.Bind("Sort", "SortButtonOffset", Vector2.zero,
                "Shift of the Sort button from its own place, next to the chest panel's \"stack all\" button, in UI pixels (x right, y up).");

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

        public ConfigEntry<bool> PlaceInSortOrder { get; }

        public ConfigEntry<KeyCode> RevealKey { get; }

        public ConfigEntry<KeyboardShortcut> LockItemKey { get; }

        public ConfigEntry<KeyboardShortcut> LockSlotKey { get; }

        public ConfigEntry<string> LockedItems { get; }

        public ConfigEntry<string> LockedSlots { get; }

        public ConfigEntry<bool> ShowRestockButton { get; }

        public ConfigEntry<Vector2> RestockButtonOffset { get; }

        public ConfigEntry<KeyboardShortcut> RestockSlotKey { get; }

        public ConfigEntry<string> RestockSlots { get; }

        public ConfigEntry<int> HalfPercent { get; }

        public ConfigEntry<int> ModSlotFood { get; }

        public ConfigEntry<int> ModSlotMeads { get; }

        public ConfigEntry<int> ModSlotAmmo { get; }

        public ConfigEntry<int> ModSlotOther { get; }

        public ConfigEntry<KeyboardShortcut> FindKey { get; }

        public ConfigEntry<float> HighlightDuration { get; }

        public ConfigEntry<bool> ScreenMarker { get; }

        public ConfigEntry<bool> Light { get; }

        public ConfigEntry<bool> Glow { get; }

        public ConfigEntry<bool> CloseInventory { get; }

        public ConfigEntry<bool> ShowButton { get; }

        public ConfigEntry<Vector2> ButtonOffset { get; }

        public ConfigEntry<Vector2> ButtonSize { get; }

        public ConfigEntry<bool> ShowSortButton { get; }

        public ConfigEntry<ChestSortOrder> SortOrder { get; }

        public ConfigEntry<ChestSortLayout> SortLayout { get; }

        public ConfigEntry<Vector2> SortButtonOffset { get; }

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
                LockedItems = LockedItems.Value,
                LockedSlots = LockedSlots.Value,
                RestockSlots = RestockSlots.Value,
            };

            return StashRules.Parse(settings, ItemTypeNames, warning => Plugin.Log.LogWarning(warning));
        }

        /// <summary>The current locks. Call <see cref="SaveLocks"/> after changing them.</summary>
        public StashLocks BuildLocks()
        {
            return StashLocks.Parse(LockedItems.Value, LockedSlots.Value);
        }

        /// <summary>Writes the locks back into the config file.</summary>
        public void SaveLocks(StashLocks locks)
        {
            LockedItems.Value = locks.FormatItems();
            LockedSlots.Value = locks.FormatSlots();
        }

        /// <summary>The current restock marks. Call <see cref="SaveRestockMarks"/> after changing them.</summary>
        public RestockMarks BuildRestockMarks()
        {
            return RestockMarks.Parse(RestockSlots.Value);
        }

        /// <summary>Writes the restock marks back into the config file, when they changed.</summary>
        public void SaveRestockMarks(RestockMarks marks)
        {
            string line = marks.Format();
            if (line != RestockSlots.Value)
            {
                RestockSlots.Value = line;
            }
        }

        /// <summary>How full the items in slot mods' slots are kept.</summary>
        public RestockLevels BuildRestockLevels()
        {
            return new RestockLevels(ModSlotFood.Value, ModSlotMeads.Value, ModSlotAmmo.Value, ModSlotOther.Value);
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
                   $"hotbar {(IncludeHotbar.Value ? "included" : "excluded")}, {BuildRules().Describe()}, new stacks {(PlaceInSortOrder.Value ? "in sort order" : "in the first empty cell")}, " +
                   $"restock button {(ShowRestockButton.Value ? "shown" : "hidden")}, restock key {RestockSlotKey.Value} (half {HalfPercent.Value}%), {BuildRestockLevels().Describe()}, " +
                   $"find key {FindKey.Value}, lock keys {LockItemKey.Value} / {LockSlotKey.Value} (shown while {RevealKey.Value} is held), highlight {HighlightDuration.Value.ToString("0.#", culture)} s, " +
                   $"button {(ShowButton.Value ? "shown" : "hidden")}, " +
                   $"sort button {(ShowSortButton.Value ? "shown" : "hidden")} ({SortOrder.Value}, {SortLayout.Value}), " +
                   $"scan {ScanRadius.Value.ToString("0.#", culture)} m every {ScanInterval.Value.ToString("0.#", culture)} s, " +
                   $"learning from chests {(LearnFromChests.Value ? "on" : "off")}, browser key {BrowserKey.Value}, " +
                   $"list sorted by {Sort.Value}, {FavoriteList.Parse(Favorites.Value).Count} pinned, {Sync.Describe()}";
        }
    }
}
