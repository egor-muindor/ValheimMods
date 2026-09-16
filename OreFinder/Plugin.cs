using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Muindor.ServerConfig;

namespace OreFinder
{
    /// <summary>
    /// Plugin entry point. Binds the configuration, starts the <see cref="Finder"/> component
    /// and applies the Harmony patch that registers the console command.
    ///
    /// Client-side: the mod only looks at the objects the game has already loaded around the
    /// local player and draws on the local HUD. Nothing is needed on the server.
    ///
    /// It is optional on the server all the same. Installed there with <c>ConfigPriority</c> on it
    /// decides what the players who also have it look for and what the map pins are called; players
    /// without it are unaffected, and the mod keeps working on servers that do not have it.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? _harmony;

        public static ManualLogSource Log { get; private set; } = null!;

        public static ModConfig Settings { get; private set; } = null!;

        /// <summary>The running finder, once the plugin has loaded.</summary>
        public static Finder? Finder { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);
            ConfigChannel.Activate(Settings.Sync, Log);
            Finder = gameObject.AddComponent<Finder>();

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded: {Settings.Describe()}");
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
