using System;
using System.Collections.Generic;
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
    /// one that shares it. Everything received is filtered by distance: a fight on the far side of
    /// the map has nothing to do with the meter on this screen.
    /// </summary>
    internal sealed class DamageChannel : MonoBehaviour
    {
        private const string HelloRpc = "CombatStats.Hello";

        private const string DamageRpc = "CombatStats.Damage";

        private bool _registered;

        private float _nextSend;

        private bool _reportedBadPacket;

        private void Update()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
            {
                if (_registered)
                {
                    // The session ended with the world; the next one starts from scratch.
                    _registered = false;
                    PeerRegistry.Clear();
                    Plugin.Collector.Clear();
                }

                return;
            }

            if (!_registered)
            {
                Register(rpc);
            }

            if (Time.time < _nextSend)
            {
                return;
            }

            _nextSend = Time.time + Mathf.Max(0.1f, Plugin.Settings.ShareInterval.Value);
            Plugin.Collector.Tick();
            Send(rpc);
        }

        private void Register(ZRoutedRpc rpc)
        {
            rpc.Register<ZPackage>(HelloRpc, OnHello);
            rpc.Register<ZPackage>(DamageRpc, OnDamage);
            _registered = true;

            var greeting = new ZPackage();
            greeting.Write(false);
            rpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, HelloRpc, greeting);
            Plugin.Debug("Said hello to the session; anyone else with the mod answers.");
        }

        /// <summary>Another client with the mod is in the session. An announcement is answered once.</summary>
        private void OnHello(long sender, ZPackage package)
        {
            if (sender == ZNet.GetUID())
            {
                return;
            }

            bool isReply;
            try
            {
                isReply = package.ReadBool();
            }
            catch (Exception)
            {
                isReply = true;
            }

            if (PeerRegistry.Add(sender))
            {
                Plugin.Debug($"{Plugin.Collector.NameOf(sender)} has the mod ({PeerRegistry.Count} in the session)");
            }

            if (isReply)
            {
                return;
            }

            ZRoutedRpc rpc = ZRoutedRpc.instance;
            if (rpc == null)
            {
                return;
            }

            var answer = new ZPackage();
            answer.Write(true);
            rpc.InvokeRoutedRPC(sender, HelloRpc, answer);
        }

        /// <summary>A batch of events another client recorded as the owner of the targets.</summary>
        private void OnDamage(long sender, ZPackage package)
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

            float radius = Plugin.Settings.ShareRadius.Value;
            if ((new Vector3(batch.X, batch.Y, batch.Z) - local.transform.position).sqrMagnitude > radius * radius)
            {
                return;
            }

            Plugin.Collector.OnRemote(batch);
        }

        private void Send(ZRoutedRpc rpc)
        {
            List<WireEvent>? pending = Plugin.Collector.TakePending();
            if (pending == null || !Plugin.Settings.ShareDamage.Value)
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
