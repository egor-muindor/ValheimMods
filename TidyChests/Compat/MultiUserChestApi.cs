using System;
using System.Reflection;
using HarmonyLib;

namespace TidyChests.Compat
{
    /// <summary>
    /// MultiUserChest's request for moving a stack inside a chest owned by another player,
    /// <c>MultiUserChest.ContainerHandler.MoveItemInChest(Container, ItemData, Vector2i, int)</c>.
    /// It is what MultiUserChest itself sends when such a chest is rearranged by drag and drop:
    /// the owner moves the stack found at the item's grid position, after checking it is the
    /// same prefab. Reached by reflection, so the mod neither needs nor ships MultiUserChest.
    /// </summary>
    internal static class MultiUserChestApi
    {
        private const string HandlerType = "MultiUserChest.ContainerHandler";

        private const string MoveMethod = "MoveItemInChest";

        private static bool _resolved;

        private static MethodInfo? _move;

        /// <summary>True when MultiUserChest is loaded and has the request this mod sends.</summary>
        public static bool CanMoveInChest
        {
            get
            {
                Resolve();
                return _move != null;
            }
        }

        /// <summary>
        /// Asks the owner of <paramref name="container"/> to move <paramref name="amount"/> units
        /// of the stack at <paramref name="item"/>'s grid position to <paramref name="to"/>.
        /// </summary>
        public static void MoveInChest(Container container, ItemDrop.ItemData item, Vector2i to, int amount)
        {
            Resolve();
            if (_move == null)
            {
                throw new InvalidOperationException($"{HandlerType}.{MoveMethod} is not available");
            }

            _move.Invoke(null, new object[] { container, item, to, amount });
        }

        private static void Resolve()
        {
            if (_resolved || !ChestMods.MultiUserChestLoaded)
            {
                return;
            }

            _resolved = true;
            Type? handler = AccessTools.TypeByName(HandlerType);
            _move = handler == null
                ? null
                : AccessTools.Method(handler, MoveMethod, new[] { typeof(Container), typeof(ItemDrop.ItemData), typeof(Vector2i), typeof(int) });
            if (_move == null)
            {
                Plugin.Log.LogWarning($"MultiUserChest is loaded but {HandlerType}.{MoveMethod} was not found (a newer version?): " +
                                      "a chest owned by another player cannot be sorted.");
            }
        }
    }
}
