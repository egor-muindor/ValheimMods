namespace Muindor.Windows
{
    /// <summary>
    /// Decides what a <c>GameCamera.UpdateMouseCapture</c> prefix does while a window of the mod
    /// is open. Kept free of Unity so the rule is unit tested.
    ///
    /// Vanilla locks the cursor every frame unless one of its own screens is open, and a mod
    /// window is not on that list. Undoing the lock in a postfix works on Windows only because
    /// Unity there defers the warp to the end of the frame; on Linux the warp reaches the
    /// hardware pointer at once, and undoing it afterwards leaves the cursor pinned to the centre
    /// of the screen. So vanilla is skipped altogether while a window is open, and the lock state
    /// is written only when it actually differs, so the release happens once and not per frame.
    /// </summary>
    public static class CursorRelease
    {
        public enum Step
        {
            /// <summary>No window is open: let vanilla run untouched.</summary>
            RunVanilla,

            /// <summary>
            /// A window is open and the cursor is still locked: free it and skip vanilla.
            /// </summary>
            Release,

            /// <summary>
            /// A window is open and the cursor is already free: only skip vanilla.
            /// </summary>
            SkipVanilla,
        }

        public static Step Decide(bool windowOpen, bool cursorFree)
        {
            if (!windowOpen)
            {
                return Step.RunVanilla;
            }

            return cursorFree ? Step.SkipVanilla : Step.Release;
        }
    }
}
