using HarmonyLib;

namespace QuickTeleport.Patches
{
    /// <summary>Registers the <c>quickteleport</c> console command.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal static class Terminal_InitTerminal_Patch
    {
        private const string Command = "quickteleport";

        private static void Postfix()
        {
            new Terminal.ConsoleCommand(
                Command,
                $"{Command} [reload|status] - reload the {MyPluginInfo.PLUGIN_NAME} config or show the active settings",
                Execute);
        }

        private static void Execute(Terminal.ConsoleEventArgs args)
        {
            Terminal terminal = args.Context;
            string action = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "status";

            switch (action)
            {
                case "reload":
                    Plugin.Settings.Reload();
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: config reloaded, {Plugin.Settings.Describe()}");
                    break;

                case "status":
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}: {Plugin.Settings.Describe()}");
                    break;

                default:
                    terminal.AddString($"Usage: {Command} [reload|status]");
                    break;
            }
        }
    }
}
