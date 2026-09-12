namespace OreFinder.Highlight
{
    /// <summary>Plain mirror of the highlight settings, taken when a vein is found.</summary>
    public readonly struct HighlightOptions
    {
        public HighlightOptions(float duration, bool beam, bool light, bool glow)
        {
            Duration = duration;
            Beam = beam;
            Light = light;
            Glow = glow;
        }

        public float Duration { get; }

        public bool Beam { get; }

        public bool Light { get; }

        public bool Glow { get; }
    }
}
