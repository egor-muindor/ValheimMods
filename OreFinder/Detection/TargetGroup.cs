namespace OreFinder.Detection
{
    /// <summary>What kind of thing a found target is. Decides the search radius, the colour and the map pin.</summary>
    public enum TargetGroup
    {
        /// <summary>A vein or scrap pile, by its drops.</summary>
        Ore,

        /// <summary>The entrance of a crypt, cave or mine.</summary>
        Dungeon,

        /// <summary>An ancient root for the sap extractor.</summary>
        Root,

        /// <summary>A pickable item from the <c>Pickables</c> list.</summary>
        Pickable,

        /// <summary>A tree from the <c>Trees</c> list, by the wood it drops.</summary>
        Tree,

        /// <summary>A monster spawner: a nest or pile, or a spawn point that respawns its creature.</summary>
        Spawner,
    }

    /// <summary>The groups that have an on/off switch, by the word used for them in the console command.</summary>
    public static class TargetGroups
    {
        /// <summary>The console words, in the order the usage text lists them.</summary>
        public static readonly string[] SwitchWords = { "ores", "dungeons", "roots", "spawners" };

        /// <summary>Parses a console word (case-insensitive) into a switchable group.</summary>
        public static bool TryParse(string? word, out TargetGroup group)
        {
            switch ((word ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "ores":
                case "ore":
                    group = TargetGroup.Ore;
                    return true;
                case "dungeons":
                case "dungeon":
                    group = TargetGroup.Dungeon;
                    return true;
                case "roots":
                case "root":
                    group = TargetGroup.Root;
                    return true;
                case "spawners":
                case "spawner":
                    group = TargetGroup.Spawner;
                    return true;
                default:
                    group = TargetGroup.Ore;
                    return false;
            }
        }

        /// <summary>The name of a group in messages: "ores", "dungeon entrances", ...</summary>
        public static string Label(TargetGroup group)
        {
            switch (group)
            {
                case TargetGroup.Ore:
                    return "ores";
                case TargetGroup.Dungeon:
                    return "dungeon entrances";
                case TargetGroup.Root:
                    return "roots";
                case TargetGroup.Spawner:
                    return "spawners";
                case TargetGroup.Pickable:
                    return "pickables";
                default:
                    return "trees";
            }
        }
    }
}
