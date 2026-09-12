using TidyChests.Hud;
using Xunit;

namespace TidyChests.Tests
{
    public class ScreenMarkerMathTests
    {
        private const float Width = 1000f;

        private const float Height = 500f;

        private const float Margin = 40f;

        private static ScreenMarker Compute(float vx, float vy, float vz)
        {
            return ScreenMarkerMath.Compute(vx, vy, vz, Width, Height, Margin);
        }

        [Fact]
        public void PointInView_MapsToPixels_WithArrowPointingDown()
        {
            ScreenMarker marker = Compute(0.25f, 0.75f, 10f);

            Assert.True(marker.OnScreen);
            Assert.Equal(250f, marker.X, 3);
            Assert.Equal(125f, marker.Y, 3);
            Assert.Equal(180f, marker.AngleDegrees, 3);
        }

        [Fact]
        public void ViewportOrigin_IsBottomLeftOfTheScreen()
        {
            ScreenMarker marker = Compute(0f, 0f, 1f);

            Assert.True(marker.OnScreen);
            Assert.Equal(0f, marker.X, 3);
            Assert.Equal(Height, marker.Y, 3);
        }

        [Fact]
        public void PointToTheRight_SitsOnTheRightEdge_PointingRight()
        {
            ScreenMarker marker = Compute(1.5f, 0.5f, 5f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Width - Margin, marker.X, 3);
            Assert.Equal(Height / 2f, marker.Y, 3);
            Assert.Equal(90f, marker.AngleDegrees, 3);
        }

        [Fact]
        public void PointAbove_SitsOnTheTopEdge_PointingUp()
        {
            ScreenMarker marker = Compute(0.5f, 1.5f, 5f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Width / 2f, marker.X, 3);
            Assert.Equal(Margin, marker.Y, 3);
            Assert.Equal(0f, marker.AngleDegrees, 3);
        }

        [Fact]
        public void PointStraightBehind_SitsOnTheBottomEdge_PointingDown()
        {
            ScreenMarker marker = Compute(0.5f, 0.5f, -5f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Width / 2f, marker.X, 3);
            Assert.Equal(Height - Margin, marker.Y, 3);
            Assert.Equal(180f, marker.AngleDegrees, 3);
        }

        [Fact]
        public void PointBehind_IsMirroredThroughTheCentre()
        {
            // Projected to the left of the centre, but behind the camera: it is really on the right.
            ScreenMarker marker = Compute(0.2f, 0.5f, -5f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Width - Margin, marker.X, 3);
            Assert.Equal(90f, marker.AngleDegrees, 3);
        }

        [Fact]
        public void DiagonalPoint_UsesPixelAspectForTheAngle_AndStopsAtTheNearestEdge()
        {
            // 1000 px right and 250 px up from the centre.
            ScreenMarker marker = Compute(1.5f, 1.0f, 5f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Width - Margin, marker.X, 2);
            Assert.Equal(250f - 250f * 0.46f, marker.Y, 2);
            Assert.Equal(75.96f, marker.AngleDegrees, 1);
        }

        [Fact]
        public void PointInFrontButOffTheLeftEdge_PointsLeft()
        {
            ScreenMarker marker = Compute(-0.1f, 0.5f, 5f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Margin, marker.X, 3);
            Assert.Equal(-90f, marker.AngleDegrees, 3);
        }

        [Fact]
        public void InvalidProjection_FallsBackToTheBottomEdge()
        {
            ScreenMarker marker = Compute(float.NaN, float.NaN, 0f);

            Assert.False(marker.OnScreen);
            Assert.Equal(Width / 2f, marker.X, 3);
            Assert.Equal(Height - Margin, marker.Y, 3);
            Assert.Equal(180f, marker.AngleDegrees, 3);
        }
    }
}
