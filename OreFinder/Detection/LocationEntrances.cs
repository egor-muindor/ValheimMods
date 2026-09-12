using System.Collections.Generic;
using UnityEngine;

namespace OreFinder.Detection
{
    /// <summary>
    /// The entrance doors of a location. The game spawns a location in two parts: every
    /// <c>ZNetView</c> child on its own, and everything else as one plain object under the
    /// location's <c>LocationProxy</c> (<c>ZoneSystem.SpawnLocation</c> in client mode). The
    /// entrance <c>Teleport</c> triggers belong to the second part: they need no networking and
    /// keep a reference to the exit inside the interior, which only survives when both are
    /// cloned together. So the doors never appear in <c>ZNetScene.m_instances</c> and have no
    /// ZDO of their own; they are found through the proxy, which is a net object.
    /// </summary>
    public static class LocationEntrances
    {
        private static readonly List<Teleport> Buffer = new List<Teleport>();

        /// <summary>
        /// The proxy has spawned its location (the clone is parented under the proxy). Until
        /// then (the zone is still loading, or the spawn is delayed) there is nothing to find.
        /// </summary>
        public static bool IsSpawned(LocationProxy proxy)
        {
            return proxy.m_instance != null;
        }

        /// <summary>Adds the location's doors, the teleports outside the interior, to <paramref name="results"/>.</summary>
        public static void Collect(LocationProxy proxy, List<Teleport> results)
        {
            proxy.GetComponentsInChildren(false, Buffer);
            foreach (Teleport teleport in Buffer)
            {
                if (TargetCatalog.IsEntrance(teleport))
                {
                    results.Add(teleport);
                }
            }

            Buffer.Clear();
        }

        /// <summary>The prefab name of the location the proxy stands for (Crypt2, TrollCave02, ...), or its hash.</summary>
        public static string LocationName(ZNetView proxyView)
        {
            int hash = proxyView.GetZDO().GetInt(ZDOVars.s_location);
            ZoneSystem zoneSystem = ZoneSystem.instance;
            ZoneSystem.ZoneLocation? location = zoneSystem != null ? zoneSystem.GetLocation(hash) : null;
            return location != null && !string.IsNullOrEmpty(location.m_prefabName) ? location.m_prefabName : hash.ToString();
        }
    }
}
