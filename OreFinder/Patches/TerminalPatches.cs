using HarmonyLib;

namespace OreFinder.Patches
{
    /// <summary>Registers the <c>orefinder</c> console command.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal static class Terminal_InitTerminal_Patch
    {
        private const string Command = "orefinder";

        private static void Postfix()
        {
            new Terminal.ConsoleCommand(
                Command,
                $"{Command} [status|on|off|reset|reload] - control {MyPluginInfo.PLUGIN_NAME}: show the settings, turn it on or off, forget the veins already shown, or reload the config",
                Execute);
        }

        private static void Execute(Terminal.ConsoleEventArgs args)
        {
            Terminal terminal = args.Context;
            string action = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "status";
            Finder? finder = Plugin.Finder;

            switch (action)
            {
                case "on":
                case "off":
                    if (finder != null)
                    {
                        finder.SetEnabled(action == "on");
                        terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: {action}");
                    }

                    break;

                case "reset":
                    int forgotten = finder != null ? finder.Reset() : 0;
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: forgot {forgotten} veins, they will be highlighted again");
                    break;

                case "reload":
                    Plugin.Settings.Reload();
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: config reloaded, {Plugin.Settings.Describe()}");
                    break;

                case "status":
                    string state = finder != null ? $", {finder.HighlightCount} highlighted now, {finder.SeenCount} found in this world" : string.Empty;
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}: {Plugin.Settings.Describe()}{state}");
                    break;

                default:
                    terminal.AddString($"Usage: {Command} [status|on|off|reset|reload]");
                    break;
            }
        }
    }
}
