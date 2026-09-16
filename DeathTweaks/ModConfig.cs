using System;
using BepInEx.Configuration;
using DeathTweaks.Rules;
using Muindor.ServerConfig;
using UnityEngine;

namespace DeathTweaks
{
    /// <summary>
    /// Typed access to the config file. Sections and keys match the original DeathTweaks
    /// mod so an existing <c>.cfg</c> can be reused by renaming it.
    ///
    /// Every rule of the mod is bound through <see cref="Sync"/>: a server that also runs
    /// DeathTweaks with <c>ConfigPriority</c> on decides them for everyone, so a death means
    /// the same thing for every player on it. Only <see cref="IsDebug"/> stays local.
    /// </summary>
    public sealed class ModConfig
    {
        private static readonly string[] ItemTypeNames = Enum.GetNames(typeof(ItemDrop.ItemData.ItemType));

        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;
            string typeList = string.Join(", ", ItemTypeNames);

            Sync = new ConfigSync(config, MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, Plugin.Log);

            Enabled = Sync.Bind("General", "Enabled", true, "Enable this mod.");
            IsDebug = config.Bind("General", "IsDebug", false, "Log every item decision on death.");

            KeepItemTypes = Sync.Bind("ItemLists", "KeepItemTypes", "",
                $"Item types to keep (comma-separated). Valid types: {typeList}");
            DropItemTypes = Sync.Bind("ItemLists", "DropItemTypes", "",
                "Item types to drop even when KeepTeleportableItems would keep them (comma-separated).");
            DestroyItemTypes = Sync.Bind("ItemLists", "DestroyItemTypes", "",
                "Item types to destroy (comma-separated). Overrides the keep and drop lists.");
            KeepItemNames = Sync.Bind("ItemLists", "KeepItems", "",
                "Items to keep (comma-separated). Use prefab names, for example: Iron,IronScrap,CopperOre");
            DropItemNames = Sync.Bind("ItemLists", "DropItems", "",
                "Items to drop even when KeepTeleportableItems would keep them (comma-separated prefab names).");
            DestroyItemNames = Sync.Bind("ItemLists", "DestroyItems", "",
                "Items to destroy (comma-separated prefab names). Overrides the keep and drop lists.");

            KeepAllItems = Sync.Bind("Toggles", "KeepAllItems", false,
                "Keep everything. Overrides all other item options.");
            DestroyAllItems = Sync.Bind("Toggles", "DestroyAllItems", false,
                "Destroy everything except quest items. Overrides all other item options except KeepAllItems.");
            KeepEquippedItems = Sync.Bind("Toggles", "KeepEquippedItems", false,
                "Keep equipped items. Overrides the item lists.");
            KeepTeleportableItems = Sync.Bind("Toggles", "KeepTeleportableItems", false,
                "Keep items that can go through portals. Does not override the item lists.");
            KeepHotbarItems = Sync.Bind("Toggles", "KeepHotbarItems", false,
                "Keep items in the first inventory row. Overrides the item lists.");
            KeepQuickSlotItems = Sync.Bind("Toggles", "KeepQuickSlotItems", false,
                "Keep items in quick slots. Overrides the item lists. EquipmentAndQuickSlots 3.x: quick slots. Extra Slots: quick, food, ammo and misc slots.");
            UseTombStone = Sync.Bind("Toggles", "UseTombStone", true,
                "Put dropped items in a tombstone. When false they are scattered on the ground.");
            CreateDeathEffects = Sync.Bind("Toggles", "CreateDeathEffects", true,
                "Create the death effects (ragdoll and particles).");
            KeepFoodLevels = Sync.Bind("Toggles", "KeepFoodLevels", false,
                "Keep active food after respawn.");

            UseFixedSpawnCoordinates = Sync.Bind("Spawn", "UseFixedSpawnCoordinates", false,
                "Respawn at FixedSpawnCoordinates after death.");
            SpawnAtStart = Sync.Bind("Spawn", "SpawnAtStart", false,
                "Respawn at the start location after death. Takes precedence over UseFixedSpawnCoordinates.");
            FixedSpawnCoordinates = Sync.Bind("Spawn", "FixedSpawnCoordinates", Vector3.zero,
                "World coordinates used when UseFixedSpawnCoordinates is on.");

            NoSkillProtection = Sync.Bind("Skills", "NoSkillProtection", false,
                "Disable the skill-loss protection that normally follows a recent death.");
            ReduceSkills = Sync.Bind("Skills", "ReduceSkills", true,
                "Lower skills on death. When false, skills are never lowered or reset, whatever the world modifiers say.");
            SkillReduceFactor = Sync.Bind("Skills", "SkillReduceFactor", 0.25f,
                new ConfigDescription(
                    "Fraction of each skill lost on a hard death (vanilla: 0.25). Multiplied by the SkillReductionRate world modifier.",
                    new AcceptableValueRange<float>(0f, 1f)));
        }

        /// <summary>The settings a server may decide, and where the current ones come from.</summary>
        public ConfigSync Sync { get; }

        public SyncedEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public SyncedEntry<string> KeepItemTypes { get; }

        public SyncedEntry<string> DropItemTypes { get; }

        public SyncedEntry<string> DestroyItemTypes { get; }

        public SyncedEntry<string> KeepItemNames { get; }

        public SyncedEntry<string> DropItemNames { get; }

        public SyncedEntry<string> DestroyItemNames { get; }

        public SyncedEntry<bool> KeepAllItems { get; }

        public SyncedEntry<bool> DestroyAllItems { get; }

        public SyncedEntry<bool> KeepEquippedItems { get; }

        public SyncedEntry<bool> KeepTeleportableItems { get; }

        public SyncedEntry<bool> KeepHotbarItems { get; }

        public SyncedEntry<bool> KeepQuickSlotItems { get; }

        public SyncedEntry<bool> UseTombStone { get; }

        public SyncedEntry<bool> CreateDeathEffects { get; }

        public SyncedEntry<bool> KeepFoodLevels { get; }

        public SyncedEntry<bool> UseFixedSpawnCoordinates { get; }

        public SyncedEntry<bool> SpawnAtStart { get; }

        public SyncedEntry<Vector3> FixedSpawnCoordinates { get; }

        public SyncedEntry<bool> NoSkillProtection { get; }

        public SyncedEntry<bool> ReduceSkills { get; }

        public SyncedEntry<float> SkillReduceFactor { get; }

        /// <summary>Re-reads the config file from disk.</summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>Builds a fresh, validated rule set from the current config values.</summary>
        public DeathRules BuildRules()
        {
            var settings = new DeathRuleSettings
            {
                KeepAllItems = KeepAllItems.Value,
                DestroyAllItems = DestroyAllItems.Value,
                KeepEquippedItems = KeepEquippedItems.Value,
                KeepHotbarItems = KeepHotbarItems.Value,
                KeepQuickSlotItems = KeepQuickSlotItems.Value,
                KeepTeleportableItems = KeepTeleportableItems.Value,
                KeepItemTypes = KeepItemTypes.Value,
                DropItemTypes = DropItemTypes.Value,
                DestroyItemTypes = DestroyItemTypes.Value,
                KeepItemNames = KeepItemNames.Value,
                DropItemNames = DropItemNames.Value,
                DestroyItemNames = DestroyItemNames.Value,
            };

            return DeathRules.Parse(settings, ItemTypeNames, warning => Plugin.Log.LogWarning(warning));
        }
    }
}
