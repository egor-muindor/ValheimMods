using HarmonyLib;

namespace DeathTweaks.Patches
{
    /// <summary>Registers the <c>deathtweaks</c> console command.</summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal static class Terminal_InitTerminal_Patch
    {
        private const string Command = "deathtweaks";

        private static void Postfix()
        {
            new Terminal.ConsoleCommand(
                Command,
                $"{Command} [reload|status] - reload the {MyPluginInfo.PLUGIN_NAME} config or show the active item rules",
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
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME}: config reloaded, {Plugin.Settings.Sync.Describe()}");
                    break;

                case "status":
                    terminal.AddString($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}: {(Plugin.Enabled ? "enabled" : "disabled")}, {Plugin.Settings.Sync.Describe()}");
                    terminal.AddString("Item rules: " + Plugin.Settings.BuildRules().Describe());
                    terminal.AddString($"Tombstone: {Plugin.Settings.UseTombStone.Value}, keep food: {Plugin.Settings.KeepFoodLevels.Value}, reduce skills: {Plugin.Settings.ReduceSkills.Value} (factor {Plugin.Settings.SkillReduceFactor.Value})");
                    break;

                default:
                    terminal.AddString($"Usage: {Command} [reload|status]");
                    break;
            }
        }
    }
}
