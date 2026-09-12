namespace TidyChests.Stash
{
    /// <summary>
    /// Everything the stash rules need to know about one inventory item, expressed without
    /// game types so the rules can be unit-tested outside the game.
    /// </summary>
    public readonly struct ItemFacts
    {
        public ItemFacts(
            string prefabName,
            string sharedName,
            string typeName,
            int maxStack,
            bool equipped,
            bool hotbar,
            bool modSlot,
            bool questItem)
        {
            PrefabName = prefabName ?? "";
            SharedName = sharedName ?? "";
            TypeName = typeName ?? "";
            MaxStack = maxStack;
            Equipped = equipped;
            Hotbar = hotbar;
            ModSlot = modSlot;
            QuestItem = questItem;
        }

        /// <summary>Prefab name, for example <c>Wood</c>. Empty when the item has no drop prefab.</summary>
        public string PrefabName { get; }

        /// <summary>Localisation token of the item name, for example <c>$item_wood</c>.</summary>
        public string SharedName { get; }

        /// <summary>Name of the <c>ItemDrop.ItemData.ItemType</c> enum member.</summary>
        public string TypeName { get; }

        /// <summary>Largest stack of this item; 1 for things that do not stack.</summary>
        public int MaxStack { get; }

        public bool Equipped { get; }

        /// <summary>True when the item sits in the first inventory row.</summary>
        public bool Hotbar { get; }

        /// <summary>True when the item sits in a slot of a supported slot mod (quick, equipment, quiver, ...).</summary>
        public bool ModSlot { get; }

        public bool QuestItem { get; }

        /// <summary>Short human-readable name for logs.</summary>
        public string DisplayName => PrefabName.Length > 0 ? PrefabName : SharedName;
    }
}
