namespace OreFinder.Map
{
    /// <summary>The five map pin icons a player can place, by their look.</summary>
    public enum PinIcon
    {
        Fire,
        House,
        Hammer,
        Dot,
        Portal,
    }

    public static class PinIcons
    {
        public static Minimap.PinType ToPinType(PinIcon icon)
        {
            switch (icon)
            {
                case PinIcon.Fire:
                    return Minimap.PinType.Icon0;
                case PinIcon.House:
                    return Minimap.PinType.Icon1;
                case PinIcon.Hammer:
                    return Minimap.PinType.Icon2;
                case PinIcon.Portal:
                    return Minimap.PinType.Icon4;
                default:
                    return Minimap.PinType.Icon3;
            }
        }
    }
}
