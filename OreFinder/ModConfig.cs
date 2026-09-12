using System.Globalization;
using BepInEx.Configuration;
using OreFinder.Detection;
using OreFinder.Highlight;
using UnityEngine;

namespace OreFinder
{
    /// <summary>Typed access to <c>muindor.OreFinder.cfg</c>.</summary>
    public sealed class ModConfig
    {
        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;

            Enabled = config.Bind("General", "Enabled", true,
                "Enable the finder. The toggle key flips this setting in game and saves it.");
            ToggleKey = config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.F9),
                "Key that turns the finder on and off in game. Modifiers are allowed, e.g. \"F9 + LeftControl\". Ignored while typing in the chat or the console.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log every vein that is found, with its prefab and the item that made it count as ore.");

            Radius = config.Bind("Detection", "Radius", 20f,
                new ConfigDescription("Search radius in metres around the player.",
                    new AcceptableValueRange<float>(1f, 200f)));
            ScanInterval = config.Bind("Detection", "ScanInterval", 1f,
                new ConfigDescription("Seconds between two scans.",
                    new AcceptableValueRange<float>(0.1f, 30f)));
            Ores = config.Bind("Detection", "Ores", "",
                "Which ores to look for, comma-separated. Empty = every ore: any mineable object that drops an item " +
                "whose name contains the word Ore or Scrap (copper, tin, silver, iron scrap piles, flametal). " +
                "Otherwise list item names (CopperOre, TinOre, SilverOre, IronScrap, FlametalOre, FlametalOreNew, ...) " +
                "or object prefab names (rock4_copper, silvervein, MineRock_Obsidian, ...). Plain rocks, obsidian and " +
                "black marble are not ores and only show up when listed here.");

            Duration = config.Bind("Highlight", "Duration", 10f,
                new ConfigDescription("Seconds a newly found vein stays highlighted. Every vein is highlighted once; " +
                    "use the console command 'orefinder reset' to forget the veins already shown.",
                    new AcceptableValueRange<float>(1f, 120f)));
            ScreenMarker = config.Bind("Highlight", "ScreenMarker", true,
                "Draw an arrow with the ore name and distance on the screen: over the vein while it is in view, " +
                "at the screen edge pointing towards it otherwise.");
            Beam = config.Bind("Highlight", "Beam", true,
                "Draw a vertical light beam above the vein.");
            Light = config.Bind("Highlight", "Light", true,
                "Light up the vein and the ground around it with a coloured, pulsing light.");
            Glow = config.Bind("Highlight", "Glow", true,
                "Tint the vein's own material with an emissive glow. Only works for materials whose shader has an emission colour.");
            Message = config.Bind("Highlight", "Message", true,
                "Show a message in the top-left corner with the ore name and distance when a vein is found.");

            MapPin = config.Bind("Map", "MapPin", true,
                "Add a dot pin named after the ore to the map when a vein is found. The pin is saved with your map like one you placed yourself.");
            MapPinSpacing = config.Bind("Map", "MapPinSpacing", 10f,
                new ConfigDescription("Do not add a pin when any other pin (yours, the mod's, a death marker, ...) is within this many metres. 0 = always add.",
                    new AcceptableValueRange<float>(0f, 100f)));
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<KeyboardShortcut> ToggleKey { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public ConfigEntry<float> Radius { get; }

        public ConfigEntry<float> ScanInterval { get; }

        public ConfigEntry<string> Ores { get; }

        public ConfigEntry<float> Duration { get; }

        public ConfigEntry<bool> ScreenMarker { get; }

        public ConfigEntry<bool> Beam { get; }

        public ConfigEntry<bool> Light { get; }

        public ConfigEntry<bool> Glow { get; }

        public ConfigEntry<bool> Message { get; }

        public ConfigEntry<bool> MapPin { get; }

        public ConfigEntry<float> MapPinSpacing { get; }

        /// <summary>Re-reads the config file from disk.</summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>Snapshot of the highlight options for one vein.</summary>
        public HighlightOptions ToHighlightOptions()
        {
            return new HighlightOptions(Duration.Value, Beam.Value, Light.Value, Glow.Value);
        }

        /// <summary>One line with the active settings, for the log and the console command.</summary>
        public string Describe()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            string ores = OreFilter.Parse(Ores.Value).Describe();
            return $"{(Enabled.Value ? "enabled" : "disabled")}, radius {Radius.Value.ToString("0.#", culture)} m, " +
                   $"scan every {ScanInterval.Value.ToString("0.##", culture)} s, ores: {ores}, " +
                   $"highlight {Duration.Value.ToString("0.#", culture)} s, map pins {(MapPin.Value ? "on" : "off")}, toggle key {ToggleKey.Value}";
        }
    }
}
