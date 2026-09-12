using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;

namespace DeathTweaks.Compat
{
    /// <summary>
    /// Bridges to the quick slot mods this plugin understands. Each mod exposes a public static
    /// API class whose quick slot getter returns the <c>ItemDrop.ItemData</c> instances currently
    /// sitting in quick slots; those are ordinary player inventory items, so only their identity
    /// is needed. Everything is resolved by reflection so the plugin loads without any of them.
    /// </summary>
    internal static class QuickSlotMods
    {
        internal sealed class Provider
        {
            private bool _resolved;
            private MethodInfo? _getItems;

            public Provider(string name, string pluginGuid, string assemblyName, string apiTypeName, string methodName)
            {
                Name = name;
                PluginGuid = pluginGuid;
                AssemblyName = assemblyName;
                ApiTypeName = apiTypeName;
                MethodName = methodName;
            }

            public string Name { get; }

            public string PluginGuid { get; }

            public string AssemblyName { get; }

            public string ApiTypeName { get; }

            public string MethodName { get; }

            public bool IsLoaded => Chainloader.PluginInfos.ContainsKey(PluginGuid);

            public string LoadedVersion =>
                Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) && info?.Metadata != null
                    ? info.Metadata.Version.ToString()
                    : "?";

            /// <summary>Items in quick slots, or null when the API cannot be reached.</summary>
            public HashSet<ItemDrop.ItemData>? GetItems()
            {
                MethodInfo? method = Resolve();
                if (method == null)
                {
                    return null;
                }

                try
                {
                    return method.Invoke(null, null) is List<ItemDrop.ItemData> items
                        ? new HashSet<ItemDrop.ItemData>(items)
                        : null;
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogWarning($"{Name} API call failed, quick slot items are treated as regular items: {exception.Message}");
                    return null;
                }
            }

            private MethodInfo? Resolve()
            {
                if (_resolved)
                {
                    return _getItems;
                }

                _resolved = true;

                Type? api = FindApiType();
                _getItems = api?.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

                if (_getItems == null)
                {
                    Plugin.Log.LogWarning($"{Name} is installed but {ApiTypeName}.{MethodName}() was not found. Update {Name}; quick slot items are treated as regular items.");
                }
                else
                {
                    Plugin.Debug($"{Name} {LoadedVersion} quick slot API resolved");
                }

                return _getItems;
            }

            private Type? FindApiType()
            {
                // The loaded plugin instance gives the exact assembly BepInEx loaded; the
                // assembly-qualified lookup is the fallback documented by the mods themselves.
                if (Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) && info?.Instance != null)
                {
                    Type? fromPlugin = info.Instance.GetType().Assembly.GetType(ApiTypeName, throwOnError: false);
                    if (fromPlugin != null)
                    {
                        return fromPlugin;
                    }
                }

                return Type.GetType($"{ApiTypeName}, {AssemblyName}", throwOnError: false);
            }
        }

        public const string EquipmentAndQuickSlotsGuid = "randyknapp.mods.equipmentandquickslots";

        public const string ExtraSlotsGuid = "shudnal.ExtraSlots";

        private static readonly Provider[] Providers =
        {
            new Provider("EquipmentAndQuickSlots", EquipmentAndQuickSlotsGuid, "EquipmentAndQuickSlots", "EquipmentAndQuickSlots.API", "GetQuickSlotItems"),
            new Provider("Extra Slots", ExtraSlotsGuid, "ExtraSlots", "ExtraSlots.API", "GetQuickSlotsItems"),
        };

        /// <summary>Human-readable list of supported mods, for messages.</summary>
        public static string SupportedMods => "EquipmentAndQuickSlots 3.x, Extra Slots";

        /// <summary>The first supported quick slot mod that BepInEx has loaded, or null.</summary>
        public static Provider? Active
        {
            get
            {
                foreach (Provider provider in Providers)
                {
                    if (provider.IsLoaded)
                    {
                        return provider;
                    }
                }

                return null;
            }
        }

        /// <summary>Items currently in quick slots, or null when no supported mod is available.</summary>
        public static HashSet<ItemDrop.ItemData>? GetQuickSlotItems()
        {
            return Active?.GetItems();
        }
    }
}
