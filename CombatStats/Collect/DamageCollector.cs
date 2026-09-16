using System;
using System.Collections.Generic;
using CombatStats.Model;
using CombatStats.Net;
using CombatStats.Stats;
using UnityEngine;

namespace CombatStats.Collect
{
    /// <summary>
    /// Where every event ends up: one ring of buckets per channel, the damage-over-time table, the
    /// names of the combatants seen so far, and the queue of events waiting to be shared.
    ///
    /// Everything here is called from Harmony patches, so it must be cheap: recording a hit is a
    /// dictionary lookup into a pooled entry and allocates nothing. Sharing one costs a small
    /// array per event, which is the price of holding it until the next packet goes out.
    /// </summary>
    internal sealed class DamageCollector
    {
        /// <summary>How long a burn or a poison can still be charged to whoever started it.</summary>
        private const double DotMemory = 30d;

        private readonly StatsRecorder?[] _recorders = new StatsRecorder?[4];

        private readonly DotAttribution _dot = new DotAttribution(DotMemory);

        private readonly Dictionary<long, string> _names = new Dictionary<long, string>();

        private readonly List<WireEvent> _pending = new List<WireEvent>();

        private readonly float[] _scratch = new float[DamageKinds.Count];

        private readonly int _historySeconds;

        private double _pruned;

        public DamageCollector(int historySeconds)
        {
            _historySeconds = historySeconds;
        }

        /// <summary>The clock every window is measured on.</summary>
        public static double Now => Time.time;

        /// <summary>The ring for a channel, created the first time the channel sees an event.</summary>
        public StatsRecorder Recorder(CombatChannel channel)
        {
            int index = (int)channel;
            StatsRecorder? recorder = _recorders[index];
            if (recorder == null)
            {
                recorder = new StatsRecorder(_historySeconds);
                _recorders[index] = recorder;
            }

            return recorder;
        }

        /// <summary>The ring for a channel, or null when nothing has ever been recorded in it.</summary>
        public StatsRecorder? RecorderIfAny(CombatChannel channel)
        {
            return _recorders[(int)channel];
        }

        /// <summary>
        /// The name to show for a combatant: what was seen locally, else the player list the
        /// server keeps. Only a real name is remembered - a placeholder would stick for the whole
        /// session, and a player first seen through someone else's packet would never get a name.
        /// </summary>
        public string NameOf(long id)
        {
            if (_names.TryGetValue(id, out string? known) && !string.IsNullOrEmpty(known))
            {
                return known!;
            }

            string? resolved = ResolveName(id);
            if (string.IsNullOrEmpty(resolved))
            {
                return "...";
            }

            _names[id] = resolved!;
            return resolved!;
        }

        /// <summary>Events collected since the last packet, or null when there are none.</summary>
        public List<WireEvent>? TakePending()
        {
            if (_pending.Count == 0)
            {
                return null;
            }

            var batch = new List<WireEvent>(_pending);
            _pending.Clear();
            return batch;
        }

        /// <summary>
        /// A blow that carried fire, poison or spirit has been through the target's owner. Those
        /// three are stripped out of the hit there and tick later with nobody's name on them, so
        /// the attacker is noted down for the ticks to be charged to.
        ///
        /// Called after the fact: the game only empties those three fields on the path that
        /// reaches <c>ApplyDamage</c>, so finding them still filled means the blow was discarded -
        /// a dodge, a corpse, PvP being off - and nothing will ever tick from it.
        /// </summary>
        public void OnBlow(Character target, HitData hit)
        {
            if (target == null || hit == null)
            {
                return;
            }

            if (hit.m_damage.m_fire > 0f || hit.m_damage.m_poison > 0f || hit.m_damage.m_spirit > 0f)
            {
                return;
            }

            if (!Attribution.TryCombatant(hit.GetAttacker(), Plugin.Settings.CountPets.Value, out long attacker, out string name))
            {
                return;
            }

            Remember(attacker, name);
            _dot.Remember(Now, Attribution.TargetKey(target.GetZDOID()), attacker);
        }

        /// <summary>
        /// The damage the target really took, on the client that owns it. This is the only place
        /// a combat event is recorded as a fact rather than an estimate.
        /// </summary>
        public void OnApplied(Character target, HitData hit)
        {
            if (target == null || hit == null)
            {
                return;
            }

            double now = Now;
            float total = Fill(hit);
            if (total <= 0.1f)
            {
                // What the game itself throws away: ApplyDamage returns before the health is
                // touched, so recording it would count damage the target never took.
                return;
            }

            bool countPets = Plugin.Settings.CountPets.Value;

            if (target.IsPlayer() && Plugin.Settings.CountDamageTaken.Value
                && Attribution.TryCombatant(target, countPets: true, out long victim, out string victimName))
            {
                Remember(victim, victimName);
                Record(CombatChannel.DamageTaken, victim, now, estimated: false);
            }

            Character attacker = hit.GetAttacker();
            if (attacker != null)
            {
                if (Attribution.TryCombatant(attacker, countPets, out long dealer, out string dealerName))
                {
                    Remember(dealer, dealerName);
                    Record(CombatChannel.DamageDealt, dealer, now, estimated: false);
                }

                return;
            }

            // No attacker on the hit: either a tick of something that was set alight, or the world
            // itself (a fall, the ocean). Only the first has a row to go into.
            if (IsOverTimeOnly() && _dot.TryResolve(now, Attribution.TargetKey(target.GetZDOID()), out long lit))
            {
                Record(CombatChannel.DamageDealt, lit, now, estimated: false);
            }
        }

        /// <summary>
        /// The local player hit something owned by a client without the mod: nobody will ever
        /// report what it really took, so the blow is recorded as sent - before resistances and
        /// armour - and marked as an estimate.
        /// </summary>
        public void OnSent(Character target, HitData hit)
        {
            if (target == null || hit == null)
            {
                return;
            }

            Player local = Player.m_localPlayer;
            if (local == null || hit.m_attacker != local.GetZDOID())
            {
                return;
            }

            ZNetView view = target.m_nview;
            if (view == null || !view.IsValid() || view.IsOwner())
            {
                return;
            }

            long owner = view.GetZDO().GetOwner();
            if (owner == 0L || PeerRegistry.Knows(owner))
            {
                return;
            }

            if (Fill(hit) <= 0f)
            {
                return;
            }

            long id = local.GetZDOID().UserID;
            Remember(id, local.GetPlayerName());
            Record(CombatChannel.DamageDealt, id, Now, estimated: true);
        }

        /// <summary>A player regained health.</summary>
        public void OnHeal(Character target, float amount)
        {
            if (target == null || amount <= 0f || !target.IsPlayer() || !Plugin.Settings.CountHealing.Value)
            {
                return;
            }

            if (!Attribution.TryCombatant(target, countPets: true, out long id, out string name))
            {
                return;
            }

            Remember(id, name);
            Array.Clear(_scratch, 0, _scratch.Length);
            _scratch[(int)DamageKind.Generic] = amount;
            Record(CombatChannel.Healing, id, Now, estimated: false);
        }

        /// <summary>A tree, a rock, a building or a ship was hit.</summary>
        public void OnObject(HitData hit)
        {
            if (hit == null || !Plugin.Settings.CountObjectDamage.Value)
            {
                return;
            }

            if (!Attribution.TryCombatant(hit.GetAttacker(), Plugin.Settings.CountPets.Value, out long id, out string name))
            {
                return;
            }

            if (Fill(hit) <= 0f)
            {
                return;
            }

            Remember(id, name);

            // Trees, rock and buildings apply their own resistances on the owner's side, and this
            // is the sending side, so the figure is what was swung, not what landed.
            Record(CombatChannel.ObjectDamage, id, Now, estimated: true);
        }

        /// <summary>
        /// Events another client saw and shared. A batch sent from outside the share radius is
        /// not dropped whole: what it says about the local player is always kept, because the
        /// owner of a creature can stand far from both the creature and the player hitting it.
        /// </summary>
        public void OnRemote(Batch batch, bool nearby)
        {
            double now = Now;
            long local = LocalId();

            foreach (WireEvent shared in batch.Events)
            {
                if (!nearby && shared.CombatantId != local)
                {
                    continue;
                }

                if (Attribution.IsCreature(shared.CombatantId) && !Plugin.Settings.CountPets.Value)
                {
                    continue;
                }

                CombatChannel channel = shared.Channel;
                if (!Wanted(channel))
                {
                    continue;
                }

                Recorder(channel).Record(now, shared.CombatantId, shared.ByKind, shared.Estimated);
            }
        }

        /// <summary>The local player's row, or zero while there is no player.</summary>
        public static long LocalId()
        {
            Player local = Player.m_localPlayer;
            return local != null ? local.GetZDOID().UserID : 0L;
        }

        /// <summary>Housekeeping: drops damage-over-time marks nobody will ever claim.</summary>
        public void Tick()
        {
            double now = Now;
            if (now - _pruned < DotMemory)
            {
                return;
            }

            _pruned = now;
            _dot.Prune(now);
        }

        /// <summary>The session is over.</summary>
        public void Clear()
        {
            for (int index = 0; index < _recorders.Length; index++)
            {
                _recorders[index]?.Clear();
            }

            _dot.Clear();
            _names.Clear();
            _pending.Clear();
        }

        private static bool Wanted(CombatChannel channel)
        {
            switch (channel)
            {
                case CombatChannel.DamageTaken: return Plugin.Settings.CountDamageTaken.Value;
                case CombatChannel.Healing: return Plugin.Settings.CountHealing.Value;
                case CombatChannel.ObjectDamage: return Plugin.Settings.CountObjectDamage.Value;
                default: return true;
            }
        }

        /// <summary>
        /// A name for a combatant seen only through the network. On a client the peer list holds
        /// nothing but the server, so the answer comes from the player list the server sends
        /// round: its character ids carry the same user id the meter keys players by.
        /// </summary>
        private static string? ResolveName(long id)
        {
            ZNet net = ZNet.instance;
            if (net == null)
            {
                return null;
            }

            if (id == ZNet.GetUID())
            {
                Player local = Player.m_localPlayer;
                if (local != null)
                {
                    return local.GetPlayerName();
                }
            }

            foreach (ZNet.PlayerInfo player in net.GetPlayerList())
            {
                if (player.m_characterID.UserID == id && !string.IsNullOrEmpty(player.m_name))
                {
                    return player.m_name;
                }
            }

            foreach (ZNetPeer peer in net.GetPeers())
            {
                if (peer.m_uid == id && !string.IsNullOrEmpty(peer.m_playerName))
                {
                    return peer.m_playerName;
                }
            }

            return null;
        }

        /// <summary>Splits a hit into the scratch array and returns what it adds up to.</summary>
        private float Fill(HitData hit)
        {
            HitData.DamageTypes damage = hit.m_damage;
            _scratch[(int)DamageKind.Blunt] = Positive(damage.m_blunt);
            _scratch[(int)DamageKind.Slash] = Positive(damage.m_slash);
            _scratch[(int)DamageKind.Pierce] = Positive(damage.m_pierce);
            _scratch[(int)DamageKind.Chop] = Positive(damage.m_chop);
            _scratch[(int)DamageKind.Pickaxe] = Positive(damage.m_pickaxe);
            _scratch[(int)DamageKind.Fire] = Positive(damage.m_fire);
            _scratch[(int)DamageKind.Frost] = Positive(damage.m_frost);
            _scratch[(int)DamageKind.Lightning] = Positive(damage.m_lightning);
            _scratch[(int)DamageKind.Poison] = Positive(damage.m_poison);
            _scratch[(int)DamageKind.Spirit] = Positive(damage.m_spirit);
            _scratch[(int)DamageKind.Generic] = Positive(damage.m_damage);

            float total = 0f;
            for (int kind = 0; kind < _scratch.Length; kind++)
            {
                total += _scratch[kind];
            }

            return total;
        }

        private static float Positive(float value)
        {
            return value > 0f ? value : 0f;
        }

        private bool IsOverTimeOnly()
        {
            for (int kind = 0; kind < _scratch.Length; kind++)
            {
                if (_scratch[kind] > 0f && !DamageKinds.IsOverTime((DamageKind)kind))
                {
                    return false;
                }
            }

            return true;
        }

        private void Record(CombatChannel channel, long combatantId, double now, bool estimated)
        {
            if (!Wanted(channel))
            {
                return;
            }

            Recorder(channel).Record(now, combatantId, _scratch, estimated);

            if (Plugin.Settings.ShareDamage.Value && ZNet.instance != null)
            {
                var copy = new float[DamageKinds.Count];
                Array.Copy(_scratch, copy, copy.Length);
                _pending.Add(new WireEvent(combatantId, channel, copy, estimated));
            }

            if (Plugin.Settings.IsDebug.Value)
            {
                Plugin.Log.LogInfo($"{channel}: {NameOf(combatantId)} {(estimated ? "~" : string.Empty)}{Sum():0.#}");
            }
        }

        private float Sum()
        {
            float total = 0f;
            for (int kind = 0; kind < _scratch.Length; kind++)
            {
                total += _scratch[kind];
            }

            return total;
        }

        private void Remember(long id, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _names[id] = name;
            }
        }
    }
}
