namespace DeathTweaks.Rules
{
    /// <summary>
    /// Raw item-handling settings as they appear in the config file. Parsed into a
    /// <see cref="DeathRules"/> instance before use.
    /// </summary>
    public sealed class DeathRuleSettings
    {
        public bool KeepAllItems { get; set; }

        public bool DestroyAllItems { get; set; }

        public bool KeepEquippedItems { get; set; }

        public bool KeepHotbarItems { get; set; }

        public bool KeepQuickSlotItems { get; set; }

        public bool KeepTeleportableItems { get; set; }

        /// <summary>Comma-separated <c>ItemType</c> names.</summary>
        public string KeepItemTypes { get; set; } = "";

        public string DropItemTypes { get; set; } = "";

        public string DestroyItemTypes { get; set; } = "";

        /// <summary>Comma-separated prefab names or <c>$item_*</c> tokens.</summary>
        public string KeepItemNames { get; set; } = "";

        public string DropItemNames { get; set; } = "";

        public string DestroyItemNames { get; set; } = "";
    }
}
