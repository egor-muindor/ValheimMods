namespace OreFinder.Detection
{
    /// <summary>What kind of thing a found target is. Decides the search radius, the colour and the map pin.</summary>
    public enum TargetGroup
    {
        /// <summary>A vein or scrap pile, by its drops.</summary>
        Ore,

        /// <summary>The entrance of a crypt, cave or mine.</summary>
        Dungeon,

        /// <summary>An ancient root for the sap extractor.</summary>
        Root,

        /// <summary>A pickable item from the <c>Pickables</c> list.</summary>
        Pickable,

        /// <summary>A tree from the <c>Trees</c> list, by the wood it drops.</summary>
        Tree,
    }
}
