namespace DeathTweaks.Rules
{
    /// <summary>
    /// The vanilla death world modifiers (global keys). They are applied on top of the
    /// mod configuration so the mod never weakens a rule the server has set.
    /// </summary>
    public sealed class WorldDeathModifiers
    {
        public static readonly WorldDeathModifiers None = new WorldDeathModifiers(false, false, false, false);

        public WorldDeathModifiers(bool keepInventory, bool keepEquip, bool deleteItems, bool deleteUnequipped)
        {
            KeepInventory = keepInventory;
            KeepEquip = keepEquip;
            DeleteItems = deleteItems;
            DeleteUnequipped = deleteUnequipped;
        }

        /// <summary><c>DeathKeepInventory</c>: nothing leaves the inventory.</summary>
        public bool KeepInventory { get; }

        /// <summary><c>DeathKeepEquip</c>: equipped items stay with the player.</summary>
        public bool KeepEquip { get; }

        /// <summary><c>DeathDeleteItems</c>: items that would be dropped are deleted instead.</summary>
        public bool DeleteItems { get; }

        /// <summary><c>DeathDeleteUnequipped</c>: unequipped items that would be dropped are deleted instead.</summary>
        public bool DeleteUnequipped { get; }

        public bool Any => KeepInventory || KeepEquip || DeleteItems || DeleteUnequipped;

        public override string ToString()
        {
            if (!Any)
            {
                return "none";
            }

            var parts = new System.Collections.Generic.List<string>(4);
            if (KeepInventory) parts.Add("DeathKeepInventory");
            if (KeepEquip) parts.Add("DeathKeepEquip");
            if (DeleteItems) parts.Add("DeathDeleteItems");
            if (DeleteUnequipped) parts.Add("DeathDeleteUnequipped");
            return string.Join(", ", parts.ToArray());
        }
    }
}
