namespace QuickTeleport.Teleport
{
    /// <summary>Plain mirror of the config values a teleport needs. Snapshot taken when a teleport starts.</summary>
    public sealed class TeleportSettings
    {
        /// <summary>
        /// Shortest fade the HUD can handle: the fade speed is <c>Time.deltaTime / duration</c>,
        /// and a zero duration turns into NaN while the game is paused.
        /// </summary>
        public const float MinFadeDuration = 0.05f;

        public TeleportMode Mode { get; set; } = TeleportMode.Auto;

        public float SpeedMultiplier { get; set; } = 4f;

        public float FadeDuration { get; set; } = 1f;

        public bool WaitForAreaLoad { get; set; } = true;

        public bool WaitForObjects { get; set; } = true;

        public float SettleTime { get; set; } = 0.5f;
    }
}
