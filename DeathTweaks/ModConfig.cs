using System;
using BepInEx.Configuration;
using DeathTweaks.Rules;
using UnityEngine;

namespace DeathTweaks
{
    /// <summary>
    /// Typed access to the config file. Sections and keys match the original DeathTweaks
    /// mod so an existing <c>.cfg</c> can be reused by renaming it.
    /// </summary>
    public sealed class ModConfig
    {
        private static readonly string[] ItemTypeNames = Enum.GetNames(typeof(ItemDrop.ItemData.ItemType));

        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;
            string typeList = string.Join(", ", ItemTypeNames);

            Enabled = config.Bind("General", "Enabled", true, "Enable this mod.");
            IsDebug = config.Bind("General", "IsDebug", false, "Log every item decision on death.");

            KeepItemTypes = config.Bind("ItemLists", "KeepItemTypes", "",
                $"Item types to keep (comma-separated). Valid types: {typeList}");
            DropItemTypes = config.Bind("ItemLists", "DropItemTypes", "",
                "Item types to drop even when KeepTeleportableItems would keep them (comma-separated).");
            DestroyItemTypes = config.Bind("ItemLists", "DestroyItemTypes", "",
                "Item types to destroy (comma-separated). Overrides the keep and drop lists.");
            KeepItemNames = config.Bind("ItemLists", "KeepItems", "",
                "Items to keep (comma-separated). Use prefab names, for example: Iron,IronScrap,CopperOre");
            DropItemNames = config.Bind("ItemLists", "DropItems", "",
                "Items to drop even when KeepTeleportableItems would keep them (comma-separated prefab names).");
            DestroyItemNames = config.Bind("ItemLists", "DestroyItems", "",
                "Items to destroy (comma-separated prefab names). Overrides the keep and drop lists.");

            KeepAllItems = config.Bind("Toggles", "KeepAllItems", false,
                "Keep everything. Overrides all other item options.");
            DestroyAllItems = config.Bind("Toggles", "DestroyAllItems", false,
                "Destroy everything except quest items. Overrides all other item options except KeepAllItems.");
            KeepEquippedItems = config.Bind("Toggles", "KeepEquippedItems", false,
                "Keep equipped items. Overrides the item lists.");
            KeepTeleportableItems = config.Bind("Toggles", "KeepTeleportableItems", false,
                "Keep items that can go through portals. Does not override the item lists.");
            KeepHotbarItems = config.Bind("Toggles", "KeepHotbarItems", false,
                "Keep items in the first inventory row. Overrides the item lists.");
            KeepQuickSlotItems = config.Bind("Toggles", "KeepQuickSlotItems", false,
                "Keep items in EquipmentAndQuickSlots quick slots. Overrides the item lists. Requires EquipmentAndQuickSlots 3.x.");
            UseTombStone = config.Bind("Toggles", "UseTombStone", true,
                "Put dropped items in a tombstone. When false they are scattered on the ground.");
            CreateDeathEffects = config.Bind("Toggles", "CreateDeathEffects", true,
                "Create the death effects (ragdoll and particles).");
            KeepFoodLevels = config.Bind("Toggles", "KeepFoodLevels", false,
                "Keep active food after respawn.");

            UseFixedSpawnCoordinates = config.Bind("Spawn", "UseFixedSpawnCoordinates", false,
                "Respawn at FixedSpawnCoordinates after death.");
            SpawnAtStart = config.Bind("Spawn", "SpawnAtStart", false,
                "Respawn at the start location after death. Takes precedence over UseFixedSpawnCoordinates.");
            FixedSpawnCoordinates = config.Bind("Spawn", "FixedSpawnCoordinates", Vector3.zero,
                "World coordinates used when UseFixedSpawnCoordinates is on.");

            NoSkillProtection = config.Bind("Skills", "NoSkillProtection", false,
                "Disable the skill-loss protection that normally follows a recent death.");
            ReduceSkills = config.Bind("Skills", "ReduceSkills", true,
                "Lower skills on death. When false, skills are never lowered or reset, whatever the world modifiers say.");
            SkillReduceFactor = config.Bind("Skills", "SkillReduceFactor", 0.25f,
                new ConfigDescription(
                    "Fraction of each skill lost on a hard death (vanilla: 0.25). Multiplied by the SkillReductionRate world modifier.",
                    new AcceptableValueRange<float>(0f, 1f)));
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public ConfigEntry<string> KeepItemTypes { get; }

        public ConfigEntry<string> DropItemTypes { get; }

        public ConfigEntry<string> DestroyItemTypes { get; }

        public ConfigEntry<string> KeepItemNames { get; }

        public ConfigEntry<string> DropItemNames { get; }

        public ConfigEntry<string> DestroyItemNames { get; }

        public ConfigEntry<bool> KeepAllItems { get; }

        public ConfigEntry<bool> DestroyAllItems { get; }

        public ConfigEntry<bool> KeepEquippedItems { get; }

        public ConfigEntry<bool> KeepTeleportableItems { get; }

        public ConfigEntry<bool> KeepHotbarItems { get; }

        public ConfigEntry<bool> KeepQuickSlotItems { get; }

        public ConfigEntry<bool> UseTombStone { get; }

        public ConfigEntry<bool> CreateDeathEffects { get; }

        public ConfigEntry<bool> KeepFoodLevels { get; }

        public ConfigEntry<bool> UseFixedSpawnCoordinates { get; }

        public ConfigEntry<bool> SpawnAtStart { get; }

        public ConfigEntry<Vector3> FixedSpawnCoordinates { get; }

        public ConfigEntry<bool> NoSkillProtection { get; }

        public ConfigEntry<bool> ReduceSkills { get; }

        public ConfigEntry<float> SkillReduceFactor { get; }

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
