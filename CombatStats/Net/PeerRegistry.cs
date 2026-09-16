using System.Collections.Generic;

namespace CombatStats.Net
{
    /// <summary>
    /// The players in this session known to run the mod.
    ///
    /// It decides one thing: whether the local client has to guess. When the owner of a target is
    /// in here, its own report of the damage is on its way and the local estimate would be a
    /// second copy of the same hit; when it is not, the estimate is all there will ever be.
    /// </summary>
    internal static class PeerRegistry
    {
        private static readonly HashSet<long> Peers = new HashSet<long>();

        /// <summary>How many other players are running the mod.</summary>
        public static int Count => Peers.Count;

        /// <summary>True when this peer has answered, or sent, a greeting.</summary>
        public static bool Knows(long uid)
        {
            return uid != 0L && Peers.Contains(uid);
        }

        /// <summary>A greeting or a packet arrived from this peer.</summary>
        public static bool Add(long uid)
        {
            return uid != 0L && Peers.Add(uid);
        }

        /// <summary>The session ended.</summary>
        public static void Clear()
        {
            Peers.Clear();
        }
    }
}
