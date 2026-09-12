namespace QuickTeleport.Teleport
{
    /// <summary>What "the destination is loaded" means for a teleport.</summary>
    public enum AreaCheck
    {
        /// <summary>Do not wait for the destination at all.</summary>
        Skip,

        /// <summary>Wait for the zone terrain only.</summary>
        TerrainOnly,

        /// <summary>Wait for the terrain and for every known object in the destination zones (vanilla).</summary>
        Full,
    }
}
