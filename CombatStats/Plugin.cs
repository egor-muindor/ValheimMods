using BepInEx;
using BepInEx.Logging;
using CombatStats.Collect;
using CombatStats.Net;
using CombatStats.Ui;
using HarmonyLib;
using Muindor.ServerConfig;
using UnityEngine;
using UnityEngine.Rendering;

namespace CombatStats
{
    /// <summary>
    /// Plugin entry point. Binds the configuration, starts the collector, the sharing channel and
    /// the two windows, and applies the Harmony patches that feed the meter.
    ///
    /// Client-side: nothing here changes how the game plays, and every packet it sends is named
    /// after the plugin, so a player without the mod is unaffected and the game's version check
    /// never sees it.
    ///
    /// It is optional on the server all the same. Installed there with <c>ConfigPriority</c> on it
    /// decides whether its players exchange combat data at all; the windows and the keys stay
    /// each player's own.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? _harmony;

        public static ManualLogSource Log { get; private set; } = null!;

        public static ModConfig Settings { get; private set; } = null!;

        /// <summary>Everything recorded this session.</summary>
        internal static DamageCollector Collector { get; private set; } = null!;

        /// <summary>The window that shows the fight as it happens.</summary>
        internal static CompactMeter? Compact { get; private set; }

        /// <summary>The window with the breakdown.</summary>
        internal static DetailWindow? Detail { get; private set; }

        /// <summary>True when the plugin has loaded and the <c>Enabled</c> setting is on.</summary>
        public static bool Enabled => Settings != null && Settings.Enabled.Value;

        /// <summary>
        /// True on a machine with no screen: a dedicated server. It has no player of its own, so
        /// it records nothing, shows nothing and answers no greeting - otherwise the clients would
        /// take it for a peer that reports what it sees and stop estimating hits on the creatures
        /// it happens to own, while its own numbers went nowhere.
        ///
        /// <c>ZNet.IsDedicated()</c> cannot be used for this: in Valheim 1.0 it returns a constant
        /// false.
        /// </summary>
        public static bool Headless { get; private set; }

        /// <summary>Seconds the rings were built with, which a window can never exceed.</summary>
        public static int HistorySeconds { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);
            ConfigChannel.Activate(Settings.Sync, Log);

            Headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            HistorySeconds = Settings.HistorySeconds;
            Collector = new DamageCollector(HistorySeconds);

            if (!Headless)
            {
                gameObject.AddComponent<DamageChannel>();
                Compact = gameObject.AddComponent<CompactMeter>();
                Detail = gameObject.AddComponent<DetailWindow>();
            }

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded: {Settings.Describe()}");
            if (Headless)
            {
                Log.LogInfo("No screen on this machine: the meter itself stays off and only the configuration channel runs.");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        /// <summary>Logs at Info level when the <c>IsDebug</c> setting is on.</summary>
        public static void Debug(string message)
        {
            if (Settings != null && Settings.IsDebug.Value)
            {
                Log.LogInfo(message);
            }
        }
    }
}
