using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;

namespace DeathTweaks.Compat
{
    /// <summary>
    /// Bridges to the slot mods this plugin understands. Each provider answers one question
    /// through the mod's public API: is this inventory item sitting in one of the mod's
    /// quick slots? The checks are based on the item's grid position, not on the mods' cached
    /// slot contents, so they stay correct in the middle of the death pipeline. Everything is
    /// resolved by reflection so the plugin loads without any of these mods.
    /// </summary>
    internal static class QuickSlotMods
    {
        internal abstract class Provider
        {
            private bool _resolved;
            private Func<ItemDrop.ItemData, bool>? _classifier;

            protected Provider(string name, string pluginGuid, string assemblyName, string apiTypeName, string slotDescription)
            {
                Name = name;
                PluginGuid = pluginGuid;
                AssemblyName = assemblyName;
                ApiTypeName = apiTypeName;
                SlotDescription = slotDescription;
            }

            public string Name { get; }

            public string PluginGuid { get; }

            public string AssemblyName { get; }

            public string ApiTypeName { get; }

            /// <summary>Which of the mod's slots count as quick slots for this plugin.</summary>
            public string SlotDescription { get; }

            public bool IsLoaded => Chainloader.PluginInfos.ContainsKey(PluginGuid);

            public string LoadedVersion =>
                Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) && info?.Metadata != null
                    ? info.Metadata.Version.ToString()
                    : "?";

            /// <summary>
            /// The quick slot test, or null when the mod's API cannot be reached. Resolution
            /// happens once; the outcome is logged.
            /// </summary>
            public Func<ItemDrop.ItemData, bool>? Classifier
            {
                get
                {
                    if (_resolved)
                    {
                        return _classifier;
                    }

                    _resolved = true;

                    try
                    {
                        Type? api = FindApiType();
                        _classifier = api == null ? null : CreateClassifier(api);
                    }
                    catch (Exception exception)
                    {
                        Plugin.Log.LogWarning($"{Name} API could not be resolved: {exception.Message}");
                        _classifier = null;
                    }

                    if (_classifier == null)
                    {
                        Plugin.Log.LogWarning($"{Name} {LoadedVersion} is installed but its API ({ApiTypeName}) was not found or has changed. Update {Name}; quick slot items are treated as regular items.");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"Quick slot support: {Name} {LoadedVersion} ({SlotDescription})");
                    }

                    return _classifier;
                }
            }

            /// <summary>Builds the test from the API type; returns null when a member is missing.</summary>
            protected abstract Func<ItemDrop.ItemData, bool>? CreateClassifier(Type api);

            protected static MethodInfo? PublicStatic(Type api, string name, params Type[] parameters)
            {
                return api.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
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

        /// <summary>
        /// EquipmentAndQuickSlots 3.x: <c>API.IsSlotCell(x, y, ref slotId)</c> names the slot at a
        /// grid position; the built-in quick slots are <c>Quick1</c> to <c>Quick6</c>.
        /// </summary>
        private sealed class EquipmentAndQuickSlotsProvider : Provider
        {
            public EquipmentAndQuickSlotsProvider()
                : base("EquipmentAndQuickSlots", EquipmentAndQuickSlotsGuid, "EquipmentAndQuickSlots", "EquipmentAndQuickSlots.API", "quick slots")
            {
            }

            protected override Func<ItemDrop.ItemData, bool>? CreateClassifier(Type api)
            {
                MethodInfo? isSlotCell = PublicStatic(api, "IsSlotCell", typeof(int), typeof(int), typeof(string).MakeByRefType());
                if (isSlotCell == null)
                {
                    return null;
                }

                return item =>
                {
                    var args = new object?[] { item.m_gridPos.x, item.m_gridPos.y, null };
                    return isSlotCell.Invoke(null, args) is true
                        && args[2] is string slotId
                        && slotId.StartsWith("Quick", StringComparison.Ordinal);
                };
            }
        }

        /// <summary>
        /// Extra Slots: quick, food, ammo and misc slots all live in the extra inventory rows
        /// (<c>API.IsGridPositionASlot</c>); equipment slots are excluded because equipped items
        /// have their own option.
        /// </summary>
        private sealed class ExtraSlotsProvider : Provider
        {
            public ExtraSlotsProvider()
                : base("Extra Slots", ExtraSlotsGuid, "ExtraSlots", "ExtraSlots.API", "quick, food, ammo and misc slots")
            {
            }

            protected override Func<ItemDrop.ItemData, bool>? CreateClassifier(Type api)
            {
                MethodInfo? isSlotPosition = PublicStatic(api, "IsGridPositionASlot", typeof(Vector2i));
                MethodInfo? isEquipmentSlotItem = PublicStatic(api, "IsItemInEquipmentSlot", typeof(ItemDrop.ItemData));
                if (isSlotPosition == null || isEquipmentSlotItem == null)
                {
                    return null;
                }

                return item =>
                    isSlotPosition.Invoke(null, new object[] { item.m_gridPos }) is true
                    && isEquipmentSlotItem.Invoke(null, new object[] { item }) is false;
            }
        }

        public const string EquipmentAndQuickSlotsGuid = "randyknapp.mods.equipmentandquickslots";

        public const string ExtraSlotsGuid = "shudnal.ExtraSlots";

        private static readonly Provider[] Providers =
        {
            new EquipmentAndQuickSlotsProvider(),
            new ExtraSlotsProvider(),
        };

        /// <summary>Human-readable list of supported mods, for messages.</summary>
        public static string SupportedMods => "EquipmentAndQuickSlots 3.x, Extra Slots";

        /// <summary>The first supported slot mod that BepInEx has loaded, or null.</summary>
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

        /// <summary>
        /// Test for "this item is in a quick slot", or null when no supported mod is available.
        /// Exceptions thrown by the mod's API are logged and count as "not in a quick slot".
        /// </summary>
        public static Func<ItemDrop.ItemData, bool>? GetQuickSlotClassifier()
        {
            Provider? provider = Active;
            Func<ItemDrop.ItemData, bool>? classifier = provider?.Classifier;
            if (provider == null || classifier == null)
            {
                return null;
            }

            return item =>
            {
                try
                {
                    return classifier(item);
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogWarning($"{provider.Name} API call failed for {item.m_shared.m_name}, treating it as a regular item: {exception.Message}");
                    return false;
                }
            };
        }
    }
}
