using System.Globalization;
using BepInEx.Configuration;
using Muindor.ServerConfig;
using OreFinder.Detection;
using OreFinder.Highlight;
using OreFinder.Map;
using UnityEngine;

namespace OreFinder
{
    /// <summary>
    /// Typed access to <c>muindor.OreFinder.cfg</c>.
    ///
    /// What is found, and what it is called on the map, is bound through <see cref="Sync"/>: a server
    /// that also runs OreFinder with <c>ConfigPriority</c> on decides it, so a party sees the same
    /// targets and writes the same pin names on a shared map. Keys and the look of the highlight
    /// stay each player's own.
    /// </summary>
    public sealed class ModConfig
    {
        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;

            Sync = new ConfigSync(config, MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, Plugin.Log);

            Enabled = Sync.Bind("General", "Enabled", true,
                "Enable the finder. The toggle key flips this setting in game and saves it, unless the server decides it.");
            ToggleKey = config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.F9),
                "Key that turns the finder on and off in game. Modifiers are allowed, e.g. \"F9 + LeftControl\". Ignored while typing in the chat or the console.");
            OresToggleKey = config.Bind("General", "OresToggleKey", KeyboardShortcut.Empty,
                "Key that turns the ore search (FindOres) on and off on its own, leaving the other targets as they are. Unset by default.");
            DungeonsToggleKey = config.Bind("General", "DungeonsToggleKey", KeyboardShortcut.Empty,
                "Key that turns the dungeon entrance search (Dungeons) on and off on its own. Unset by default.");
            SpawnersToggleKey = config.Bind("General", "SpawnersToggleKey", KeyboardShortcut.Empty,
                "Key that turns the spawner search (Spawners) on and off on its own. Unset by default.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log every vein that is found, with its prefab and the item that made it count as ore.");

            Radius = Sync.Bind("Detection", "Radius", 20f,
                new ConfigDescription("Search radius in metres around the player.",
                    new AcceptableValueRange<float>(1f, 200f)));
            ScanInterval = Sync.Bind("Detection", "ScanInterval", 1f,
                new ConfigDescription("Seconds between two scans.",
                    new AcceptableValueRange<float>(0.1f, 30f)));
            FindOres = Sync.Bind("Detection", "FindOres", true,
                "Look for ores at all. Off = only the targets from the Targets section are found; the Ores list below still says which ores count. " +
                "The console command 'orefinder ores on|off' and OresToggleKey flip this setting.");
            Ores = Sync.Bind("Detection", "Ores", "",
                "Which ores to look for, comma-separated. Empty = every ore: any mineable object that drops an item " +
                "whose name contains the word Ore or Scrap (copper, tin, silver, iron scrap piles, flametal). " +
                "Otherwise list item names (CopperOre, TinOre, SilverOre, IronScrap, FlametalOre, FlametalOreNew, ...) " +
                "or object prefab names (rock4_copper, silvervein, MineRock_Obsidian, ...). Plain rocks, obsidian and " +
                "black marble are not ores and only show up when listed here.");

            Dungeons = Sync.Bind("Targets", "Dungeons", true,
                "Find the entrances of crypts, caves and mines: any door with an Enter prompt (burial chambers, sunken crypts, troll caves, frost caves, infested mines, ...).");
            Roots = Sync.Bind("Targets", "Roots", true,
                "Find ancient roots (the sap extractor spots in the Mistlands).");
            Spawners = Sync.Bind("Targets", "Spawners", true,
                "Find monster spawners: greydwarf nests, evil bone piles, body piles and the like, plus the invisible spawn points that respawn " +
                "their creature (surtling spawners at fire geysers, ...). Spawn points that fire only once are skipped.");
            Pickables = Sync.Bind("Targets", "Pickables", "Pickable_DragonEgg, Pickable_Mushroom_JotunPuffs, Pickable_Mushroom_Magecap, Pickable_Fiddlehead, Pickable_VoltureEgg",
                "Pickables to find, by the object's prefab name (Pickable_DragonEgg, Pickable_Mushroom_JotunPuffs, Pickable_Mushroom_Magecap, Pickable_Fiddlehead, " +
                "Pickable_VoltureEgg, Pickable_Thistle, CloudberryBush, Pickable_BogIronOre, ...) or by the item it gives (DragonEgg, Thistle, Cloudberry, ...), " +
                "comma-separated. Already picked ones are skipped until they regrow. Empty = none.");
            Trees = Sync.Bind("Targets", "Trees", "",
                "Trees to find, by the wood they drop (YggdrasilWood, Blackwood, Frostwood, ElderBark, FineWood, ...), comma-separated. Empty = none.");
            TargetRadius = Sync.Bind("Targets", "TargetRadius", 40f,
                new ConfigDescription("Search radius in metres for everything except ores (Radius is for ores).",
                    new AcceptableValueRange<float>(1f, 200f)));

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

            WishboneNeeded = Sync.Bind("Hidden", "WishboneNeeded", WishboneRule.InInventory,
                "Hidden ores are the ones the game marks for the Wishbone: silver veins and the scrap piles with a beacon. " +
                "InInventory: found only while the Wishbone is anywhere in your inventory. Equipped: only while it is equipped, " +
                "like the game's own finder. NotNeeded: always found. Hidden veins skipped for lack of the Wishbone are found later once you carry it.");
            WishboneItem = Sync.Bind("Hidden", "WishboneItem", "Wishbone",
                "Item that counts as the Wishbone, by prefab name or $item_ name. Change it for a modded finder item.");

            CustomNames = Sync.Bind("Names", "CustomNames", false,
                "Show your own names from the Names setting instead of the game's names (Copper deposit, Silver vein, ...) " +
                "in the screen marker, the message and the map pin.");
            Names = Sync.Bind("Names", "Names",
                "CopperOre=C, TinOre=T, SilverOre=S, IronScrap=I, FlametalOre=F, FlametalOreNew=F, GoldOre=B, Obsidian=O, Pickable_Mushroom_Magecap=Mc, Pickable_Fiddlehead=Fh, $item_ancientroot=YR",
                "Your names as key=name pairs separated by commas. The key is the ore item (CopperOre, GoldOre, ...), the pickable's prefab or item (Pickable_DragonEgg, DragonEgg), " +
                "the wood of a tree (YggdrasilWood), a dungeon's location key ($location_forestcrypt), a spawner's prefab (Spawner_GreydwarfNest) or the object prefab (rock4_copper, silvervein). " +
                "Targets without a pair get the initials of their name (Burial Chambers = BC, Dragon egg = DE, Magecap = Ma). Only used when CustomNames is on.");

            MapPin = Sync.Bind("Map", "MapPin", true,
                "Add a dot pin named after the ore to the map when a vein is found. The pin is saved with your map like one you placed yourself.");
            MapPinSpacing = Sync.Bind("Map", "MapPinSpacing", 10f,
                new ConfigDescription("Do not add a pin when any other pin (yours, the mod's, a death marker, ...) is within this many metres. 0 = always add.",
                    new AcceptableValueRange<float>(0f, 100f)));
            OrePin = Sync.Bind("Map", "OrePin", PinIcon.Dot, "Map pin icon for ores.");
            DungeonPin = Sync.Bind("Map", "DungeonPin", PinIcon.House, "Map pin icon for dungeon entrances.");
            SpawnerPin = Sync.Bind("Map", "SpawnerPin", PinIcon.Hammer, "Map pin icon for spawners.");
            OtherPin = Sync.Bind("Map", "OtherPin", PinIcon.Dot, "Map pin icon for roots, pickables and trees.");
        }

        /// <summary>The settings a server may decide, and where the current ones come from.</summary>
        public ConfigSync Sync { get; }

        public SyncedEntry<bool> Enabled { get; }

        public ConfigEntry<KeyboardShortcut> ToggleKey { get; }

        public ConfigEntry<KeyboardShortcut> OresToggleKey { get; }

        public ConfigEntry<KeyboardShortcut> DungeonsToggleKey { get; }

        public ConfigEntry<KeyboardShortcut> SpawnersToggleKey { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public SyncedEntry<float> Radius { get; }

        public SyncedEntry<float> ScanInterval { get; }

        public SyncedEntry<bool> FindOres { get; }

        public SyncedEntry<string> Ores { get; }

        public SyncedEntry<bool> Dungeons { get; }

        public SyncedEntry<bool> Roots { get; }

        public SyncedEntry<bool> Spawners { get; }

        public SyncedEntry<string> Pickables { get; }

        public SyncedEntry<string> Trees { get; }

        public SyncedEntry<float> TargetRadius { get; }

        public ConfigEntry<float> Duration { get; }

        public ConfigEntry<bool> ScreenMarker { get; }

        public ConfigEntry<bool> Beam { get; }

        public ConfigEntry<bool> Light { get; }

        public ConfigEntry<bool> Glow { get; }

        public ConfigEntry<bool> Message { get; }

        public SyncedEntry<WishboneRule> WishboneNeeded { get; }

        public SyncedEntry<string> WishboneItem { get; }

        public SyncedEntry<bool> CustomNames { get; }

        public SyncedEntry<string> Names { get; }

        public SyncedEntry<bool> MapPin { get; }

        public SyncedEntry<float> MapPinSpacing { get; }

        public SyncedEntry<PinIcon> OrePin { get; }

        public SyncedEntry<PinIcon> DungeonPin { get; }

        public SyncedEntry<PinIcon> SpawnerPin { get; }

        public SyncedEntry<PinIcon> OtherPin { get; }

        /// <summary>The on/off setting of a group, or null for the groups that are lists (pickables, trees).</summary>
        public SyncedEntry<bool>? SwitchFor(TargetGroup group)
        {
            switch (group)
            {
                case TargetGroup.Ore:
                    return FindOres;
                case TargetGroup.Dungeon:
                    return Dungeons;
                case TargetGroup.Root:
                    return Roots;
                case TargetGroup.Spawner:
                    return Spawners;
                default:
                    return null;
            }
        }

        /// <summary>The map pin icon for a group.</summary>
        public PinIcon PinFor(TargetGroup group)
        {
            switch (group)
            {
                case TargetGroup.Ore:
                    return OrePin.Value;
                case TargetGroup.Dungeon:
                    return DungeonPin.Value;
                case TargetGroup.Spawner:
                    return SpawnerPin.Value;
                default:
                    return OtherPin.Value;
            }
        }

        /// <summary>The non-ore targets from the config.</summary>
        public TargetOptions ToTargetOptions()
        {
            return new TargetOptions
            {
                Ores = FindOres.Value,
                Dungeons = Dungeons.Value,
                Roots = Roots.Value,
                Spawners = Spawners.Value,
                Pickables = NameList.Parse(Pickables.Value),
                Trees = NameList.Parse(Trees.Value),
            };
        }

        /// <summary>Everything the catalog depends on, to know when to rebuild it.</summary>
        public string CatalogSource()
        {
            return string.Join("\n", FindOres.Value, Ores.Value, Dungeons.Value, Roots.Value, Spawners.Value, Pickables.Value, Trees.Value, CustomNames.Value, CustomNames.Value ? Names.Value : string.Empty);
        }

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
            return $"{(Enabled.Value ? "enabled" : "disabled")}, radius {Radius.Value.ToString("0.#", culture)} m for ores, " +
                   $"{TargetRadius.Value.ToString("0.#", culture)} m for the rest, scan every {ScanInterval.Value.ToString("0.##", culture)} s, " +
                   $"targets: {ToTargetOptions().Describe()}, ore list: {ores}, " +
                   $"highlight {Duration.Value.ToString("0.#", culture)} s, map pins {(MapPin.Value ? "on" : "off")}, " +
                   $"names {(CustomNames.Value ? OreNames.Parse(Names.Value).Describe() : "from the game")}, " +
                   $"hidden ores {(WishboneNeeded.Value == WishboneRule.NotNeeded ? "always" : $"need {WishboneItem.Value} {(WishboneNeeded.Value == WishboneRule.Equipped ? "equipped" : "in the inventory")}")}, " +
                   $"toggle key {ToggleKey.Value}, {Sync.Describe()}";
        }
    }
}
