using System;
using BepInEx.Logging;
using HarmonyLib;

namespace Muindor.ServerConfig
{
    /// <summary>
    /// Carries the server's configuration to the clients that have the mod, over one custom RPC
    /// named after the plugin GUID.
    ///
    /// Neither side needs the other to have the mod:
    ///
    /// - A vanilla client receives an RPC whose name it does not know. <c>ZRpc.HandlePackage</c>
    ///   looks the name up in its table, finds nothing and drops the packet; the connection is not
    ///   affected and the game's version check never sees the mod.
    /// - A client on a server without the mod is never sent anything and keeps its own settings.
    ///
    /// The server only ever sends and the client only ever receives, so a modded client cannot push
    /// settings onto the server or onto other players.
    /// </summary>
    public static class ConfigChannel
    {
        private static ConfigSync? _sync;

        private static ManualLogSource? _log;

        private static string _rpcName = string.Empty;

        /// <summary>Starts carrying the settings of <paramref name="sync"/>. Called once, from the plugin's Awake.</summary>
        public static void Activate(ConfigSync sync, ManualLogSource log)
        {
            _sync = sync;
            _log = log;
            _rpcName = sync.PluginGuid + ".ServerConfig";
            sync.Changed += Broadcast;
        }

        /// <summary>
        /// Client side: a connection to a server begins. Until that server says otherwise the player's
        /// own settings are in charge, so anything a previous server decided is forgotten here.
        /// </summary>
        internal static void OnNewConnection(ZNet net, ZNetPeer peer)
        {
            if (_sync == null || net.IsServer() || peer == null || !peer.m_server || peer.m_rpc == null)
            {
                return;
            }

            _sync.Clear();
            peer.m_rpc.Register<string>(_rpcName, Receive);
        }

        /// <summary>
        /// Server side: a client finished the handshake and was accepted (the peer has an id by then;
        /// every refusal in <c>ZNet.RPC_PeerInfo</c> leaves it at zero). Tell it what we run on.
        /// </summary>
        internal static void OnPeerInfo(ZNet net, ZRpc rpc)
        {
            if (_sync == null || !net.IsServer())
            {
                return;
            }

            ZNetPeer peer = net.GetPeer(rpc);
            if (peer == null || !peer.IsReady() || peer.m_rpc == null)
            {
                return;
            }

            peer.m_rpc.Invoke(_rpcName, _sync.BuildPayload().Serialize());
            _log?.LogInfo($"Sent the configuration to {peer.m_playerName} (ConfigPriority {(_sync.Priority.Value ? "on" : "off")})");
        }

        /// <summary>The session is over: the player's own settings are in charge again.</summary>
        internal static void OnSessionEnd()
        {
            _sync?.Clear();
        }

        /// <summary>A patch threw. The game keeps going; only the settings channel is affected.</summary>
        internal static void PatchFailed(string patch, Exception exception)
        {
            _log?.LogError($"{patch} failed, the configuration channel may be inactive: {exception}");
        }

        /// <summary>Server side: a synced setting changed while players are connected, so send it again.</summary>
        private static void Broadcast()
        {
            ZNet net = ZNet.instance;
            if (_sync == null || net == null || !net.IsServer())
            {
                return;
            }

            string text = _sync.BuildPayload().Serialize();
            int sent = 0;
            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (peer.IsReady() && peer.m_rpc != null)
                {
                    peer.m_rpc.Invoke(_rpcName, text);
                    sent++;
                }
            }

            if (sent > 0)
            {
                _log?.LogInfo($"A setting changed; sent the configuration to {sent} player(s)");
            }
        }

        /// <summary>Client side: the server's configuration arrived.</summary>
        private static void Receive(ZRpc rpc, string text)
        {
            if (_sync == null)
            {
                return;
            }

            ZNet net = ZNet.instance;
            if (net != null && net.IsServer())
            {
                // Only the server decides. Nothing in this mod sends towards it, so this is someone else's packet.
                _log?.LogWarning("Ignored a configuration sent to this server by a client");
                return;
            }

            if (!ConfigPayload.TryParse(text, out ConfigPayload? payload, out string error))
            {
                _log?.LogWarning($"Ignored the configuration from the server, keeping the local settings: {error}");
                return;
            }

            _sync.Apply(payload!);
        }
    }

    /// <summary>Registers the receiving end on the client, for the one peer that is the server.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
    internal static class ZNet_OnNewConnection_Patch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            try
            {
                ConfigChannel.OnNewConnection(__instance, peer);
            }
            catch (Exception exception)
            {
                ConfigChannel.PatchFailed(nameof(ZNet.OnNewConnection), exception);
            }
        }
    }

    /// <summary>Sends the configuration to a client the server has just accepted.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    internal static class ZNet_RPC_PeerInfo_Patch
    {
        private static void Postfix(ZNet __instance, ZRpc rpc)
        {
            try
            {
                ConfigChannel.OnPeerInfo(__instance, rpc);
            }
            catch (Exception exception)
            {
                ConfigChannel.PatchFailed(nameof(ZNet.RPC_PeerInfo), exception);
            }
        }
    }

    /// <summary>Leaving the world drops anything a server decided.</summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    internal static class ZNet_OnDestroy_Patch
    {
        private static void Postfix()
        {
            try
            {
                ConfigChannel.OnSessionEnd();
            }
            catch (Exception exception)
            {
                ConfigChannel.PatchFailed(nameof(ZNet.OnDestroy), exception);
            }
        }
    }
}
