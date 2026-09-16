namespace CombatStats.Model
{
    /// <summary>What the compact window does when nothing is happening.</summary>
    public enum MeterMode
    {
        /// <summary>Shown while a fight is on, faded out once it goes quiet.</summary>
        Auto = 0,

        /// <summary>Always on screen, even with an empty window.</summary>
        Always = 1,

        /// <summary>Never shown. Events are still recorded for the detail window.</summary>
        Off = 2,
    }

    /// <summary>The corner or edge of the screen a window is pinned to.</summary>
    public enum ScreenAnchor
    {
        TopLeft = 0,
        TopRight = 1,
        MiddleLeft = 2,
        MiddleRight = 3,
        BottomLeft = 4,
        BottomRight = 5,
    }
}
