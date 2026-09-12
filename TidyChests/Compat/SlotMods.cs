using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace TidyChests.Compat
{
    /// <summary>
    /// Bridges to the inventory slot mods this plugin understands. Each provider answers one
    /// question about an inventory item: does it sit in one of the mod's slots (quick slots,
    /// equipment slots, food, ammo, quiver rows)? Such items are never stashed. The checks are
    /// based on the item's grid position, and everything is resolved by reflection so the plugin
    /// loads without any of these mods.
    /// </summary>
    internal static class SlotMods
    {
        internal abstract class Provider
        {
            private bool _resolved;
            private Func<ItemDrop.ItemData, bool>? _classifier;

            protected Provider(string name, string pluginGuid, string slotDescription)
            {
                Name = name;
                PluginGuid = pluginGuid;
                SlotDescription = slotDescription;
            }

            public string Name { get; }

            public string PluginGuid { get; }

            /// <summary>Which of the mod's slots are protected from stashing.</summary>
            public string SlotDescription { get; }

            public bool IsLoaded => Chainloader.PluginInfos.ContainsKey(PluginGuid);

            public string LoadedVersion =>
                Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) && info?.Metadata != null
                    ? info.Metadata.Version.ToString()
                    : "?";

            /// <summary>The slot test, or null when the mod's API cannot be reached. Resolved once; the outcome is logged.</summary>
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
                        Assembly? assembly = PluginAssembly();
                        _classifier = assembly == null ? null : CreateClassifier(assembly);
                    }
                    catch (Exception exception)
                    {
                        Plugin.Log.LogWarning($"{Name} API could not be resolved: {exception.Message}");
                        _classifier = null;
                    }

                    if (_classifier == null)
                    {
                        Plugin.Log.LogWarning($"{Name} {LoadedVersion} is installed but its API was not found or has changed. Update {Name}; its slots are not protected from stashing.");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"Slot support: {Name} {LoadedVersion} ({SlotDescription} are never stashed)");
                    }

                    return _classifier;
                }
            }

            /// <summary>Builds the test from the mod's assembly; returns null when a member is missing.</summary>
            protected abstract Func<ItemDrop.ItemData, bool>? CreateClassifier(Assembly assembly);

            protected static MethodInfo? PublicStatic(Type api, string name, params Type[] parameters)
            {
                return api.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            }

            private Assembly? PluginAssembly()
            {
                if (Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) && info?.Instance != null)
                {
                    return info.Instance.GetType().Assembly;
                }

                return null;
            }
        }

        /// <summary>Extra Slots: quick, food, ammo, misc and equipment slots all answer <c>API.IsGridPositionASlot</c>.</summary>
        private sealed class ExtraSlotsProvider : Provider
        {
            public ExtraSlotsProvider()
                : base("Extra Slots", ExtraSlotsGuid, "quick, food, ammo, misc and equipment slots")
            {
            }

            protected override Func<ItemDrop.ItemData, bool>? CreateClassifier(Assembly assembly)
            {
                Type? api = assembly.GetType("ExtraSlots.API", throwOnError: false);
                MethodInfo? isSlotPosition = api == null ? null : PublicStatic(api, "IsGridPositionASlot", typeof(Vector2i));
                if (isSlotPosition == null)
                {
                    return null;
                }

                return item => isSlotPosition.Invoke(null, new object[] { item.m_gridPos }) is true;
            }
        }

        /// <summary>EquipmentAndQuickSlots 3.x: <c>API.IsSlotCell(x, y, ref slotId)</c> is true for every equipment and quick slot cell.</summary>
        private sealed class EquipmentAndQuickSlotsProvider : Provider
        {
            public EquipmentAndQuickSlotsProvider()
                : base("EquipmentAndQuickSlots", EquipmentAndQuickSlotsGuid, "equipment and quick slots")
            {
            }

            protected override Func<ItemDrop.ItemData, bool>? CreateClassifier(Assembly assembly)
            {
                Type? api = assembly.GetType("EquipmentAndQuickSlots.API", throwOnError: false);
                MethodInfo? isSlotCell = api == null ? null : PublicStatic(api, "IsSlotCell", typeof(int), typeof(int), typeof(string).MakeByRefType());
                if (isSlotCell == null)
                {
                    return null;
                }

                return item => isSlotCell.Invoke(null, new object?[] { item.m_gridPos.x, item.m_gridPos.y, null }) is true;
            }
        }

        /// <summary>
        /// Better Archery: the quiver is two extra inventory rows, the upper one unused. The row
        /// index and the quiver toggle are public static fields of the plugin class.
        /// </summary>
        private sealed class BetterArcheryProvider : Provider
        {
            public BetterArcheryProvider()
                : base("Better Archery", BetterArcheryGuid, "quiver rows")
            {
            }

            protected override Func<ItemDrop.ItemData, bool>? CreateClassifier(Assembly assembly)
            {
                Type? plugin = assembly.GetTypes().FirstOrDefault(type => type.IsClass && type.Name == "BetterArchery");
                FieldInfo? enabled = plugin?.GetField("ConfigQuiverEnabled", BindingFlags.Public | BindingFlags.Static);
                FieldInfo? rowIndex = plugin?.GetField("QuiverRowIndex", BindingFlags.Public | BindingFlags.Static);
                if (enabled == null || rowIndex == null)
                {
                    return null;
                }

                return item =>
                {
                    if (!(enabled.GetValue(null) is ConfigEntry<bool> toggle) || !toggle.Value || !(rowIndex.GetValue(null) is int row) || row <= 0)
                    {
                        return false;
                    }

                    return item.m_gridPos.y == row || item.m_gridPos.y == row - 1;
                };
            }
        }

        public const string ExtraSlotsGuid = "shudnal.ExtraSlots";

        public const string EquipmentAndQuickSlotsGuid = "randyknapp.mods.equipmentandquickslots";

        public const string BetterArcheryGuid = "ishid4.mods.betterarchery";

        private static readonly Provider[] Providers =
        {
            new ExtraSlotsProvider(),
            new EquipmentAndQuickSlotsProvider(),
            new BetterArcheryProvider(),
        };

        /// <summary>Human-readable list of supported mods, for messages.</summary>
        public static string SupportedMods => "Extra Slots, EquipmentAndQuickSlots 3.x, Better Archery";

        /// <summary>Every supported slot mod that BepInEx has loaded.</summary>
        public static IEnumerable<Provider> Active => Providers.Where(provider => provider.IsLoaded);

        /// <summary>Resolves the loaded mods' APIs so the outcome is logged at startup rather than at the first stash.</summary>
        public static void Report()
        {
            bool any = false;
            foreach (Provider provider in Active)
            {
                any = true;
                _ = provider.Classifier;
            }

            if (!any)
            {
                Plugin.Log.LogInfo($"No slot mod loaded ({SupportedMods}); only equipped and hotbar items are protected");
            }
        }

        /// <summary>
        /// Test for "this item sits in a slot of a slot mod", or null when no supported mod is
        /// available. Exceptions thrown by a mod's API are logged and count as "not in a slot".
        /// </summary>
        public static Func<ItemDrop.ItemData, bool>? GetClassifier()
        {
            var classifiers = new List<KeyValuePair<string, Func<ItemDrop.ItemData, bool>>>();
            foreach (Provider provider in Active)
            {
                Func<ItemDrop.ItemData, bool>? classifier = provider.Classifier;
                if (classifier != null)
                {
                    classifiers.Add(new KeyValuePair<string, Func<ItemDrop.ItemData, bool>>(provider.Name, classifier));
                }
            }

            if (classifiers.Count == 0)
            {
                return null;
            }

            return item =>
            {
                foreach (KeyValuePair<string, Func<ItemDrop.ItemData, bool>> classifier in classifiers)
                {
                    try
                    {
                        if (classifier.Value(item))
                        {
                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        Plugin.Log.LogWarning($"{classifier.Key} API call failed for {item.m_shared.m_name}, treating it as a regular item: {exception.Message}");
                    }
                }

                return false;
            };
        }
    }
}
