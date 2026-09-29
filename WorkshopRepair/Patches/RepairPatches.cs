using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WorkshopRepair.Repair;

namespace WorkshopRepair.Patches
{
    /// <summary>
    /// The two changes to the repair button of a crafting station.
    ///
    /// <c>InventoryGui.CanRepair</c> decides, item by item, whether the station the player is
    /// using repairs it; both the glow of the button and the repair itself go through it. A
    /// postfix lets another station nearby say yes when this one says no.
    ///
    /// <c>InventoryGui.RepairOneItem</c> is what the button calls, and repairs the first item
    /// that qualifies. A prefix replaces it with one that repairs them all.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class RepairPatches
    {
        private static readonly List<ItemDrop.ItemData> WornItems = new List<ItemDrop.ItemData>();

        private static readonly List<CraftingStation> UsedStations = new List<CraftingStation>();

        /// <summary>
        /// The nearby station the last <c>CanRepair</c> call relied on, or null when the player's
        /// own station was enough (or nothing was).
        /// </summary>
        private static CraftingStation? _lastNearbyStation;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.CanRepair))]
        private static void CanRepairPostfix(ItemDrop.ItemData item, ref bool __result)
        {
            _lastNearbyStation = null;
            if (__result || !Plugin.Enabled || !Plugin.Settings.UseNearbyStations.Value)
            {
                return;
            }

            _lastNearbyStation = FindNearbyStation(item);
            __result = _lastNearbyStation != null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(InventoryGui.RepairOneItem))]
        private static bool RepairOneItemPrefix(InventoryGui __instance)
        {
            if (!Plugin.Enabled || !Plugin.Settings.RepairAllAtOnce.Value)
            {
                return true;
            }

            RepairAll(__instance);
            return false;
        }

        /// <summary>A nearby station that repairs an item the player's own station refused.</summary>
        private static CraftingStation? FindNearbyStation(ItemDrop.ItemData item)
        {
            Player player = Player.m_localPlayer;
            if (player == null || item == null || !item.m_shared.m_canBeReparied)
            {
                return null;
            }

            CraftingStation current = player.GetCurrentCraftingStation();
            if (current == null || !current.m_canRepair)
            {
                return null;
            }

            Recipe? recipe = ObjectDB.instance != null ? ObjectDB.instance.GetRecipe(item) : null;
            return recipe != null ? NearbyStations.Find(player, current, recipe) : null;
        }

        /// <summary>
        /// Vanilla <c>RepairOneItem</c>, carried on to the end of the list: the same checks, the
        /// same skill gain and durability per item, one sound per station that did the work, and
        /// one message for the lot.
        /// </summary>
        private static void RepairAll(InventoryGui gui)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            CraftingStation current = player.GetCurrentCraftingStation();
            if ((current == null && !player.NoCostCheat()) || (current != null && !current.CheckUsable(player, showMessage: false)))
            {
                return;
            }

            WornItems.Clear();
            UsedStations.Clear();
            player.GetInventory().GetWornItems(WornItems);

            int repaired = 0;
            ItemDrop.ItemData? only = null;
            foreach (ItemDrop.ItemData item in WornItems)
            {
                if (!gui.CanRepair(item))
                {
                    continue;
                }

                CraftingStation? station = _lastNearbyStation != null ? _lastNearbyStation : current;
                player.RaiseSkill(Skills.SkillType.Crafting, 1f - item.m_durability / item.GetMaxDurability());
                item.m_durability = item.GetMaxDurability();
                if (station != null && !UsedStations.Contains(station))
                {
                    UsedStations.Add(station);
                }

                Plugin.Debug($"Repaired {item.m_shared.m_name} at {(station != null ? station.m_name : "no station (no-cost mode)")}");
                repaired++;
                only = item;
            }

            foreach (CraftingStation station in UsedStations)
            {
                station.m_repairItemDoneEffects.Create(station.transform.position, Quaternion.identity);
            }

            UsedStations.Clear();
            WornItems.Clear();

            if (repaired == 0)
            {
                // Vanilla's own words, untranslated in the game as well.
                player.Message(MessageHud.MessageType.Center, "No more item to repair");
            }
            else if (repaired == 1)
            {
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", only!.m_shared.m_name));
            }
            else
            {
                player.Message(MessageHud.MessageType.Center, RepairRules.SeveralRepairedMessage(repaired));
            }
        }
    }
}
