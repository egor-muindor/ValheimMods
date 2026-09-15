using System;
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

        // Reused between calls: the knowledge scan and the browser collect on a timer, and a
        // dictionary plus a closure per call would be garbage for the sake of one sort.
        private static readonly List<Nearby> Buffer = new List<Nearby>();

        private static readonly Comparison<Nearby> ByDistance =
            (a, b) => a.DistanceSquared.CompareTo(b.DistanceSquared);

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
            Buffer.Clear();
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
                    Buffer.Add(new Nearby(container, distanceSquared));
                }
            }

            Buffer.Sort(ByDistance);
            foreach (Nearby nearby in Buffer)
            {
                result.Add(nearby.Container);
            }

            // Nothing outside a call should keep containers alive.
            Buffer.Clear();
        }

        private readonly struct Nearby
        {
            public Nearby(Container container, float distanceSquared)
            {
                Container = container;
                DistanceSquared = distanceSquared;
            }

            public Container Container { get; }

            public float DistanceSquared { get; }
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
