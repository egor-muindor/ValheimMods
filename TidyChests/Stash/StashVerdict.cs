namespace TidyChests.Stash
{
    /// <summary>Why an item is, or is not, a candidate for stashing.</summary>
    public enum StashVerdict
    {
        Stash,
        NotStackable,
        QuestItem,
        Equipped,
        ModSlot,
        Hotbar,
        TypeExcluded,
        Blacklisted,
    }
}
