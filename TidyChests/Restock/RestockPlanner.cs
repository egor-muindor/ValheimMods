using System;
using System.Collections.Generic;
using System.Linq;

namespace TidyChests.Restock
{
    /// <summary>
    /// Plans a restock on snapshots, so it can be unit-tested and the game side only has to run a
    /// list of moves. Every slot, in the order given, is topped up to its level from the stacks of
    /// the same item (name, quality and world level) in the chests, nearest chest first. A single
    /// stack that covers the whole shortfall is preferred over several small ones: with
    /// MultiUserChest a slot waiting for another player's chest takes no second request until the
    /// first is answered. An empty slot takes the quality and world level of the first stack found.
    /// The snapshots are updated as the plan is built, so two slots of the same item share the chests.
    /// </summary>
    public static class RestockPlanner
    {
        public static RestockPlan Plan(IReadOnlyList<RestockTarget> targets, IReadOnlyList<RestockSource> sources)
        {
            List<RestockSource> byDistance = sources.OrderBy(source => source.Distance).ToList();
            var moves = new List<RestockMove>();
            int units = 0;
            int filled = 0;
            int stocked = 0;
            int notFound = 0;

            foreach (RestockTarget target in targets)
            {
                if (target.MaxStack > 0 && target.Count >= RestockLevels.Wanted(target.MaxStack, target.Percent))
                {
                    stocked++;
                    continue;
                }

                var candidates = new List<KeyValuePair<RestockSource, RestockStack>>();
                foreach (RestockSource source in byDistance)
                {
                    foreach (RestockStack stack in source.Stacks)
                    {
                        if (stack.Count > 0 && stack.MaxStack > 1 && Matches(target, stack))
                        {
                            candidates.Add(new KeyValuePair<RestockSource, RestockStack>(source, stack));
                        }
                    }
                }

                if (candidates.Count == 0)
                {
                    notFound++;
                    continue;
                }

                // An empty slot takes the first stack found; the rest must stack with it.
                RestockStack first = candidates[0].Value;
                candidates.RemoveAll(candidate => candidate.Value.Quality != first.Quality || candidate.Value.WorldLevel != first.WorldLevel);
                int maxStack = target.MaxStack > 0 ? target.MaxStack : first.MaxStack;
                int need = RestockLevels.Wanted(maxStack, target.Percent) - target.Count;
                if (need <= 0)
                {
                    stocked++;
                    continue;
                }

                int covering = candidates.FindIndex(candidate => candidate.Value.Count >= need);
                if (covering > 0)
                {
                    KeyValuePair<RestockSource, RestockStack> best = candidates[covering];
                    candidates.RemoveAt(covering);
                    candidates.Insert(0, best);
                }

                int taken = 0;
                foreach (KeyValuePair<RestockSource, RestockStack> candidate in candidates)
                {
                    if (taken >= need)
                    {
                        break;
                    }

                    int amount = Math.Min(candidate.Value.Count, need - taken);
                    candidate.Value.Count -= amount;
                    taken += amount;
                    moves.Add(new RestockMove(target.Index, candidate.Key.Index, candidate.Value.Index, amount));
                }

                units += taken;
                filled++;
            }

            return new RestockPlan(moves, units, filled, stocked, notFound);
        }

        private static bool Matches(RestockTarget target, RestockStack stack)
        {
            return stack.Name == target.Name
                   && (target.Quality <= 0 || stack.Quality == target.Quality)
                   && (target.WorldLevel < 0 || stack.WorldLevel == target.WorldLevel);
        }
    }
}
