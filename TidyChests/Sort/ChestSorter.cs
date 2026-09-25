using System.Collections.Generic;
using TidyChests.Compat;
using TidyChests.Containers;
using UnityEngine;

namespace TidyChests.Sort
{
    /// <summary>
    /// The Sort button: sorts the chest that is open, and only that one. The
    /// <see cref="SortPlanner"/> decides the result; this class reads the chest and writes it
    /// back in one of two ways.
    ///
    /// The owner of the chest (always the case without MultiUserChest, since opening a chest
    /// hands its ownership to whoever opens it) rewrites the stacks in place and saves once:
    /// one ZDO write, however many stacks moved. A player who does not own it, because another
    /// player opened it first under MultiUserChest, sends the planned moves as MultiUserChest's
    /// own move requests; the owner applies them in order and refuses any whose source no
    /// longer holds the expected item, so a chest changed in the meantime ends up partly
    /// sorted but never loses an item.
    /// </summary>
    internal static class ChestSorter
    {
        /// <summary>Presses sooner than this after a sort are ignored.</summary>
        private const float Cooldown = 0.2f;

        /// <summary>After a sort through MultiUserChest, the owner's result has to arrive before the next one is planned.</summary>
        private const float RemoteCooldown = 1f;

        private static float _nextSort;

        /// <summary>Sorts the open chest and returns a one-line summary for the log or console.</summary>
        public static string SortOpenChest()
        {
            if (!Plugin.Enabled)
            {
                return "disabled";
            }

            float now = Time.unscaledTime;
            if (now < _nextSort)
            {
                return "too soon after the last sort";
            }

            InventoryGui gui = InventoryGui.instance;
            Player player = Player.m_localPlayer;
            if (gui == null || gui.m_currentContainer == null || player == null)
            {
                return "no chest open";
            }

            Container container = gui.m_currentContainer;
            Inventory inventory = container.GetInventory();
            ZNetView view = container.m_nview;
            if (inventory == null || view == null || !view.IsValid())
            {
                return "chest not ready";
            }

            _nextSort = now + Cooldown;
            gui.SetupDragItem(null, null, 1);

            ModConfig settings = Plugin.Settings;
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            SortPlan plan = SortPlanner.Plan(Snapshot(items), inventory.GetWidth(), inventory.GetHeight(), settings.SortOrder.Value, settings.SortLayout.Value);
            string name = ContainerAccess.NameOf(container);
            Plugin.Debug($"Sort {name} ({inventory.GetWidth()}x{inventory.GetHeight()}) by {settings.SortOrder.Value}, {settings.SortLayout.Value}: " +
                         $"{items.Count} stacks, {plan.Kinds} kinds, {plan.Moves.Count} moves, owner {view.IsOwner()}");
            if (!plan.HasChanges)
            {
                return "already sorted";
            }

            if (view.IsOwner())
            {
                Apply(inventory, items, plan);
                return $"sorted {name} in place";
            }

            if (ChestMods.MultiUserChestLoaded && MultiUserChestApi.CanMoveInChest)
            {
                Request(container, items, plan);
                _nextSort = now + RemoteCooldown;
                return $"sent {plan.Moves.Count} moves to the owner of {name}" + (plan.Skipped > 0 ? $", {plan.Skipped} stacks left in place" : "");
            }

            player.Message(MessageHud.MessageType.Center, Translations.Get(Translations.SortInUse));
            return "chest owned by another player";
        }

        private static List<SortStack> Snapshot(List<ItemDrop.ItemData> items)
        {
            var stacks = new List<SortStack>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                ItemDrop.ItemData.SharedData shared = item.m_shared;
                string id = item.m_dropPrefab != null ? item.m_dropPrefab.name : shared.m_name;
                stacks.Add(new SortStack(i, new GridCell(item.m_gridPos.x, item.m_gridPos.y), id, shared.m_name,
                    Localization.instance.Localize(shared.m_name), SortKeys.TypeRank(shared.m_itemType.ToString()),
                    item.m_quality, shared.m_maxQuality, item.m_worldLevel, item.m_stack, shared.m_maxStackSize));
            }

            return stacks;
        }

        /// <summary>Writes the sorted state straight into the owned chest, then saves it once.</summary>
        private static void Apply(Inventory inventory, List<ItemDrop.ItemData> items, SortPlan plan)
        {
            foreach (SortPlacement placement in plan.Placements)
            {
                ItemDrop.ItemData item = items[placement.Index];
                if (placement.Removed)
                {
                    inventory.m_inventory.Remove(item);
                    continue;
                }

                item.m_gridPos = new Vector2i(placement.Cell.X, placement.Cell.Y);
                item.m_stack = placement.Count;
            }

            inventory.Changed();
        }

        /// <summary>
        /// Sends the planned moves to the chest's owner. Each request names the source cell
        /// through the grid position of the item handed over, so the item is a copy placed where
        /// the stack will be by then; the local chest does not change until the owner's result
        /// arrives.
        /// </summary>
        private static void Request(Container container, List<ItemDrop.ItemData> items, SortPlan plan)
        {
            foreach (SortMove move in plan.Moves)
            {
                ItemDrop.ItemData copy = items[move.Index].Clone();
                copy.m_gridPos = new Vector2i(move.From.X, move.From.Y);
                copy.m_stack = move.StackCount;
                MultiUserChestApi.MoveInChest(container, copy, new Vector2i(move.To.X, move.To.Y), move.Amount);
            }
        }
    }
}
