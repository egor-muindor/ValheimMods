namespace DeathTweaks.Rules
{
    /// <summary>What happens to a single inventory item when the player dies.</summary>
    public enum ItemFate
    {
        /// <summary>The item stays in the player's inventory.</summary>
        Keep,

        /// <summary>The item goes to the tombstone, or on the ground when tombstones are disabled.</summary>
        Drop,

        /// <summary>The item is removed from the game.</summary>
        Destroy,
    }
}
