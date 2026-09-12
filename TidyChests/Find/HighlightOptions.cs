namespace TidyChests.Find
{
    /// <summary>How a found chest is shown; a snapshot of the config for one search.</summary>
    public readonly struct HighlightOptions
    {
        public HighlightOptions(float duration, bool light, bool glow)
        {
            Duration = duration;
            Light = light;
            Glow = glow;
        }

        public float Duration { get; }

        public bool Light { get; }

        public bool Glow { get; }
    }
}
