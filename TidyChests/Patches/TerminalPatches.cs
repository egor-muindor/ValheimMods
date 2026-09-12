using HarmonyLib;
using TidyChests.Stash;

namespace TidyChests.Patches
{
    /// <summary>Registers the <c>tidychests</c> console command.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal static class Terminal_InitTerminal_Patch
    {
        private const string Command = "tidychests";

        private static void Postfix()
        {
            new Terminal.ConsoleCommand(
                Command,
                $"{Command} [stash|reload|status] - stash items into nearby chests, reload the {MyPluginInfo.PLUGIN_NAME} config or show the active settings",
                Execute);
        }

        private static void Execute(Terminal.ConsoleEventArgs args)
        {
            Terminal terminal = args.Context;
            string action = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "status";

            switch (action)
            {
                case "stash":
                    if (Player.m_localPlayer == null)
                    {
                        terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: no player in the world");
                        break;
                    }

                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: {Stasher.Stash(Player.m_localPlayer)}");
                    break;

                case "reload":
                    Plugin.Settings.Reload();
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: config reloaded, {Plugin.Settings.Describe()}");
                    break;

                case "status":
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}: {Plugin.Settings.Describe()}; {Plugin.Finder?.HighlightCount ?? 0} chests highlighted");
                    break;

                default:
                    terminal.AddString($"Usage: {Command} [stash|reload|status]");
                    break;
            }
        }
    }
}
