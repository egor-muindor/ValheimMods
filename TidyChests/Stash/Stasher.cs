using System;
using System.Collections.Generic;
using System.Globalization;
using TidyChests.Compat;
using TidyChests.Containers;
using UnityEngine;

namespace TidyChests.Stash
{
    /// <summary>
    /// The Stash button: classifies the player's items, snapshots the usable chests in range,
    /// lets the <see cref="StashPlanner"/> decide and executes the moves with the vanilla
    /// inventory calls (the same ones the "stack all" button uses, which chest-sharing mods
    /// intercept).
    /// </summary>
    internal static class Stasher
    {
        /// <summary>Runs one stash for the local player and returns a one-line summary for the log or console.</summary>
        public static string Stash(Player player)
        {
            if (!Plugin.Enabled)
            {
                return "disabled";
            }

            if (player == null || player.IsTeleporting())
            {
                return "no player";
            }

            ModConfig settings = Plugin.Settings;
            InventoryGui gui = InventoryGui.instance;
            if (gui != null)
            {
                gui.SetupDragItem(null, null, 1);
            }

            List<ItemDrop.ItemData> candidates = Candidates(player, settings, out List<ItemSnapshot> itemSnapshots);
            if (candidates.Count == 0)
            {
                return Report(player, settings, Translations.Get(Translations.Nothing), "no stashable items");
            }

            bool multiUserChest = ChestMods.MultiUserChestLoaded;
            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            List<Container> usable = UsableContainers(player, settings, playerId, multiUserChest, out List<ContainerSnapshot> containerSnapshots);
            if (usable.Count == 0)
            {
                string radius = settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture);
                return Report(player, settings, Translations.Get(Translations.NoChests, radius), "no usable chest in range");
            }

            StashPlan plan = StashPlanner.Plan(itemSnapshots, containerSnapshots);
            Plugin.Debug($"Plan: {plan.Moves.Count} moves, {plan.Units} units from {plan.ItemsTouched} stacks into {plan.ContainersUsed} chests");
            if (plan.Moves.Count == 0)
            {
                return Report(player, settings, Translations.Get(Translations.Nothing), "no chest holds these items");
            }

            Inventory inventory = player.GetInventory();
            int moved = 0;
            var touched = new HashSet<Container>();
            foreach (KeyValuePair<int, List<StashMove>> group in GroupByContainer(plan.Moves))
            {
                Container container = usable[group.Key];
                if (container == null || container.GetInventory() == null)
                {
                    continue;
                }

                if (!ContainerAccess.PrepareForWrite(container, multiUserChest))
                {
                    Plugin.Log.LogWarning($"Could not take ownership of {ContainerAccess.NameOf(container)}, skipping it");
                    continue;
                }

                foreach (StashMove move in group.Value)
                {
                    ItemDrop.ItemData item = candidates[move.Item];
                    int units = MoveInto(inventory, item, container.GetInventory(), move.Amount);
                    Plugin.Debug($"  {item.m_shared.m_name} x{move.Amount} -> {ContainerAccess.NameOf(container)} ({containerSnapshots[group.Key].Distance.ToString("0.#", CultureInfo.InvariantCulture)} m): {units} moved");
                    if (units > 0)
                    {
                        moved += units;
                        touched.Add(container);
                    }
                }
            }

            inventory.Changed();
            foreach (Container container in touched)
            {
                if (gui != null && container != null)
                {
                    gui.m_moveItemEffects.Create(container.transform.position, Quaternion.identity);
                }
            }

            string message = moved > 0 ? Translations.Get(Translations.Stashed, moved, touched.Count) : Translations.Get(Translations.Nothing);
            return Report(player, settings, message, $"{moved} units into {touched.Count} chests");
        }

        /// <summary>The stackable, unequipped, unprotected items in grid order, with their snapshots for the planner.</summary>
        private static List<ItemDrop.ItemData> Candidates(Player player, ModConfig settings, out List<ItemSnapshot> snapshots)
        {
            StashRules rules = settings.BuildRules();
            Func<ItemDrop.ItemData, bool>? inModSlot = SlotMods.GetClassifier();
            Inventory inventory = player.GetInventory();
            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            items.Sort((a, b) => a.m_gridPos.y != b.m_gridPos.y ? a.m_gridPos.y.CompareTo(b.m_gridPos.y) : a.m_gridPos.x.CompareTo(b.m_gridPos.x));
            Plugin.Debug($"Stash by {player.GetPlayerName()}: {items.Count} items, inventory {inventory.GetWidth()}x{inventory.GetHeight()}, {rules.Describe()}");

            var candidates = new List<ItemDrop.ItemData>();
            snapshots = new List<ItemSnapshot>();
            foreach (ItemDrop.ItemData item in items)
            {
                ItemFacts facts = Describe(item, player, inModSlot);
                StashVerdict verdict = rules.Judge(facts);
                Plugin.Debug($"  {facts.DisplayName} x{item.m_stack} at [{item.m_gridPos.x},{item.m_gridPos.y}] ({facts.TypeName}) -> {verdict}");
                if (verdict != StashVerdict.Stash)
                {
                    continue;
                }

                snapshots.Add(new ItemSnapshot(candidates.Count, item.m_shared.m_name, item.m_quality, item.m_worldLevel, item.m_stack, item.m_shared.m_maxStackSize));
                candidates.Add(item);
            }

            return candidates;
        }

        private static List<Container> UsableContainers(Player player, ModConfig settings, long playerId, bool multiUserChest, out List<ContainerSnapshot> snapshots)
        {
            var nearby = new List<Container>();
            ContainerRegistry.CollectNearby(player.transform.position, settings.Radius.Value, nearby);
            Plugin.Debug($"{nearby.Count} containers within {settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture)} m ({ContainerRegistry.Count} loaded), MultiUserChest {(multiUserChest ? "on" : "off")}");

            var usable = new List<Container>();
            snapshots = new List<ContainerSnapshot>();
            foreach (Container container in nearby)
            {
                float distance = Vector3.Distance(container.transform.position, player.transform.position);
                if (!ContainerAccess.CanStashInto(container, playerId, multiUserChest, out string reason))
                {
                    Plugin.Debug($"  skip {ContainerAccess.NameOf(container)} at {distance.ToString("0.#", CultureInfo.InvariantCulture)} m: {reason}");
                    continue;
                }

                Inventory inventory = container.GetInventory();
                var stacks = new List<StackSnapshot>();
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    stacks.Add(new StackSnapshot(item.m_shared.m_name, item.m_quality, item.m_worldLevel, item.m_stack, item.m_shared.m_maxStackSize));
                }

                snapshots.Add(new ContainerSnapshot(usable.Count, ContainerAccess.NameOf(container), distance, stacks, inventory.GetEmptySlots()));
                usable.Add(container);
            }

            return usable;
        }

        /// <summary>
        /// Moves up to <paramref name="amount"/> units of <paramref name="item"/> from the player's
        /// inventory into <paramref name="target"/> and returns how many were moved (or requested,
        /// when a chest-sharing mod answers asynchronously).
        ///
        /// A whole stack goes through <c>Inventory.AddItem</c> + <c>RemoveItem</c>, exactly like
        /// the vanilla "stack all" button. A part of a stack is merged into the matching stacks and
        /// then into empty cells with the positional <c>MoveItemToThis</c> the inventory drag and
        /// drop uses, so every unit passes through a call that other chest mods intercept.
        /// </summary>
        private static int MoveInto(Inventory from, ItemDrop.ItemData item, Inventory target, int amount)
        {
            if (!from.ContainsItem(item))
            {
                return 0;
            }

            amount = Math.Min(amount, item.m_stack);
            if (amount <= 0)
            {
                return 0;
            }

            if (amount == item.m_stack)
            {
                if (target.AddItem(item))
                {
                    from.RemoveItem(item);
                    return amount;
                }

                // AddItem tops up existing stacks before it gives up on the rest.
                return from.ContainsItem(item) ? Math.Max(0, amount - item.m_stack) : amount;
            }

            int moved = 0;
            foreach (ItemDrop.ItemData stack in new List<ItemDrop.ItemData>(target.GetAllItems()))
            {
                if (moved >= amount || !from.ContainsItem(item))
                {
                    break;
                }

                if (stack.m_shared.m_name != item.m_shared.m_name || stack.m_quality != item.m_quality || stack.m_worldLevel != item.m_worldLevel)
                {
                    continue;
                }

                int space = stack.m_shared.m_maxStackSize - stack.m_stack;
                if (space <= 0)
                {
                    continue;
                }

                moved += MoveTo(from, item, target, Math.Min(space, amount - moved), stack.m_gridPos);
            }

            for (int y = 0; y < target.GetHeight() && moved < amount; y++)
            {
                for (int x = 0; x < target.GetWidth() && moved < amount; x++)
                {
                    if (!from.ContainsItem(item) || target.GetItemAt(x, y) != null)
                    {
                        continue;
                    }

                    moved += MoveTo(from, item, target, Math.Min(item.m_shared.m_maxStackSize, amount - moved), new Vector2i(x, y));
                }
            }

            return moved;
        }

        private static int MoveTo(Inventory from, ItemDrop.ItemData item, Inventory target, int units, Vector2i cell)
        {
            int before = item.m_stack;
            bool accepted = target.MoveItemToThis(from, item, units, cell.x, cell.y);
            if (accepted)
            {
                return units;
            }

            // A refused move can still have merged a part; count what left the stack.
            return from.ContainsItem(item) ? Math.Max(0, before - item.m_stack) : before;
        }

        private static IEnumerable<KeyValuePair<int, List<StashMove>>> GroupByContainer(IReadOnlyList<StashMove> moves)
        {
            var order = new List<int>();
            var groups = new Dictionary<int, List<StashMove>>();
            foreach (StashMove move in moves)
            {
                if (!groups.TryGetValue(move.Container, out List<StashMove>? group))
                {
                    group = new List<StashMove>();
                    groups[move.Container] = group;
                    order.Add(move.Container);
                }

                group.Add(move);
            }

            foreach (int container in order)
            {
                yield return new KeyValuePair<int, List<StashMove>>(container, groups[container]);
            }
        }

        private static ItemFacts Describe(ItemDrop.ItemData item, Player player, Func<ItemDrop.ItemData, bool>? inModSlot)
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;
            return new ItemFacts(
                prefabName: item.m_dropPrefab != null ? item.m_dropPrefab.name : "",
                sharedName: shared.m_name,
                typeName: shared.m_itemType.ToString(),
                maxStack: shared.m_maxStackSize,
                equipped: item.m_equipped || player.IsItemEquiped(item),
                hotbar: item.m_gridPos.y == 0,
                modSlot: inModSlot != null && inModSlot(item),
                questItem: shared.m_questItem,
                gridX: item.m_gridPos.x,
                gridY: item.m_gridPos.y);
        }

        private static string Report(Player player, ModConfig settings, string message, string summary)
        {
            Plugin.Debug($"Stash result: {summary}");
            if (settings.ShowMessage.Value)
            {
                player.Message(MessageHud.MessageType.Center, message);
            }

            return summary;
        }
    }
}
