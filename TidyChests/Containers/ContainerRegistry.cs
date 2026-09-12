using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TidyChests.Containers
{
    /// <summary>
    /// Every loaded container (chests, carts, ship cargo, ...) as it wakes up, so a stash or a
    /// search only has to walk this list instead of the whole scene. Objects the game destroys
    /// without the destroyed callback (zone unloads) are dropped when they are next seen.
    /// </summary>
    internal static class ContainerRegistry
    {
        private static readonly List<Container> All = new List<Container>();

        public static int Count => All.Count;

        public static void Add(Container container)
        {
            if (container != null && !All.Contains(container))
            {
                All.Add(container);
            }
        }

        public static void Remove(Container container)
        {
            All.Remove(container);
        }

        /// <summary>Adds the live containers within <paramref name="radius"/> of <paramref name="center"/> to <paramref name="result"/>, nearest first.</summary>
        public static void CollectNearby(Vector3 center, float radius, List<Container> result)
        {
            float radiusSquared = radius * radius;
            var distances = new Dictionary<Container, float>();
            for (int i = All.Count - 1; i >= 0; i--)
            {
                Container container = All[i];
                if (container == null)
                {
                    All.RemoveAt(i);
                    continue;
                }

                float distanceSquared = (container.transform.position - center).sqrMagnitude;
                if (distanceSquared <= radiusSquared)
                {
                    result.Add(container);
                    distances[container] = distanceSquared;
                }
            }

            result.Sort((a, b) => distances[a].CompareTo(distances[b]));
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    internal static class Container_Awake_Patch
    {
        private static void Postfix(Container __instance)
        {
            // A container without a ZDO (prefab preview, placement ghost) never gets an inventory.
            if (__instance.GetInventory() != null)
            {
                ContainerRegistry.Add(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
    internal static class Container_OnDestroyed_Patch
    {
        private static void Postfix(Container __instance)
        {
            ContainerRegistry.Remove(__instance);
        }
    }
}
