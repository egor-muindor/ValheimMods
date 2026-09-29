namespace WorkshopRepair.Repair
{
    /// <summary>
    /// The game's own answer to "can this station repair this item", taken out of
    /// <c>InventoryGui.CanRepair</c> so it can be asked of any station, not only the one the
    /// player is standing at. Plain strings and numbers, so it is testable without the game.
    /// </summary>
    public static class RepairRules
    {
        /// <summary>
        /// The highest station level the game ever asks for when repairing: an upgraded
        /// workbench counts as level 4 at most, whatever its extensions add up to.
        /// </summary>
        public const int MaxCountedLevel = 4;

        /// <summary>
        /// True when a station named <paramref name="stationName"/> at <paramref name="stationLevel"/>
        /// repairs an item whose recipe is made at <paramref name="craftingStation"/> and repaired at
        /// <paramref name="repairStation"/> (either may be null) from <paramref name="minStationLevel"/> on.
        /// </summary>
        public static bool StationRepairs(string stationName, int stationLevel, string? craftingStation, string? repairStation, int minStationLevel)
        {
            if (string.IsNullOrEmpty(stationName))
            {
                return false;
            }

            bool matches = (repairStation != null && repairStation == stationName) ||
                           (craftingStation != null && craftingStation == stationName);
            return matches && CountedLevel(stationLevel) >= minStationLevel;
        }

        /// <summary>The level a station counts as for repairs.</summary>
        public static int CountedLevel(int stationLevel)
        {
            return stationLevel < MaxCountedLevel ? stationLevel : MaxCountedLevel;
        }

        /// <summary>The line shown once a click has repaired <paramref name="count"/> items, when there is more than one.</summary>
        public static string SeveralRepairedMessage(int count)
        {
            return $"Repaired {count} items";
        }
    }
}
