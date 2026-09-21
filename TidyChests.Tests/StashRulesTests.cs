using System.Collections.Generic;
using TidyChests.Stash;
using Xunit;

namespace TidyChests.Tests
{
    public class StashRulesTests
    {
        private static readonly string[] KnownTypes =
        {
            "None", "Material", "Consumable", "OneHandedWeapon", "Bow", "Shield", "Helmet", "Chest", "Ammo",
            "Legs", "Hands", "Trophy", "TwoHandedWeapon", "Torch", "Misc", "Shoulder", "Utility", "Tool",
            "Fish", "AmmoNonEquipable", "Trinket",
        };

        private const string DefaultTypes = "Material, Consumable, Ammo, AmmoNonEquipable, Trophy, Misc, Fish";

        private static StashRules Rules(string types = DefaultTypes, string blacklist = "", bool includeHotbar = false, List<string>? warnings = null, string lockedItems = "", string lockedSlots = "")
        {
            var settings = new StashRuleSettings { ItemTypes = types, Blacklist = blacklist, IncludeHotbar = includeHotbar, LockedItems = lockedItems, LockedSlots = lockedSlots };
            return StashRules.Parse(settings, KnownTypes, warnings == null ? null : warnings.Add);
        }

        private static ItemFacts Item(
            string prefab = "Wood",
            string shared = "$item_wood",
            string type = "Material",
            int maxStack = 50,
            bool equipped = false,
            bool hotbar = false,
            bool modSlot = false,
            bool quest = false,
            int x = -1,
            int y = -1)
        {
            return new ItemFacts(prefab, shared, type, maxStack, equipped, hotbar, modSlot, quest, x, y);
        }

        [Fact]
        public void LockedItemStaysWhereverItSits()
        {
            StashRules rules = Rules(lockedItems: "$item_wood");
            Assert.Equal(StashVerdict.LockedItem, rules.Judge(Item(x: 2, y: 3)));
            Assert.Equal(StashVerdict.LockedItem, rules.Judge(Item()));
            Assert.Equal(StashVerdict.Stash, rules.Judge(Item(prefab: "Coal", shared: "$item_coal", x: 2, y: 3)));
        }

        [Fact]
        public void LockedSlotKeepsWhateverLiesInIt()
        {
            StashRules rules = Rules(lockedSlots: "2:3");
            Assert.Equal(StashVerdict.LockedSlot, rules.Judge(Item(x: 2, y: 3)));
            Assert.Equal(StashVerdict.LockedSlot, rules.Judge(Item(prefab: "Coal", shared: "$item_coal", x: 2, y: 3)));
            Assert.Equal(StashVerdict.Stash, rules.Judge(Item(x: 3, y: 2)));
            Assert.Equal(StashVerdict.Stash, rules.Judge(Item()));
        }

        [Fact]
        public void SlotLockWinsOverItemLockInTheVerdict()
        {
            StashRules rules = Rules(lockedItems: "$item_wood", lockedSlots: "2:3");
            Assert.Equal(StashVerdict.LockedSlot, rules.Judge(Item(x: 2, y: 3)));
        }

        [Fact]
        public void DescribeCountsTheLocks()
        {
            Assert.Contains("1 locked items, 2 locked slots", Rules(lockedItems: "$item_wood", lockedSlots: "0:1, 1:1").Describe());
        }

        [Fact]
        public void RegularMaterialInTheGridIsStashed()
        {
            Assert.Equal(StashVerdict.Stash, Rules().Judge(Item()));
        }

        [Fact]
        public void UnstackableItemsStay()
        {
            Assert.Equal(StashVerdict.NotStackable, Rules().Judge(Item(prefab: "Tankard", shared: "$item_tankard", type: "Misc", maxStack: 1)));
        }

        [Fact]
        public void EquippedItemsStay_EvenWhenTheirTypeIsAllowed()
        {
            Assert.Equal(StashVerdict.Equipped, Rules(types: DefaultTypes + ", Torch").Judge(Item(prefab: "Torch", type: "Torch", maxStack: 2, equipped: true)));
            Assert.Equal(StashVerdict.Equipped, Rules().Judge(Item(equipped: true)));
        }

        [Fact]
        public void ToolsWeaponsAndArmourAreExcludedByType()
        {
            StashRules rules = Rules();

            Assert.Equal(StashVerdict.TypeExcluded, rules.Judge(Item(prefab: "AxeBronze", type: "OneHandedWeapon", maxStack: 2)));
            Assert.Equal(StashVerdict.TypeExcluded, rules.Judge(Item(prefab: "Hammer", type: "Tool", maxStack: 2)));
            Assert.Equal(StashVerdict.TypeExcluded, rules.Judge(Item(prefab: "HelmetBronze", type: "Helmet", maxStack: 2)));
            Assert.Equal(StashVerdict.TypeExcluded, rules.Judge(Item(prefab: "Wishbone", type: "Utility", maxStack: 2)));
        }

        [Fact]
        public void HotbarItemsStay_UnlessIncluded()
        {
            ItemFacts food = Item(prefab: "CookedMeat", shared: "$item_cookedmeat", type: "Consumable", hotbar: true);

            Assert.Equal(StashVerdict.Hotbar, Rules().Judge(food));
            Assert.Equal(StashVerdict.Stash, Rules(includeHotbar: true).Judge(food));
        }

        [Fact]
        public void ItemsInModSlotsStay_EvenOnTheHotbarSetting()
        {
            Assert.Equal(StashVerdict.ModSlot, Rules(includeHotbar: true).Judge(Item(modSlot: true)));
        }

        [Fact]
        public void QuestItemsStay()
        {
            Assert.Equal(StashVerdict.QuestItem, Rules().Judge(Item(quest: true)));
        }

        [Fact]
        public void BlacklistMatchesPrefabAndItemNames_CaseInsensitive_WithOrWithoutDollar()
        {
            StashRules rules = Rules(blacklist: " wood , $ITEM_COAL;Resin ");

            Assert.Equal(StashVerdict.Blacklisted, rules.Judge(Item(prefab: "Wood", shared: "$item_wood")));
            Assert.Equal(StashVerdict.Blacklisted, rules.Judge(Item(prefab: "Coal", shared: "$item_coal")));
            Assert.Equal(StashVerdict.Blacklisted, rules.Judge(Item(prefab: "Resin", shared: "$item_resin")));
            Assert.Equal(StashVerdict.Stash, rules.Judge(Item(prefab: "Stone", shared: "$item_stone")));
        }

        [Fact]
        public void ItemWithoutPrefabIsNotBlacklistedByAnEmptyName()
        {
            StashRules rules = Rules(blacklist: "Wood");

            Assert.Equal(StashVerdict.Stash, rules.Judge(Item(prefab: "", shared: "$item_stone")));
        }

        [Fact]
        public void UnknownTypeNamesAreReportedAndIgnored()
        {
            var warnings = new List<string>();
            StashRules rules = Rules(types: "Material, Junk", warnings: warnings);

            Assert.Single(warnings);
            Assert.Contains("Junk", warnings[0]);
            Assert.Equal(new[] { "Material" }, rules.ItemTypes);
        }

        [Fact]
        public void TypeNamesAreCaseInsensitive()
        {
            StashRules rules = Rules(types: "material,CONSUMABLE");

            Assert.Equal(StashVerdict.Stash, rules.Judge(Item(type: "Material")));
            Assert.Equal(StashVerdict.Stash, rules.Judge(Item(type: "Consumable")));
            Assert.Equal(StashVerdict.TypeExcluded, rules.Judge(Item(type: "Trophy")));
        }

        [Fact]
        public void EmptyTypeListStashesNothing()
        {
            Assert.Equal(StashVerdict.TypeExcluded, Rules(types: "").Judge(Item()));
        }

        [Fact]
        public void DescribeListsTypesAndBlacklist()
        {
            Assert.Equal("types: Consumable, Material; blacklist: Coal, Wood; 0 locked items, 0 locked slots", Rules(types: "Material, Consumable", blacklist: "Wood, Coal").Describe());
            Assert.Equal("types: Material; blacklist: item_coal; 0 locked items, 0 locked slots", Rules(types: "Material", blacklist: "$item_coal").Describe());
            Assert.Equal("types: none; blacklist: empty; 0 locked items, 0 locked slots", Rules(types: "").Describe());
        }
    }
}
