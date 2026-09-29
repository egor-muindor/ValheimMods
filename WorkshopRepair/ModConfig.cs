using System.Globalization;
using BepInEx.Configuration;
using Muindor.ServerConfig;

namespace WorkshopRepair
{
    /// <summary>
    /// Typed access to <c>muindor.WorkshopRepair.cfg</c>.
    ///
    /// Every rule of play is bound through <see cref="Sync"/>, so a server that also runs
    /// WorkshopRepair with <c>ConfigPriority</c> on decides how repairs work for all its players.
    /// Only <c>IsDebug</c> stays each player's own.
    /// </summary>
    public sealed class ModConfig
    {
        public ModConfig(ConfigFile config)
        {
            Sync = new ConfigSync(config, MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, Plugin.Log);

            Enabled = Sync.Bind("General", "Enabled", true,
                "Turn the mod on. With this off the repair button works exactly as in vanilla.");
            IsDebug = config.Bind("General", "IsDebug", false,
                "Log every repaired item and the station that repaired it.");

            RepairAllAtOnce = Sync.Bind("Repair", "RepairAllAtOnce", true,
                "One click on the repair button repairs every item that can be repaired here. Off: one item per click, as in vanilla.");
            UseNearbyStations = Sync.Bind("Repair", "UseNearbyStations", true,
                "A crafting station also repairs the items that belong to another station standing nearby: at the workbench, a forge within reach repairs the bronze and iron gear too. " +
                "That station must be usable as if you walked up to it - upgraded enough, under a roof, with its fire lit.");
            NearbyRadius = Sync.Bind("Repair", "NearbyRadius", 30f,
                new ConfigDescription("How far from you, in metres, another station may stand and still repair your items.",
                    new AcceptableValueRange<float>(2f, 100f)));
        }

        /// <summary>The settings a server may decide, and where the current ones come from.</summary>
        public ConfigSync Sync { get; }

        public SyncedEntry<bool> Enabled { get; }

        public ConfigEntry<bool> IsDebug { get; }

        public SyncedEntry<bool> RepairAllAtOnce { get; }

        public SyncedEntry<bool> UseNearbyStations { get; }

        public SyncedEntry<float> NearbyRadius { get; }

        /// <summary>One line with the active settings, for the log.</summary>
        public string Describe()
        {
            string nearby = UseNearbyStations.Value
                ? $"stations within {NearbyRadius.Value.ToString("0.#", CultureInfo.InvariantCulture)} m help"
                : "only the station in use repairs";
            return $"{(Enabled.Value ? "enabled" : "disabled")}, " +
                   $"{(RepairAllAtOnce.Value ? "all items per click" : "one item per click")}, {nearby}, {Sync.Describe()}";
        }
    }
}
