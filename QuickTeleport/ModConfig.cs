using System.Globalization;
using BepInEx.Configuration;
using QuickTeleport.Teleport;

namespace QuickTeleport
{
    /// <summary>Typed access to <c>muindor.QuickTeleport.cfg</c>.</summary>
    public sealed class ModConfig
    {
        private readonly ConfigFile _config;

        public ModConfig(ConfigFile config)
        {
            _config = config;

            Enabled = config.Bind("General", "Enabled", true, "Enable this mod. Off = vanilla teleports.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log the timeline of every teleport (screen black, area loaded, settled, finished).");

            Mode = config.Bind("Teleport", "Mode", TeleportMode.Auto,
                "Auto: no fixed waits, the teleport ends as soon as the screen is black and the destination is loaded. " +
                "Multiplier: everything vanilla does (the fade, the 2 s move delay, the 8 s portal minimum, the 15 s floor timeout) divided by SpeedMultiplier.");
            SpeedMultiplier = config.Bind("Teleport", "SpeedMultiplier", 4f,
                new ConfigDescription("Multiplier mode only. 1 = vanilla timing, 4 = four times faster.",
                    new AcceptableValueRange<float>(1f, 100f)));
            FadeDuration = config.Bind("Teleport", "FadeDuration", TeleportSettings.DefaultFadeDuration,
                new ConfigDescription(
                    "Seconds for the screen to fade to black before the teleport and back afterwards (vanilla: 1, default: 0.1). " +
                    "The player is never moved before the screen is black, so this is the shortest possible teleport. In Multiplier mode it is divided by SpeedMultiplier too.",
                    new AcceptableValueRange<float>(TeleportSettings.MinFadeDuration, 5f)));

            WaitForAreaLoad = config.Bind("Loading", "WaitForAreaLoad", true,
                "Wait until the destination is loaded before ending the teleport (vanilla). " +
                "false: end it right after the fade, like the old QuickTeleport 'Skip Loading Area'; you may float or fall until the world appears.");
            WaitForObjects = config.Bind("Loading", "WaitForObjects", true,
                "Also wait for the objects of the destination (buildings, trees) to spawn, not only for the terrain (vanilla). " +
                "false: like the old QuickTeleport 'Skip Loading Objects'; you may end up under a building floor. Ignored when WaitForAreaLoad is false.");
            SettleTime = config.Bind("Loading", "SettleTime", 0.5f,
                new ConfigDescription(
                    "Auto mode with WaitForObjects only. After the destination is loaded, wait this many seconds without new objects arriving from the server before ending the teleport. " +
                    "Never waits past the vanilla minimum measured from the start of the teleport (8 s for portals, 2 s for dungeons). 0 disables.",
                    new AcceptableValueRange<float>(0f, 5f)));
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public ConfigEntry<TeleportMode> Mode { get; }

        public ConfigEntry<float> SpeedMultiplier { get; }

        public ConfigEntry<float> FadeDuration { get; }

        public ConfigEntry<bool> WaitForAreaLoad { get; }

        public ConfigEntry<bool> WaitForObjects { get; }

        public ConfigEntry<float> SettleTime { get; }

        /// <summary>Re-reads the config file from disk.</summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>Snapshot of the current values for one teleport.</summary>
        public TeleportSettings ToSettings()
        {
            return new TeleportSettings
            {
                Mode = Mode.Value,
                SpeedMultiplier = SpeedMultiplier.Value,
                FadeDuration = FadeDuration.Value,
                WaitForAreaLoad = WaitForAreaLoad.Value,
                WaitForObjects = WaitForObjects.Value,
                SettleTime = SettleTime.Value,
            };
        }

        /// <summary>One line with the active settings, for the log and the console command.</summary>
        public string Describe()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            string timing = Mode.Value == TeleportMode.Multiplier
                ? $"Multiplier x{SpeedMultiplier.Value.ToString("0.##", culture)}"
                : $"Auto (settle {SettleTime.Value.ToString("0.##", culture)} s)";
            string loading = !WaitForAreaLoad.Value ? "no wait for area load"
                : !WaitForObjects.Value ? "wait for terrain only"
                : "wait for terrain and objects";
            return $"{(Enabled.Value ? "enabled" : "disabled")}, {timing}, fade {FadeDuration.Value.ToString("0.##", culture)} s, {loading}";
        }
    }
}
