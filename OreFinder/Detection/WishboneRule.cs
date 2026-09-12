namespace OreFinder.Detection
{
    /// <summary>When hidden ores (the ones the game marks for the Wishbone) may be found.</summary>
    public enum WishboneRule
    {
        /// <summary>Only while the Wishbone is anywhere in the inventory.</summary>
        InInventory,

        /// <summary>Only while the Wishbone is equipped, like the game's own finder.</summary>
        Equipped,

        /// <summary>Always, the Wishbone is not needed.</summary>
        NotNeeded,
    }
}
