namespace TidyChests.Stash
{
    /// <summary>The raw config values the stash rules are built from.</summary>
    public sealed class StashRuleSettings
    {
        public bool IncludeHotbar { get; set; }

        /// <summary>Comma-separated <c>ItemDrop.ItemData.ItemType</c> names that may be stashed.</summary>
        public string ItemTypes { get; set; } = "";

        /// <summary>Comma-separated prefab or item names that are never stashed.</summary>
        public string Blacklist { get; set; } = "";
    }
}
