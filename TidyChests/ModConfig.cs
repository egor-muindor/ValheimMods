using System;
using System.Globalization;
using BepInEx.Configuration;
using TidyChests.Find;
using TidyChests.Stash;
using UnityEngine;

namespace TidyChests
{
    /// <summary>Typed access to <c>muindor.TidyChests.cfg</c>.</summary>
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

            Enabled = config.Bind("General", "Enabled", true, "Enable this mod: the Stash button and the find key.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log every item decision and every chest that is considered when stashing.");

            Radius = config.Bind("Stash", "Radius", 10f,
                new ConfigDescription("Chests within this many metres of the player are used, both by the Stash button and by the find key.",
                    new AcceptableValueRange<float>(1f, 20f)));
            IncludeHotbar = config.Bind("Stash", "IncludeHotbar", false,
                "Also stash items from the first inventory row (the hotbar).");
            ItemTypes = config.Bind("Stash", "ItemTypes", DefaultItemTypes,
                $"Item types that may be stashed (comma-separated). Equipment, tools and weapons are left out by default. Valid types: {typeList}");
            Blacklist = config.Bind("Stash", "Blacklist", "",
                "Items that are never stashed (comma-separated). Use prefab names or item names, for example: Wood, $item_coal, Resin");
            ShowMessage = config.Bind("Stash", "ShowMessage", true,
                "Show a message with the result after stashing.");

            FindKey = config.Bind("Find", "FindKey", new KeyboardShortcut(KeyCode.T),
                "With the inventory open, point at an item and press this key: the inventory closes and every chest in range that holds the item is highlighted. Modifiers are allowed, e.g. \"T + LeftControl\".");
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
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public ConfigEntry<float> Radius { get; }

        public ConfigEntry<bool> IncludeHotbar { get; }

        public ConfigEntry<string> ItemTypes { get; }

        public ConfigEntry<string> Blacklist { get; }

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
                   $"button {(ShowButton.Value ? "shown" : "hidden")}";
        }
    }
}
