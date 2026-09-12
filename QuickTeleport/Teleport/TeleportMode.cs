namespace QuickTeleport.Teleport
{
    /// <summary>How the teleport wait is shortened.</summary>
    public enum TeleportMode
    {
        /// <summary>
        /// No fixed waits: the teleport ends as soon as the screen is black and the
        /// destination is loaded (plus the settle wait).
        /// </summary>
        Auto,

        /// <summary>Every vanilla wait divided by the speed multiplier.</summary>
        Multiplier,
    }
}
