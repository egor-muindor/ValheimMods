using System;

namespace OreFinder.Hud
{
    /// <summary>Where to draw the marker of a world point, in GUI pixels (origin top-left, y down).</summary>
    public readonly struct ScreenMarker
    {
        public ScreenMarker(float x, float y, float angleDegrees, bool onScreen)
        {
            X = x;
            Y = y;
            AngleDegrees = angleDegrees;
            OnScreen = onScreen;
        }

        public float X { get; }

        public float Y { get; }

        /// <summary>Clockwise rotation of an up-pointing arrow so that it points at the target; 180 = down.</summary>
        public float AngleDegrees { get; }

        /// <summary>True when the point is in front of the camera and inside the screen.</summary>
        public bool OnScreen { get; }
    }

    /// <summary>Pure math behind the screen markers, kept free of Unity types so it can be unit tested.</summary>
    public static class ScreenMarkerMath
    {
        private const float Epsilon = 1e-4f;

        /// <summary>
        /// Computes the marker for a point given in viewport coordinates: <paramref name="vx"/>
        /// and <paramref name="vy"/> run 0..1 across the screen (y up), <paramref name="vz"/> is
        /// the depth and is negative behind the camera. On-screen points map straight to pixels
        /// with the arrow pointing down at them. Other points get an arrow on the screen edge,
        /// <paramref name="margin"/> pixels inside, pointing in their direction.
        /// </summary>
        public static ScreenMarker Compute(float vx, float vy, float vz, float width, float height, float margin)
        {
            bool finite = !IsBad(vx) && !IsBad(vy) && !IsBad(vz);
            bool inFront = vz > 0f;
            if (finite && inFront && vx >= 0f && vx <= 1f && vy >= 0f && vy <= 1f)
            {
                return new ScreenMarker(vx * width, (1f - vy) * height, 180f, true);
            }

            float centerX = width / 2f;
            float centerY = height / 2f;
            float dx = 0f;
            float dy = 1f;
            if (finite)
            {
                dx = vx * width - centerX;
                dy = (1f - vy) * height - centerY;
                if (!inFront)
                {
                    // Behind the camera the projection is mirrored through the centre.
                    dx = -dx;
                    dy = -dy;
                }

                if (Math.Abs(dx) < Epsilon && Math.Abs(dy) < Epsilon)
                {
                    dx = 0f;
                    dy = 1f;
                }
            }

            float halfWidth = Math.Max(1f, centerX - margin);
            float halfHeight = Math.Max(1f, centerY - margin);
            float t = float.MaxValue;
            if (Math.Abs(dx) > Epsilon)
            {
                t = Math.Min(t, halfWidth / Math.Abs(dx));
            }

            if (Math.Abs(dy) > Epsilon)
            {
                t = Math.Min(t, halfHeight / Math.Abs(dy));
            }

            float angle = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
            return new ScreenMarker(centerX + dx * t, centerY + dy * t, angle, false);
        }

        private static bool IsBad(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }
    }
}
