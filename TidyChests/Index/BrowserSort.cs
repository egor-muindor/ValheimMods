namespace TidyChests.Index
{
    /// <summary>
    /// How the chest list is ordered while its search box is empty. The Name and Count headers
    /// pick one of these; clicking the header that is already active flips its direction.
    /// </summary>
    public enum BrowserSort
    {
        /// <summary>Most of it first. The default: the list answers "what do we have a lot of".</summary>
        CountDescending,

        /// <summary>Least of it first, so what is running out is at the top.</summary>
        CountAscending,

        NameAscending,

        NameDescending,
    }
}
