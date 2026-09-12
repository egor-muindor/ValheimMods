namespace DeathTweaks
{
    /// <summary>
    /// Tracks whether the local player is currently inside <c>Player.OnDeath</c>, so patches
    /// on methods that are also called elsewhere (for example <c>Skills.Clear</c>) can limit
    /// themselves to the death pipeline.
    /// </summary>
    internal static class DeathContext
    {
        public static bool InOnDeath { get; private set; }

        public static void Enter() => InOnDeath = true;

        public static void Exit() => InOnDeath = false;
    }
}
