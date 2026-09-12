using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;

namespace DeathTweaks.Compat
{
    /// <summary>
    /// Bridge to EquipmentAndQuickSlots 3.x through its public integration API
    /// (<c>EquipmentAndQuickSlots.API</c>), resolved by reflection so the plugin loads
    /// without the mod present. Slot items are ordinary entries of the player inventory
    /// in 3.x, so only their identity is needed.
    /// </summary>
    internal static class EquipmentAndQuickSlotsCompat
    {
        public const string PluginGuid = "randyknapp.mods.equipmentandquickslots";

        private const string ApiTypeName = "EquipmentAndQuickSlots.API, EquipmentAndQuickSlots";

        private static bool _resolved;
        private static MethodInfo? _getQuickSlotItems;

        public static bool IsLoaded => Chainloader.PluginInfos.ContainsKey(PluginGuid);

        /// <summary>
        /// Items currently sitting in quick slots, or null when the mod is not installed or
        /// its API cannot be reached.
        /// </summary>
        public static HashSet<ItemDrop.ItemData>? GetQuickSlotItems()
        {
            if (!IsLoaded)
            {
                return null;
            }

            MethodInfo? method = ResolveGetQuickSlotItems();
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
                Plugin.Log.LogWarning($"EquipmentAndQuickSlots API call failed, quick slot items are treated as regular items: {exception.Message}");
                return null;
            }
        }

        private static MethodInfo? ResolveGetQuickSlotItems()
        {
            if (_resolved)
            {
                return _getQuickSlotItems;
            }

            _resolved = true;

            Type? api = Type.GetType(ApiTypeName, throwOnError: false);
            _getQuickSlotItems = api?.GetMethod("GetQuickSlotItems", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

            if (_getQuickSlotItems == null)
            {
                Plugin.Log.LogWarning("EquipmentAndQuickSlots is installed but its API was not found. Update EquipmentAndQuickSlots to 3.x for quick slot support.");
            }
            else
            {
                Plugin.Debug("EquipmentAndQuickSlots API resolved");
            }

            return _getQuickSlotItems;
        }
    }
}
