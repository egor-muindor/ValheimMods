using System;
using System.Collections.Generic;
using System.Linq;

namespace TidyChests.Stash
{
    /// <summary>
    /// Terraria-style quick stack, planned on snapshots so it can be unit-tested and so the
    /// game side only has to execute a list of moves. Every item, in the order given, goes to
    /// the nearest container that already holds an item with the same name; existing stacks
    /// (same name, quality and world level) are topped up first, then empty slots are used;
    /// what is left continues to the next container. The snapshots are updated as the plan is
    /// built, so empty slots are shared correctly between items.
    /// </summary>
    public static class StashPlanner
    {
        public static StashPlan Plan(IReadOnlyList<ItemSnapshot> items, IReadOnlyList<ContainerSnapshot> containers)
        {
            List<ContainerSnapshot> byDistance = containers.OrderBy(container => container.Distance).ToList();
            var moves = new List<StashMove>();
            var itemsTouched = new HashSet<int>();
            var containersUsed = new HashSet<int>();
            int units = 0;

            foreach (ItemSnapshot item in items)
            {
                int remaining = item.Count;
                foreach (ContainerSnapshot container in byDistance)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    if (!container.Holds(item.Name))
                    {
                        continue;
                    }

                    int amount = Fill(container, item, remaining);
                    if (amount <= 0)
                    {
                        continue;
                    }

                    remaining -= amount;
                    units += amount;
                    itemsTouched.Add(item.Index);
                    containersUsed.Add(container.Index);
                    moves.Add(new StashMove(item.Index, container.Index, amount));
                }
            }

            return new StashPlan(moves, units, itemsTouched.Count, containersUsed.Count);
        }

        /// <summary>Puts up to <paramref name="remaining"/> units into the container's stacks and empty slots; returns how many fit.</summary>
        private static int Fill(ContainerSnapshot container, ItemSnapshot item, int remaining)
        {
            int amount = 0;
            foreach (StackSnapshot stack in container.Stacks)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (!stack.Accepts(item) || stack.FreeSpace <= 0)
                {
                    continue;
                }

                int units = Math.Min(stack.FreeSpace, remaining);
                stack.Count += units;
                remaining -= units;
                amount += units;
            }

            while (remaining > 0 && container.EmptySlots > 0)
            {
                int units = Math.Min(Math.Max(1, item.MaxStack), remaining);
                container.Stacks.Add(new StackSnapshot(item.Name, item.Quality, item.WorldLevel, units, item.MaxStack));
                container.EmptySlots--;
                remaining -= units;
                amount += units;
            }

            return amount;
        }
    }
}
