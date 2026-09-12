using System;
using BepInEx;
using BepInEx.Logging;
using DeathTweaks.Compat;
using HarmonyLib;

namespace DeathTweaks
{
    /// <summary>
    /// Plugin entry point. Binds the configuration and applies the Harmony patches.
    ///
    /// The vanilla death pipeline (<c>Player.OnDeath</c>) is never replaced. Each feature
    /// patches the smallest vanilla method that owns the behaviour, so game updates that
    /// change other parts of the pipeline are picked up automatically.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(EquipmentAndQuickSlotsCompat.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? _harmony;

        public static ManualLogSource Log { get; private set; } = null!;

        public static ModConfig Settings { get; private set; } = null!;

        /// <summary>True when the plugin has loaded and the <c>Enabled</c> setting is on.</summary>
        public static bool Enabled => Settings != null && Settings.Enabled.Value;

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded");
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

        /// <summary>Logs an unexpected exception from a patch. The patch then falls back to vanilla.</summary>
        public static void PatchFailed(string patch, Exception exception)
        {
            Log.LogError($"{patch} failed, falling back to vanilla behaviour: {exception}");
        }
    }
}
