using System;
using System.Collections.Generic;
using DeathTweaks.Rules;
using Xunit;

namespace DeathTweaks.Tests
{
    public class DeathRulesTests
    {
        private static readonly string[] KnownTypes =
        {
            "None", "Material", "Consumable", "OneHandedWeapon", "Bow", "Shield", "Helmet", "Chest",
            "Ammo", "Customization", "Legs", "Hands", "Trophy", "TwoHandedWeapon", "Torch", "Misc",
            "Shoulder", "Utility", "Tool", "Attach_Atgeir", "Fish", "TwoHandedWeaponLeft",
            "AmmoNonEquipable", "Trinket",
        };

        private static DeathRules Rules(Action<DeathRuleSettings>? configure = null, List<string>? warnings = null)
        {
            var settings = new DeathRuleSettings();
            configure?.Invoke(settings);
            return DeathRules.Parse(settings, KnownTypes, warnings == null ? null : warnings.Add);
        }

        private static ItemFacts Item(
            string prefab = "Iron",
            string type = "Material",
            bool equipped = false,
            bool hotbar = false,
            bool quickSlot = false,
            bool teleportable = true,
            bool quest = false,
            string shared = "$item_iron")
        {
            return new ItemFacts(prefab, shared, type, equipped, hotbar, quickSlot, teleportable, quest);
        }

        [Fact]
        public void DefaultSettings_DropEverythingExceptQuestItems()
        {
            var rules = Rules();

            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(equipped: true), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(hotbar: true), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(quest: true), WorldDeathModifiers.None));
        }

        [Fact]
        public void KeepAllItems_OverridesEverything()
        {
            var rules = Rules(s =>
            {
                s.KeepAllItems = true;
                s.DestroyAllItems = true;
                s.DestroyItemTypes = "Material";
            });

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(), WorldDeathModifiers.None));
        }

        [Fact]
        public void DestroyAllItems_DestroysEquippedAndHotbar_ButNotQuestItems()
        {
            var rules = Rules(s =>
            {
                s.DestroyAllItems = true;
                s.KeepEquippedItems = true;
                s.KeepHotbarItems = true;
                s.KeepItemTypes = "Material";
            });

            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(equipped: true, type: "Chest"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(hotbar: true), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(quest: true), WorldDeathModifiers.None));
        }

        [Fact]
        public void KeepEquipped_Hotbar_QuickSlot_BeatItemLists()
        {
            var rules = Rules(s =>
            {
                s.KeepEquippedItems = true;
                s.KeepHotbarItems = true;
                s.KeepQuickSlotItems = true;
                s.DestroyItemTypes = "Chest,Material";
                s.DestroyItemNames = "Iron";
            });

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(type: "Chest", equipped: true), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(hotbar: true), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(quickSlot: true), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(), WorldDeathModifiers.None));
        }

        [Fact]
        public void KeepToggles_DoNothingWhenItemDoesNotMatch()
        {
            var rules = Rules(s =>
            {
                s.KeepEquippedItems = true;
                s.KeepHotbarItems = true;
                s.KeepQuickSlotItems = true;
            });

            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(), WorldDeathModifiers.None));
        }

        [Fact]
        public void DestroyLists_BeatKeepLists()
        {
            var rules = Rules(s =>
            {
                s.DestroyItemTypes = "Material";
                s.KeepItemTypes = "Material";
                s.KeepItemNames = "Iron";
            });

            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(), WorldDeathModifiers.None));
        }

        [Fact]
        public void KeepLists_BeatDropLists()
        {
            var rules = Rules(s =>
            {
                s.KeepItemNames = "Iron";
                s.DropItemTypes = "Material";
            });

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(prefab: "Iron"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(prefab: "Copper"), WorldDeathModifiers.None));
        }

        [Fact]
        public void DropLists_BeatKeepTeleportable()
        {
            var rules = Rules(s =>
            {
                s.KeepTeleportableItems = true;
                s.DropItemNames = "Iron";
            });

            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(prefab: "Iron"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(prefab: "Wood"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(prefab: "CopperOre", teleportable: false), WorldDeathModifiers.None));
        }

        [Fact]
        public void ListEntries_AreTrimmedAndCaseInsensitive()
        {
            var rules = Rules(s =>
            {
                s.KeepItemTypes = " material , TROPHY ,, ";
                s.KeepItemNames = " iron ;";
            });

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(type: "Material", prefab: "Copper"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(type: "Trophy", prefab: "TrophyBoar"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(type: "Misc", prefab: "IRON"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(type: "Misc", prefab: "Copper"), WorldDeathModifiers.None));
        }

        [Fact]
        public void NameLists_MatchSharedNameToken()
        {
            var rules = Rules(s => s.KeepItemNames = "$item_iron");

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(prefab: "Iron", shared: "$item_iron"), WorldDeathModifiers.None));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(prefab: "", shared: "$item_iron"), WorldDeathModifiers.None));
        }

        [Fact]
        public void UnknownTypeNames_ProduceWarnings_AndAreIgnored()
        {
            var warnings = new List<string>();
            var rules = Rules(s => s.KeepItemTypes = "Material,Sword", warnings);

            Assert.Single(warnings);
            Assert.Contains("Sword", warnings[0]);
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(type: "Material"), WorldDeathModifiers.None));
        }

        [Fact]
        public void WorldKeepInventory_KeepsEverything()
        {
            var rules = Rules(s => s.DestroyAllItems = true);
            var world = new WorldDeathModifiers(keepInventory: true, keepEquip: false, deleteItems: false, deleteUnequipped: false);

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(), world));
        }

        [Fact]
        public void WorldKeepEquip_ProtectsEquippedItemsOnly()
        {
            var rules = Rules(s => s.DestroyItemTypes = "Chest");
            var world = new WorldDeathModifiers(keepInventory: false, keepEquip: true, deleteItems: false, deleteUnequipped: false);

            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(type: "Chest", equipped: true), world));
            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(type: "Chest", equipped: false), world));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(), world));
        }

        [Fact]
        public void WorldDeleteItems_TurnsDropsIntoDestroy_ButNotKeeps()
        {
            var rules = Rules(s => s.KeepItemNames = "Iron");
            var world = new WorldDeathModifiers(keepInventory: false, keepEquip: false, deleteItems: true, deleteUnequipped: false);

            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(prefab: "Copper"), world));
            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(prefab: "Copper", equipped: true, type: "Chest"), world));
            Assert.Equal(ItemFate.Keep, rules.Resolve(Item(prefab: "Iron"), world));
        }

        [Fact]
        public void WorldDeleteUnequipped_DestroysOnlyUnequippedDrops()
        {
            var rules = Rules();
            var world = new WorldDeathModifiers(keepInventory: false, keepEquip: false, deleteItems: false, deleteUnequipped: true);

            Assert.Equal(ItemFate.Destroy, rules.Resolve(Item(), world));
            Assert.Equal(ItemFate.Drop, rules.Resolve(Item(type: "Chest", equipped: true), world));
        }

        [Fact]
        public void Describe_ListsActiveRules()
        {
            var rules = Rules(s =>
            {
                s.KeepEquippedItems = true;
                s.KeepItemTypes = "Material";
            });

            string text = rules.Describe();

            Assert.Contains("KeepEquippedItems", text);
            Assert.Contains("Material", text);
        }
    }
}
