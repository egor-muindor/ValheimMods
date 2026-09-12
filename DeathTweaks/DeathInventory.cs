using System;
using System.Collections.Generic;
using DeathTweaks.Compat;
using DeathTweaks.Rules;
using UnityEngine;

namespace DeathTweaks
{
    /// <summary>
    /// Applies the item rules to the dying player's inventory around the vanilla
    /// <c>Player.CreateTombStone</c> call.
    ///
    /// Kept items are pulled out of the live inventory list before vanilla runs and put back
    /// afterwards, so vanilla still creates the grave with <c>Inventory.MoveInventoryToGrave</c>
    /// (grid positions, inventory size, world modifiers and other mods' tombstone patches all
    /// keep working). Destroyed items are removed up front. Dropped items are left to vanilla,
    /// or scattered on the ground when tombstones are disabled.
    /// </summary>
    internal static class DeathInventory
    {
        private sealed class HeldItem
        {
            public HeldItem(ItemDrop.ItemData item, bool wasEquipped)
            {
                Item = item;
                WasEquipped = wasEquipped;
            }

            public ItemDrop.ItemData Item { get; }

            public bool WasEquipped { get; }
        }

        private static readonly List<HeldItem> Held = new List<HeldItem>();

        /// <summary>
        /// Prepares the inventory for the vanilla tombstone code.
        /// Returns true when vanilla <c>CreateTombStone</c> should still run.
        /// </summary>
        public static bool Prepare(Player player)
        {
            if (Held.Count > 0)
            {
                // Should be impossible; a stale entry would resurrect an item from an earlier grave.
                Plugin.Log.LogWarning($"Discarding {Held.Count} stale held items from a previous death");
                Held.Clear();
            }

            DeathRules rules = Plugin.Settings.BuildRules();
            WorldDeathModifiers world = ReadWorldModifiers();
            Inventory inventory = player.GetInventory();
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            HashSet<ItemDrop.ItemData>? quickSlotItems = null;
            if (Plugin.Settings.KeepQuickSlotItems.Value)
            {
                quickSlotItems = QuickSlotMods.GetQuickSlotItems();
                if (quickSlotItems == null)
                {
                    Plugin.Log.LogWarning($"KeepQuickSlotItems is on but no supported quick slot mod is available ({QuickSlotMods.SupportedMods}); quick slot items are treated as regular items");
                }
            }

            Plugin.Debug($"Death of {player.GetPlayerName()}: {items.Count} items, rules: {rules.Describe()}, world modifiers: {world}, quick slot items: {(quickSlotItems == null ? "n/a" : quickSlotItems.Count.ToString())}");

            var keep = new List<ItemDrop.ItemData>();
            var drop = new List<ItemDrop.ItemData>();
            var destroy = new List<ItemDrop.ItemData>();

            foreach (ItemDrop.ItemData item in items)
            {
                ItemFacts facts = Describe(item, quickSlotItems);
                ItemFate fate = rules.Resolve(facts, world);
                Plugin.Debug($"  {facts.DisplayName} ({facts.TypeName}{(facts.Equipped ? ", equipped" : "")}{(facts.QuickSlot ? ", quick slot" : "")}{(facts.Hotbar ? ", hotbar" : "")}) -> {fate}");

                switch (fate)
                {
                    case ItemFate.Keep:
                        keep.Add(item);
                        break;
                    case ItemFate.Destroy:
                        destroy.Add(item);
                        break;
                    default:
                        drop.Add(item);
                        break;
                }
            }

            if (drop.Count == 0 && destroy.Count == 0)
            {
                Plugin.Debug("Nothing leaves the inventory, skipping tombstone");
                return false;
            }

            foreach (ItemDrop.ItemData item in destroy)
            {
                Unequip(player, item);
                inventory.RemoveItem(item);
            }

            foreach (ItemDrop.ItemData item in keep)
            {
                Held.Add(new HeldItem(item, item.m_equipped));
                items.Remove(item);
            }

            if (Plugin.Settings.UseTombStone.Value)
            {
                inventory.Changed();
                return true;
            }

            foreach (ItemDrop.ItemData item in drop)
            {
                // The item leaves the inventory only once its copy exists in the world, so a
                // failed drop keeps the item with the player instead of losing it.
                if (!TryScatterOnGround(player, item))
                {
                    continue;
                }

                Unequip(player, item);
                items.Remove(item);
            }

            inventory.Changed();
            return false;
        }

        /// <summary>Puts kept items back. Safe to call more than once.</summary>
        public static void RestoreHeld(Player player)
        {
            if (Held.Count == 0)
            {
                return;
            }

            List<ItemDrop.ItemData> items = player.GetInventory().GetAllItems();

            foreach (HeldItem held in Held)
            {
                // Vanilla unequips through the humanoid's slot references while the item is out
                // of the inventory. Restoring the flag makes the death save store the item as
                // worn, and the respawned player re-equips it from that flag.
                held.Item.m_equipped = held.WasEquipped;

                if (!items.Contains(held.Item))
                {
                    items.Add(held.Item);
                }
            }

            Plugin.Debug($"Restored {Held.Count} kept items");
            Held.Clear();
            player.GetInventory().Changed();
        }

        private static ItemFacts Describe(ItemDrop.ItemData item, HashSet<ItemDrop.ItemData>? quickSlotItems)
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;

            return new ItemFacts(
                prefabName: item.m_dropPrefab != null ? item.m_dropPrefab.name : "",
                sharedName: shared.m_name,
                typeName: shared.m_itemType.ToString(),
                equipped: item.m_equipped,
                hotbar: item.m_gridPos.y == 0,
                quickSlot: quickSlotItems != null && quickSlotItems.Contains(item),
                teleportable: shared.m_teleportable,
                questItem: shared.m_questItem);
        }

        private static WorldDeathModifiers ReadWorldModifiers()
        {
            ZoneSystem zoneSystem = ZoneSystem.instance;
            if (zoneSystem == null)
            {
                return WorldDeathModifiers.None;
            }

            return new WorldDeathModifiers(
                keepInventory: zoneSystem.GetGlobalKey(GlobalKeys.DeathKeepInventory),
                keepEquip: zoneSystem.GetGlobalKey(GlobalKeys.DeathKeepEquip),
                deleteItems: zoneSystem.GetGlobalKey(GlobalKeys.DeathDeleteItems),
                deleteUnequipped: zoneSystem.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped));
        }

        private static void Unequip(Player player, ItemDrop.ItemData item)
        {
            if (item.m_equipped)
            {
                player.UnequipItem(item, triggerEquipEffects: false);
            }
        }

        private static bool TryScatterOnGround(Player player, ItemDrop.ItemData item)
        {
            if (item.m_dropPrefab == null)
            {
                Plugin.Log.LogWarning($"{item.m_shared.m_name} has no drop prefab and cannot be placed on the ground; keeping it");
                return false;
            }

            try
            {
                Vector3 position = player.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                ItemDrop.DropItem(item, 0, position, rotation);
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Log.LogWarning($"Could not drop {item.m_shared.m_name} on the ground; keeping it: {exception.Message}");
                return false;
            }
        }
    }
}
