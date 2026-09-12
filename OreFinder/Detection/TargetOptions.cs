using System.Collections.Generic;

namespace OreFinder.Detection
{
    /// <summary>Which groups of targets to look for, mirrored from the config.</summary>
    public sealed class TargetOptions
    {
        /// <summary>Ore veins and scrap piles. Off = only the other groups are found.</summary>
        public bool Ores { get; set; } = true;

        public bool Dungeons { get; set; } = true;

        public bool Roots { get; set; } = true;

        /// <summary>Monster spawners: nests, bone and body piles, and the spawn points that respawn.</summary>
        public bool Spawners { get; set; } = true;

        public NameList Pickables { get; set; } = NameList.Parse(string.Empty);

        public NameList Trees { get; set; } = NameList.Parse(string.Empty);

        public string Describe()
        {
            var parts = new List<string>();
            if (Ores)
            {
                parts.Add("ores");
            }

            if (Dungeons)
            {
                parts.Add("dungeon entrances");
            }

            if (Roots)
            {
                parts.Add("roots");
            }

            if (Spawners)
            {
                parts.Add("spawners");
            }

            if (!Pickables.IsEmpty)
            {
                parts.Add($"pickables ({Pickables.Describe()})");
            }

            if (!Trees.IsEmpty)
            {
                parts.Add($"trees ({Trees.Describe()})");
            }

            return parts.Count == 0 ? "none" : string.Join(", ", parts);
        }
    }
}
