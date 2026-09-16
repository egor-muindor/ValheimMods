using HarmonyLib;
using Muindor.ServerConfig;
using OreFinder.Detection;

namespace OreFinder.Patches
{
    /// <summary>Registers the <c>orefinder</c> console command.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal static class Terminal_InitTerminal_Patch
    {
        private const string Command = "orefinder";

        private static readonly string Usage =
            $"{Command} [status|on|off|reset|reload|{string.Join("|", TargetGroups.SwitchWords)} [on|off]]";

        private static void Postfix()
        {
            new Terminal.ConsoleCommand(
                Command,
                $"{Usage} - control {MyPluginInfo.PLUGIN_NAME}: show the settings, turn it on or off, forget the veins already shown, " +
                "reload the config, or turn one group of targets on or off (no on/off = flip it)",
                Execute);
        }

        private static void Execute(Terminal.ConsoleEventArgs args)
        {
            Terminal terminal = args.Context;
            string action = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "status";
            Finder? finder = Plugin.Finder;

            if (TargetGroups.TryParse(action, out TargetGroup group))
            {
                SwitchGroup(terminal, finder, group, args.Args.Length > 2 ? args.Args[2].ToLowerInvariant() : null);
                return;
            }

            switch (action)
            {
                case "on":
                case "off":
                    if (finder != null)
                    {
                        terminal.AddString(finder.SetEnabled(action == "on")
                            ? $"{MyPluginInfo.PLUGIN_NAME}: {action}"
                            : $"{MyPluginInfo.PLUGIN_NAME}: unchanged, {Finder.LockedNotice}");
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
                    terminal.AddString($"Usage: {Usage}");
                    break;
            }
        }

        private static void SwitchGroup(Terminal terminal, Finder? finder, TargetGroup group, string? state)
        {
            SyncedEntry<bool>? entry = Plugin.Settings.SwitchFor(group);
            if (entry == null)
            {
                terminal.AddString($"Usage: {Usage}");
                return;
            }

            bool enabled;
            switch (state)
            {
                case null:
                case "":
                    enabled = !entry.Value;
                    break;
                case "on":
                    enabled = true;
                    break;
                case "off":
                    enabled = false;
                    break;
                default:
                    terminal.AddString($"Usage: {Usage}");
                    return;
            }

            if (entry.IsLocked)
            {
                terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: {TargetGroups.Label(group)} unchanged, {Finder.LockedNotice}");
                return;
            }

            if (finder != null)
            {
                finder.SetGroupEnabled(group, enabled);
            }
            else
            {
                entry.LocalValue = enabled;
            }

            terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: {TargetGroups.Label(group)} {(enabled ? "on" : "off")}");
        }
    }
}
