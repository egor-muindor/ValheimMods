namespace TidyChests.Patches
{
    /// <summary>Input actions the chest browser owns while its search field receives keyboard text.</summary>
    internal static class BrowserInput
    {
        internal const string MapAction = "Map";

        /// <summary>
        /// The map key is also a valid search character. Let the search field receive it instead
        /// of opening the map, which would close the browser and discard the query.
        /// </summary>
        public static bool ShouldBlockAction(string action, bool searchFocused)
        {
            return searchFocused && action == MapAction;
        }
    }
}
