using UnityEngine;

namespace OreFinder.Map
{
    /// <summary>Adds the map pin for a found target, named after it and saved with the player's map.</summary>
    internal static class MapPins
    {
        /// <summary>
        /// Adds the pin unless any other pin lies within <paramref name="spacing"/> metres (0 = always add).
        /// Moving or momentary pins (players, shouts, pings, events) do not count. Returns true when a pin was added.
        /// </summary>
        public static bool TryAdd(Vector3 position, string name, float spacing, Minimap.PinType type)
        {
            Minimap map = Minimap.instance;
            if (map == null)
            {
                return false;
            }

            if (spacing > 0f)
            {
                foreach (Minimap.PinData pin in map.m_pins)
                {
                    if (!IsTransient(pin.m_type) && Utils.DistanceXZ(pin.m_pos, position) < spacing)
                    {
                        return false;
                    }
                }
            }

            map.AddPin(position, type, name, save: true, isChecked: false);
            return true;
        }

        private static bool IsTransient(Minimap.PinType type)
        {
            return type == Minimap.PinType.Player
                   || type == Minimap.PinType.Shout
                   || type == Minimap.PinType.Ping
                   || type == Minimap.PinType.RandomEvent
                   || type == Minimap.PinType.EventArea;
        }
    }
}
