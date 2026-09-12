using BepInEx.Bootstrap;

namespace TidyChests.Compat
{
    /// <summary>
    /// Mods that change how chests are shared between players. With MultiUserChest every
    /// inventory call on a chest the local player does not own is turned into a request to
    /// the chest's owner, so this plugin must neither claim ownership nor refuse chests that
    /// are in use; the plain vanilla calls are exactly what it expects.
    /// </summary>
    internal static class ChestMods
    {
        public const string MultiUserChestGuid = "com.maxsch.valheim.MultiUserChest";

        public static bool MultiUserChestLoaded => Chainloader.PluginInfos.ContainsKey(MultiUserChestGuid);

        public static void Report()
        {
            if (MultiUserChestLoaded)
            {
                string version = Chainloader.PluginInfos[MultiUserChestGuid].Metadata.Version.ToString();
                Plugin.Log.LogInfo($"MultiUserChest {version} found: chests in use by other players are stashed into through its requests");
            }
        }
    }
}
