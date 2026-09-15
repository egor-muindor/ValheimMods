using UnityEngine;

namespace TidyChests.Containers
{
    /// <summary>
    /// Whether the local player may look into a container or put items into it from a
    /// distance, and how to get write access to it. The checks mirror what vanilla does when a
    /// chest is opened (ward, privacy) plus what a remote write needs: nobody else using it,
    /// and ownership of its network object. With MultiUserChest the last two are its business.
    /// </summary>
    internal static class ContainerAccess
    {
        /// <summary>
        /// True when the local player could open <paramref name="container"/> by hand, so its
        /// contents may be listed and learned from; otherwise <paramref name="reason"/> says
        /// why not. Says nothing about writing: see <see cref="CanStashInto"/>.
        /// </summary>
        public static bool CanRead(Container container, long playerId, out string reason)
        {
            reason = "";
            if (container == null || container.GetInventory() == null)
            {
                reason = "not initialised";
                return false;
            }

            ZNetView view = container.m_nview;
            if (view == null || !view.IsValid())
            {
                reason = "no network object";
                return false;
            }

            Player carrier = view.GetComponent<Player>();
            if (carrier != null && carrier != Player.m_localPlayer)
            {
                reason = "carried by another player";
                return false;
            }

            if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, flash: false))
            {
                reason = "inside someone else's ward";
                return false;
            }

            if (!container.CheckAccess(playerId))
            {
                reason = "private chest of another player";
                return false;
            }

            return true;
        }

        /// <summary>True when items may be moved into <paramref name="container"/>; otherwise <paramref name="reason"/> says why not.</summary>
        public static bool CanStashInto(Container container, long playerId, bool multiUserChest, out string reason)
        {
            if (!CanRead(container, playerId, out reason))
            {
                return false;
            }

            ZNetView view = container.m_nview;
            if (IsOpenByLocalPlayer(container) || multiUserChest)
            {
                return true;
            }

            if (container.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) == 1)
            {
                reason = "in use by another player";
                return false;
            }

            if (container.m_wagon != null && container.m_wagon.InUse())
            {
                reason = "cart in use";
                return false;
            }

            if (!IsAlone() && container.GetComponentInParent<Ship>() != null)
            {
                // Owning the cargo means owning the ship; not while someone else may be sailing it.
                reason = "ship cargo in multiplayer without MultiUserChest";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Makes the container writable: refreshes the local copy of its contents and takes
        /// ownership of its network object. MultiUserChest forwards writes to the owner itself.
        /// </summary>
        public static bool PrepareForWrite(Container container, bool multiUserChest)
        {
            if (multiUserChest)
            {
                return true;
            }

            container.Load();
            container.m_nview.ClaimOwnership();
            return container.m_nview.IsOwner();
        }

        public static bool IsOpenByLocalPlayer(Container container)
        {
            InventoryGui gui = InventoryGui.instance;
            return gui != null && gui.m_currentContainer == container;
        }

        /// <summary>Localized display name of the container, for logs and markers.</summary>
        public static string NameOf(Container container)
        {
            Inventory? inventory = container.GetInventory();
            return inventory == null ? container.name : Localization.instance.Localize(inventory.GetName());
        }

        private static bool IsAlone()
        {
            ZNet net = ZNet.instance;
            return net == null || net.GetNrOfPlayers() <= 1;
        }
    }
}
