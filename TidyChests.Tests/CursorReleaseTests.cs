using Muindor.Windows;
using Xunit;

namespace TidyChests.Tests
{
    /// <summary>
    /// The chest list pinned the cursor to the centre of the screen on Linux: vanilla locked the
    /// mouse every frame and the old postfix undid it after the warp. These pin the rule the
    /// prefix follows instead.
    /// </summary>
    public class CursorReleaseTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NoWindowLeavesVanillaAlone(bool cursorFree)
        {
            Assert.Equal(CursorRelease.Step.RunVanilla, CursorRelease.Decide(windowOpen: false, cursorFree));
        }

        [Fact]
        public void OpenWindowReleasesALockedCursor()
        {
            Assert.Equal(CursorRelease.Step.Release, CursorRelease.Decide(windowOpen: true, cursorFree: false));
        }

        [Fact]
        public void OpenWindowDoesNotTouchAFreeCursor()
        {
            Assert.Equal(CursorRelease.Step.SkipVanilla, CursorRelease.Decide(windowOpen: true, cursorFree: true));
        }
    }
}
