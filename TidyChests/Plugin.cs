using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Muindor.ServerConfig;
using TidyChests.Compat;
using TidyChests.Find;
using TidyChests.Index;
using TidyChests.Knowledge;
using TidyChests.Ui;

namespace TidyChests
{
    /// <summary>
    /// Plugin entry point. Binds the configuration, registers the translations, starts the
    /// <see cref="ChestFinder"/> component and applies the Harmony patches (container registry,
    /// inventory button, localization words, console command).
    ///
    /// Client-side: items are moved with the same inventory calls the vanilla "stack all"
    /// button uses, so the server needs nothing and other players need not have the mod.
    ///
    /// It is optional on the server all the same. Installed there with <c>ConfigPriority</c> on it
    /// decides the stash and scan settings for the players who also have it; players without it are
    /// unaffected, and the mod keeps working on servers that do not have it.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SlotMods.ExtraSlotsGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SlotMods.EquipmentAndQuickSlotsGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SlotMods.BetterArcheryGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ChestMods.MultiUserChestGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? _harmony;

        public static ManualLogSource Log { get; private set; } = null!;

        public static ModConfig Settings { get; private set; } = null!;

        /// <summary>The running finder, once the plugin has loaded.</summary>
        public static ChestFinder? Finder { get; private set; }

        /// <summary>The running chest list, once the plugin has loaded.</summary>
        public static ChestBrowser? Browser { get; private set; }

        /// <summary>The running knowledge scan, once the plugin has loaded.</summary>
        public static KnowledgeScanner? Scanner { get; private set; }

        /// <summary>What the chests in range hold; shared by the chest list and the knowledge scan.</summary>
        internal static ChestIndex Index { get; } = new ChestIndex();

        /// <summary>True when the plugin has loaded and the <c>Enabled</c> setting is on.</summary>
        public static bool Enabled => Settings != null && Settings.Enabled.Value;

        private void Awake()
        {
            Log = Logger;
            Settings = new ModConfig(Config);
            ConfigChannel.Activate(Settings.Sync, Log);
            Finder = gameObject.AddComponent<ChestFinder>();
            Browser = gameObject.AddComponent<ChestBrowser>();
            Scanner = gameObject.AddComponent<KnowledgeScanner>();

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Translations.ApplyIfLoaded();

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded: {Settings.Describe()}");
            SlotMods.Report();
            ChestMods.Report();
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
