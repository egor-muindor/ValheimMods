namespace DeathTweaks.Rules
{
    /// <summary>
    /// Everything the rule engine needs to know about one item, expressed without
    /// game types so the engine can be unit-tested outside the game.
    /// </summary>
    public readonly struct ItemFacts
    {
        public ItemFacts(
            string prefabName,
            string sharedName,
            string typeName,
            bool equipped,
            bool hotbar,
            bool quickSlot,
            bool teleportable,
            bool questItem)
        {
            PrefabName = prefabName ?? "";
            SharedName = sharedName ?? "";
            TypeName = typeName ?? "";
            Equipped = equipped;
            Hotbar = hotbar;
            QuickSlot = quickSlot;
            Teleportable = teleportable;
            QuestItem = questItem;
        }

        /// <summary>Prefab name, for example <c>Iron</c>. Empty when the item has no drop prefab.</summary>
        public string PrefabName { get; }

        /// <summary>Localisation token of the item name, for example <c>$item_iron</c>.</summary>
        public string SharedName { get; }

        /// <summary>Name of the <c>ItemDrop.ItemData.ItemType</c> enum member.</summary>
        public string TypeName { get; }

        public bool Equipped { get; }

        /// <summary>True when the item sits in the first inventory row.</summary>
        public bool Hotbar { get; }

        /// <summary>True when the item sits in an EquipmentAndQuickSlots quick slot.</summary>
        public bool QuickSlot { get; }

        public bool Teleportable { get; }

        public bool QuestItem { get; }

        /// <summary>Short human-readable name for logs.</summary>
        public string DisplayName => PrefabName.Length > 0 ? PrefabName : SharedName;
    }
}
