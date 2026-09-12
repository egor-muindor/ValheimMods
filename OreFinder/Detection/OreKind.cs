using UnityEngine;

namespace OreFinder.Detection
{
    /// <summary>What the finder shows for one kind of ore object.</summary>
    public sealed class OreKind
    {
        public OreKind(string oreItem, string displayName, Color color)
        {
            OreItem = oreItem;
            DisplayName = displayName;
            Color = color;
        }

        /// <summary>The drop that made the object count as ore, e.g. <c>CopperOre</c>.</summary>
        public string OreItem { get; }

        /// <summary>Localised name shown on the marker, e.g. "Copper deposit".</summary>
        public string DisplayName { get; }

        /// <summary>Colour of the marker, beam and light.</summary>
        public Color Color { get; }

        /// <summary>Picks a colour by the ore name; unknown ores are gold.</summary>
        public static Color ColorFor(string oreItem)
        {
            string name = oreItem.ToLowerInvariant();
            if (name.Contains("flametal"))
            {
                return new Color(1f, 0.3f, 0.1f);
            }

            if (name.Contains("copper"))
            {
                return new Color(1f, 0.55f, 0.2f);
            }

            if (name.Contains("silver"))
            {
                return new Color(0.9f, 0.95f, 1f);
            }

            if (name.Contains("tin"))
            {
                return new Color(0.7f, 0.85f, 1f);
            }

            if (name.Contains("iron"))
            {
                return new Color(0.9f, 0.4f, 0.15f);
            }

            return new Color(1f, 0.85f, 0.3f);
        }
    }
}
