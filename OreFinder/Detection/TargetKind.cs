using UnityEngine;

namespace OreFinder.Detection
{
    /// <summary>What the finder shows for one kind of target object.</summary>
    public sealed class TargetKind
    {
        public TargetKind(TargetGroup group, string key, string displayName, Color color, bool hidden)
        {
            Group = group;
            Key = key;
            DisplayName = displayName;
            Color = color;
            Hidden = hidden;
        }

        public TargetGroup Group { get; }

        /// <summary>
        /// What identifies the target in the <c>Names</c> setting: the ore item (<c>CopperOre</c>),
        /// the dungeon's location key (<c>$location_forestcrypt</c>), the root's name key, the
        /// pickable item (<c>DragonEgg</c>), the wood item of a tree or the spawner's prefab
        /// (<c>Spawner_GreydwarfNest</c>).
        /// </summary>
        public string Key { get; }

        /// <summary>Name shown on the marker, the message and the map pin.</summary>
        public string DisplayName { get; }

        /// <summary>Colour of the marker, beam and light.</summary>
        public Color Color { get; }

        /// <summary>
        /// The game marks it for the Wishbone (a <c>Beacon</c> component): silver veins and the
        /// scrap piles with a beacon. Found only under the <c>WishboneNeeded</c> rule.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>Picks a colour by group and, for ores, by the ore name; unknown ores are gold.</summary>
        public static Color ColorFor(TargetGroup group, string key)
        {
            switch (group)
            {
                case TargetGroup.Dungeon:
                    return new Color(0.75f, 0.45f, 1f);
                case TargetGroup.Root:
                    return new Color(0.4f, 1f, 0.55f);
                case TargetGroup.Pickable:
                    return new Color(0.7f, 1f, 0.3f);
                case TargetGroup.Tree:
                    return new Color(0.3f, 0.9f, 0.4f);
                case TargetGroup.Spawner:
                    return new Color(1f, 0.3f, 0.35f);
            }

            string name = key.ToLowerInvariant();
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
