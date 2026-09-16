using System;
using System.Collections.Generic;
using CombatStats.Ui;
using UnityEngine;

namespace CombatStats.Net
{
    /// <summary>
    /// Carries what this client saw to the other players who have the mod.
    ///
    /// Two routed RPCs, both named after the plugin so nothing else can collide with them. A
    /// client without the mod looks the name up, finds nothing and drops the packet, which is why
    /// the mod stays optional for everyone in the session.
    ///
    /// Only the client that owns a target sees what the target really took, so that client is the
    /// one that shares it. Everything received is filtered by distance, except what is about the
    /// local player: their own hits count wherever the client reporting them happens to stand.
    /// </summary>
    internal sealed class DamageChannel : MonoBehaviour
    {
        private const string HelloRpc = "CombatStats.Hello";

        private const string DamageRpc = "CombatStats.Damage";

        /// <summary>
        /// The routed RPC this client registered its handlers on. The game builds a new one for
        /// every session and never clears the static that points at the old one, so the object
        /// itself - not a flag, and not <c>instance != null</c> - is what says whether the
        /// handlers are still live.
        /// </summary>
        private ZRoutedRpc? _bound;

        private int _peers = -1;

        private bool _sharing;

        private float _nextSend;

        private bool _reportedBadPacket;

        private void Update()
        {
            if (Plugin.Settings == null)
            {
                return;
            }

            ZNet net = ZNet.instance;
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (net == null || rpc == null)
            {
                if (_bound != null)
                {
                    EndSession();
                }

                return;
            }

            if (!ReferenceEquals(rpc, _bound))
            {
                EndSession();
                Register(rpc);
            }

            if (Time.time < _nextSend)
            {
                return;
            }

            _nextSend = Time.time + Mathf.Max(0.1f, Plugin.Settings.ShareInterval.Value);
            Announce(net, rpc);
            Plugin.Collector.Tick();
            Send(rpc);
        }

        /// <summary>The world is gone: nothing recorded in it belongs to the next one.</summary>
        private void EndSession()
        {
            _bound = null;
            _peers = -1;
            PeerRegistry.Clear();
            Plugin.Collector.Clear();
            DetailWindow.Instance?.Close();
            Plugin.Debug("Session over; the meter starts again from nothing");
        }

        private void Register(ZRoutedRpc rpc)
        {
            rpc.Register<ZPackage>(HelloRpc, OnHello);
            rpc.Register<ZPackage>(DamageRpc, OnDamage);
            _bound = rpc;

            // Peers join after this point, so the greeting waits for one to exist.
            _peers = -1;
            _sharing = Plugin.Settings.ShareDamage.Value;
        }

        /// <summary>
        /// Says hello whenever the company changes, because a greeting sent before a peer has
        /// finished its handshake goes nowhere: the game only adds it to the routed RPC at the end
        /// of <c>ZNet.RPC_PeerInfo</c>. The sharing flag travels with it, so a client that keeps
        /// its numbers to itself is not mistaken for one that will report them.
        /// </summary>
        private void Announce(ZNet net, ZRoutedRpc rpc)
        {
            int peers = net.GetPeers().Count;
            bool sharing = Plugin.Settings.ShareDamage.Value;
            if (peers == _peers && sharing == _sharing)
            {
                return;
            }

            _peers = peers;
            _sharing = sharing;

            if (peers > 0)
            {
                Hello(rpc, ZRoutedRpc.Everybody, reply: false, sharing);
            }
        }

        private static void Hello(ZRoutedRpc rpc, long target, bool reply, bool sharing)
        {
            var package = new ZPackage();
            package.Write(reply);
            package.Write(sharing);
            rpc.InvokeRoutedRPC(target, HelloRpc, package);
        }

        /// <summary>Another client with the mod is in the session. An announcement is answered once.</summary>
        private void OnHello(long sender, ZPackage package)
        {
            try
            {
                Greeted(sender, package);
            }
            catch (Exception exception)
            {
                ReportBadPacket(sender, exception.Message);
            }
        }

        private void Greeted(long sender, ZPackage package)
        {
            if (sender == ZNet.GetUID())
            {
                return;
            }

            bool reply = true;
            bool sharing = true;
            try
            {
                reply = package.ReadBool();
                sharing = package.ReadBool();
            }
            catch (Exception)
            {
                // An older or newer version of the mod; the defaults above are the safe reading.
            }

            if (sharing)
            {
                if (PeerRegistry.Add(sender))
                {
                    Plugin.Debug($"{Plugin.Collector.NameOf(sender)} has the mod and shares ({PeerRegistry.Count} in the session)");
                }
            }
            else if (PeerRegistry.Remove(sender))
            {
                Plugin.Debug($"{Plugin.Collector.NameOf(sender)} has the mod but keeps its numbers to itself");
            }

            if (reply)
            {
                return;
            }

            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc != null)
            {
                Hello(rpc, sender, reply: true, Plugin.Settings.ShareDamage.Value);
            }
        }

        /// <summary>A batch of events another client recorded as the owner of the targets.</summary>
        private void OnDamage(long sender, ZPackage package)
        {
            try
            {
                Received(sender, package);
            }
            catch (Exception exception)
            {
                // The game logs and swallows what escapes an RPC handler, but a packet from
                // another machine is exactly what must not be allowed to get that far.
                ReportBadPacket(sender, exception.Message);
            }
        }

        private void Received(long sender, ZPackage package)
        {
            if (sender == ZNet.GetUID() || !Plugin.Enabled)
            {
                return;
            }

            PeerRegistry.Add(sender);

            byte[] data;
            try
            {
                data = package.ReadByteArray();
            }
            catch (Exception exception)
            {
                ReportBadPacket(sender, exception.Message);
                return;
            }

            if (!EventCodec.TryDecode(data, out Batch? batch, out string error) || batch == null)
            {
                ReportBadPacket(sender, error);
                return;
            }

            Player local = Player.m_localPlayer;
            if (local == null)
            {
                return;
            }

            // The owner of a creature may stand a long way from it - ownership only moves when the
            // creature leaves the owner's active area - so a batch from far away is not thrown out
            // whole: what it says about the local player is kept regardless.
            float radius = Plugin.Settings.ShareRadius.Value;
            bool nearby = (new Vector3(batch.X, batch.Y, batch.Z) - local.transform.position).sqrMagnitude <= radius * radius;

            Plugin.Collector.OnRemote(batch, nearby);
        }

        private void Send(ZRoutedRpc rpc)
        {
            List<WireEvent>? pending = Plugin.Collector.TakePending();
            if (pending == null || pending.Count == 0 || !Plugin.Settings.ShareDamage.Value || _peers <= 0)
            {
                return;
            }

            Player local = Player.m_localPlayer;
            if (local == null)
            {
                return;
            }

            Vector3 position = local.transform.position;
            byte[] data = EventCodec.Encode(ZNet.GetUID(), position.x, position.y, position.z, pending);

            var package = new ZPackage();
            package.Write(data);
            rpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, DamageRpc, package);
        }

        private void ReportBadPacket(long sender, string error)
        {
            if (_reportedBadPacket)
            {
                return;
            }

            _reportedBadPacket = true;
            Plugin.Log.LogWarning($"Ignored a combat packet from {sender}: {error}. Probably a different version of the mod; this is only reported once.");
        }
    }
}
