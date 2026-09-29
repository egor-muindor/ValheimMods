using System.Collections.Generic;
using UnityEngine;

namespace WorkshopRepair.Repair
{
    /// <summary>
    /// Finds a crafting station near the player that can repair an item the station the player
    /// is using cannot.
    ///
    /// A station counts when it can repair at all, is within <c>NearbyRadius</c> of the player,
    /// is the right kind and level for the item's recipe, and is usable right now - a forge
    /// without its roof or a station that needs a fire repairs nothing, just as when the player
    /// walks up to it.
    /// </summary>
    internal static class NearbyStations
    {
        /// <summary>Whether each station is usable, remembered for the frame it was asked in.</summary>
        private static readonly Dictionary<CraftingStation, bool> UsableThisFrame = new Dictionary<CraftingStation, bool>();

        private static int _usableFrame = -1;

        /// <summary>
        /// The closest station, other than <paramref name="current"/>, that repairs
        /// <paramref name="recipe"/> for <paramref name="player"/>; null when there is none.
        /// </summary>
        public static CraftingStation? Find(Player player, CraftingStation current, Recipe recipe)
        {
            string? craftingStation = recipe.m_craftingStation != null ? recipe.m_craftingStation.m_name : null;
            string? repairStation = recipe.m_repairStation != null ? recipe.m_repairStation.m_name : null;
            if (craftingStation == null && repairStation == null)
            {
                return null;
            }

            Vector3 position = player.transform.position;
            float radius = Plugin.Settings.NearbyRadius.Value;
            CraftingStation? best = null;
            float bestDistance = float.MaxValue;

            foreach (CraftingStation station in CraftingStation.m_allStations)
            {
                if (station == null || station == current || !station.m_canRepair)
                {
                    continue;
                }

                // The cheap tests first: this runs every frame the crafting panel is open.
                float distance = Vector3.Distance(position, station.transform.position);
                if (distance > radius || distance >= bestDistance)
                {
                    continue;
                }

                if (!RepairRules.StationRepairs(station.m_name, station.GetLevel(), craftingStation, repairStation, recipe.m_minStationLevel))
                {
                    continue;
                }

                if (!IsUsable(station, player))
                {
                    continue;
                }

                best = station;
                bestDistance = distance;
            }

            return best;
        }

        /// <summary>
        /// <c>CheckUsable</c> casts rays for the roof, so it is asked once per station per frame
        /// however many items want to know.
        /// </summary>
        private static bool IsUsable(CraftingStation station, Player player)
        {
            if (_usableFrame != Time.frameCount)
            {
                _usableFrame = Time.frameCount;
                UsableThisFrame.Clear();
            }

            if (!UsableThisFrame.TryGetValue(station, out bool usable))
            {
                usable = station.CheckUsable(player, showMessage: false);
                UsableThisFrame[station] = usable;
            }

            return usable;
        }
    }
}
