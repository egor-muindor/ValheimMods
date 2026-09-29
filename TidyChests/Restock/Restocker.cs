using System;
using System.Collections.Generic;
using System.Globalization;
using TidyChests.Compat;
using TidyChests.Containers;
using UnityEngine;

namespace TidyChests.Restock
{
    /// <summary>
    /// The Restock button: collects the slots to top up (the marked ones and the items in slot
    /// mods' slots), snapshots the usable chests in range, lets the <see cref="RestockPlanner"/>
    /// decide and takes the items with the positional <c>MoveItemToThis</c> the inventory drag
    /// and drop uses, which MultiUserChest turns into a request to the chest's owner.
    /// </summary>
    internal static class Restocker
    {
        /// <summary>Presses sooner than this after a restock are ignored.</summary>
        private const float Cooldown = 0.2f;

        /// <summary>After a request through MultiUserChest, the owner's answer has to arrive before the next restock is planned.</summary>
        private const float RemoteCooldown = 1f;

        private static float _next;

        /// <summary>A slot to top up: where it is, and the planner's view of it.</summary>
        private readonly struct Slot
        {
            public Slot(Vector2i position, RestockTarget target)
            {
                Position = position;
                Target = target;
            }

            public Vector2i Position { get; }

            public RestockTarget Target { get; }
        }

        /// <summary>Runs one restock for the local player and returns a one-line summary for the log or console.</summary>
        public static string Restock(Player player)
        {
            if (!Plugin.Enabled)
            {
                return "disabled";
            }

            if (player == null || player.IsTeleporting())
            {
                return "no player";
            }

            float now = Time.unscaledTime;
            if (now < _next)
            {
                return "too soon after the last restock";
            }

            _next = now + Cooldown;
            ModConfig settings = Plugin.Settings;
            InventoryGui gui = InventoryGui.instance;
            if (gui != null)
            {
                gui.SetupDragItem(null, null, 1);
            }

            Inventory inventory = player.GetInventory();
            List<Slot> slots = Slots(player, inventory, settings);
            if (slots.Count == 0)
            {
                return Report(player, settings, Translations.Get(Translations.RestockNoSlots, settings.RestockSlotKey.Value), "no slot to restock");
            }

            bool multiUserChest = ChestMods.MultiUserChestLoaded;
            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
            var names = new HashSet<string>();
            var targets = new List<RestockTarget>();
            foreach (Slot slot in slots)
            {
                names.Add(slot.Target.Name);
                targets.Add(slot.Target);
            }

            List<Container> usable = UsableContainers(player, settings, playerId, multiUserChest, names, out List<List<ItemDrop.ItemData>> contents, out List<RestockSource> sources);
            string radius = settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture);
            if (usable.Count == 0)
            {
                return Report(player, settings, Translations.Get(Translations.NoChests, radius), "no usable chest in range");
            }

            RestockPlan plan = RestockPlanner.Plan(targets, sources);
            Plugin.Debug($"Restock plan: {plan.Moves.Count} moves, {plan.Units} units into {plan.SlotsFilled} slots, {plan.AlreadyStocked} already stocked, {plan.NotFound} not found");
            if (plan.Moves.Count == 0)
            {
                return plan.NotFound == 0
                    ? Report(player, settings, Translations.Get(Translations.RestockStocked), "everything stocked")
                    : Report(player, settings, Translations.Get(Translations.RestockNotFound, radius), "no chest holds these items");
            }

            int moved = 0;
            var filled = new HashSet<int>();
            var prepared = new Dictionary<Container, bool>();
            var touched = new HashSet<Container>();
            bool remote = false;
            foreach (RestockMove move in plan.Moves)
            {
                Container container = usable[move.Source];
                ItemDrop.ItemData item = contents[move.Source][move.Stack];
                Inventory? chest = container != null ? container.GetInventory() : null;
                if (chest == null || !chest.ContainsItem(item))
                {
                    continue;
                }

                if (!prepared.TryGetValue(container!, out bool writable))
                {
                    writable = ContainerAccess.PrepareForWrite(container!, multiUserChest);
                    prepared[container!] = writable;
                    if (!writable)
                    {
                        Plugin.Log.LogWarning($"Could not take ownership of {ContainerAccess.NameOf(container!)}, skipping it");
                    }
                }

                if (!writable)
                {
                    continue;
                }

                Slot slot = slots[move.Target];
                remote |= multiUserChest && !container!.m_nview.IsOwner();
                int units = Take(chest, item, inventory, move.Amount, slot.Position);
                Plugin.Debug($"  {item.m_shared.m_name} x{move.Amount} from {ContainerAccess.NameOf(container!)} ({sources[move.Source].Distance.ToString("0.#", CultureInfo.InvariantCulture)} m) -> [{slot.Position.x},{slot.Position.y}]: {units} taken");
                if (units > 0)
                {
                    moved += units;
                    filled.Add(move.Target);
                    touched.Add(container!);
                }
            }

            if (remote)
            {
                _next = now + RemoteCooldown;
            }

            inventory.Changed();
            foreach (Container container in touched)
            {
                if (gui != null && container != null)
                {
                    gui.m_moveItemEffects.Create(container.transform.position, Quaternion.identity);
                }
            }

            string message = moved > 0 ? Translations.Get(Translations.Restocked, moved, filled.Count) : Translations.Get(Translations.RestockNotFound, radius);
            return Report(player, settings, message, $"{moved} units into {filled.Count} slots");
        }

        /// <summary>The marked slots, then the items in slot mods' slots that are not marked, in grid order.</summary>
        private static List<Slot> Slots(Player player, Inventory inventory, ModConfig settings)
        {
            RestockMarks marks = settings.BuildRestockMarks();
            RestockLevels levels = settings.BuildRestockLevels();
            var slots = new List<Slot>();
            Plugin.Debug($"Restock by {player.GetPlayerName()}: {marks.Describe()}, {levels.Describe()}");

            bool remembered = false;
            foreach (RestockMarks.Mark mark in marks.Marks)
            {
                int x = mark.Slot.X;
                int y = mark.Slot.Y;
                if (x >= inventory.GetWidth() || y >= inventory.GetHeight())
                {
                    Plugin.Debug($"  [{x},{y}] is outside the {inventory.GetWidth()}x{inventory.GetHeight()} inventory");
                    continue;
                }

                ItemDrop.ItemData? item = inventory.GetItemAt(x, y);
                RestockTarget target;
                if (item != null)
                {
                    if (item.m_shared.m_maxStackSize <= 1)
                    {
                        Plugin.Debug($"  [{x},{y}] {item.m_shared.m_name} does not stack");
                        continue;
                    }

                    remembered |= marks.Remember(x, y, item.m_shared.m_name);

                    target = Describe(slots.Count, item, mark.Percent);
                }
                else if (mark.Item.Length > 0)
                {
                    target = RestockTarget.Empty(slots.Count, mark.Item, mark.Percent);
                }
                else
                {
                    Plugin.Debug($"  [{x},{y}] is empty and has never held anything");
                    continue;
                }

                Plugin.Debug($"  [{x},{y}] {target.Name} x{target.Count}, kept at {target.Percent}%");
                slots.Add(new Slot(new Vector2i(x, y), target));
            }

            if (remembered)
            {
                settings.SaveRestockMarks(marks);
            }

            Func<ItemDrop.ItemData, bool>? inModSlot = SlotMods.GetClassifier();
            if (inModSlot == null)
            {
                return slots;
            }

            var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            items.Sort((a, b) => a.m_gridPos.y != b.m_gridPos.y ? a.m_gridPos.y.CompareTo(b.m_gridPos.y) : a.m_gridPos.x.CompareTo(b.m_gridPos.x));
            foreach (ItemDrop.ItemData item in items)
            {
                ItemDrop.ItemData.SharedData shared = item.m_shared;
                if (shared.m_maxStackSize <= 1 || marks.Find(item.m_gridPos.x, item.m_gridPos.y) != null || !inModSlot(item))
                {
                    continue;
                }

                bool feeds = shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f;
                RestockKind kind = RestockLevels.KindOf(shared.m_itemType.ToString(), feeds);
                int percent = levels.PercentFor(kind);
                Plugin.Debug($"  [{item.m_gridPos.x},{item.m_gridPos.y}] {shared.m_name} x{item.m_stack} in a mod slot, {kind}: {(percent > 0 ? $"kept at {percent}%" : "left alone")}");
                if (percent > 0)
                {
                    slots.Add(new Slot(item.m_gridPos, Describe(slots.Count, item, percent)));
                }
            }

            return slots;
        }

        private static RestockTarget Describe(int index, ItemDrop.ItemData item, int percent)
        {
            return new RestockTarget(index, item.m_shared.m_name, item.m_quality, item.m_worldLevel, item.m_stack, item.m_shared.m_maxStackSize, percent);
        }

        /// <summary>The chests the player may take from, with their items of the wanted <paramref name="names"/>.</summary>
        private static List<Container> UsableContainers(Player player, ModConfig settings, long playerId, bool multiUserChest, HashSet<string> names,
            out List<List<ItemDrop.ItemData>> contents, out List<RestockSource> sources)
        {
            var nearby = new List<Container>();
            ContainerRegistry.CollectNearby(player.transform.position, settings.Radius.Value, nearby);
            Plugin.Debug($"{nearby.Count} containers within {settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture)} m ({ContainerRegistry.Count} loaded), MultiUserChest {(multiUserChest ? "on" : "off")}");

            var usable = new List<Container>();
            contents = new List<List<ItemDrop.ItemData>>();
            sources = new List<RestockSource>();
            foreach (Container container in nearby)
            {
                float distance = Vector3.Distance(container.transform.position, player.transform.position);
                if (!ContainerAccess.CanStashInto(container, playerId, multiUserChest, out string reason))
                {
                    Plugin.Debug($"  skip {ContainerAccess.NameOf(container)} at {distance.ToString("0.#", CultureInfo.InvariantCulture)} m: {reason}");
                    continue;
                }

                var items = new List<ItemDrop.ItemData>(container.GetInventory().GetAllItems());
                var stacks = new List<RestockStack>();
                for (int i = 0; i < items.Count; i++)
                {
                    ItemDrop.ItemData item = items[i];
                    if (names.Contains(item.m_shared.m_name))
                    {
                        stacks.Add(new RestockStack(i, item.m_shared.m_name, item.m_quality, item.m_worldLevel, item.m_stack, item.m_shared.m_maxStackSize));
                    }
                }

                sources.Add(new RestockSource(usable.Count, distance, stacks));
                contents.Add(items);
                usable.Add(container);
            }

            return usable;
        }

        /// <summary>
        /// Moves up to <paramref name="units"/> of <paramref name="item"/> from the chest into the
        /// player's slot at <paramref name="cell"/>, the way dragging it there does, and returns
        /// how many were moved (or requested, when MultiUserChest answers later).
        /// </summary>
        private static int Take(Inventory chest, ItemDrop.ItemData item, Inventory inventory, int units, Vector2i cell)
        {
            int before = item.m_stack;
            units = Math.Min(units, before);
            if (units <= 0)
            {
                return 0;
            }

            bool accepted = inventory.MoveItemToThis(chest, item, units, cell.x, cell.y);
            if (accepted)
            {
                return units;
            }

            // A refused move can still have merged a part; count what left the stack.
            return chest.ContainsItem(item) ? Math.Max(0, before - item.m_stack) : before;
        }

        private static string Report(Player player, ModConfig settings, string message, string summary)
        {
            Plugin.Debug($"Restock result: {summary}");
            if (settings.ShowMessage.Value)
            {
                player.Message(MessageHud.MessageType.Center, message);
            }

            return summary;
        }
    }
}
