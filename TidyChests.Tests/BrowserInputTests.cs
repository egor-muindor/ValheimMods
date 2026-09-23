using TidyChests.Patches;
using Xunit;

namespace TidyChests.Tests
{
    public class BrowserInputTests
    {
        [Fact]
        public void FocusedSearchBlocksTheMapAction()
        {
            Assert.True(BrowserInput.ShouldBlockAction(BrowserInput.MapAction, searchFocused: true));
        }

        [Fact]
        public void UnfocusedSearchDoesNotBlockTheMapAction()
        {
            Assert.False(BrowserInput.ShouldBlockAction(BrowserInput.MapAction, searchFocused: false));
        }

        [Fact]
        public void FocusedSearchDoesNotBlockOtherActions()
        {
            Assert.False(BrowserInput.ShouldBlockAction("Use", searchFocused: true));
        }
    }
}
