using System;
using System.Collections.Generic;
using System.IO;
using CombatStats.Model;

namespace CombatStats.Net
{
    /// <summary>One event as it travels between clients.</summary>
    public readonly struct WireEvent
    {
        public WireEvent(long combatantId, CombatChannel channel, float[] byKind, bool estimated)
        {
            CombatantId = combatantId;
            Channel = channel;
            ByKind = byKind;
            Estimated = estimated;
        }

        /// <summary>The combatant the damage belongs to.</summary>
        public long CombatantId { get; }

        /// <summary>Which meter the event goes into.</summary>
        public CombatChannel Channel { get; }

        /// <summary>Damage per kind, indexed by <see cref="DamageKind"/>.</summary>
        public float[] ByKind { get; }

        /// <summary>Set when the sender only had the pre-mitigation figure.</summary>
        public bool Estimated { get; }
    }

    /// <summary>A decoded packet: who sent it, from where, and what they saw.</summary>
    public sealed class Batch
    {
        public Batch(byte version, long sender, float x, float y, float z, List<WireEvent> events)
        {
            Version = version;
            Sender = sender;
            X = x;
            Y = y;
            Z = z;
            Events = events;
        }

        public byte Version { get; }

        /// <summary>Peer id of the sender, as <c>ZNet.GetUID</c> reports it.</summary>
        public long Sender { get; }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public List<WireEvent> Events { get; }
    }

    /// <summary>
    /// The bytes a client shares. Kept as a plain byte array rather than a <c>ZPackage</c> so the
    /// format can be tested without the game, and so a malformed packet from another machine is
    /// refused here instead of throwing inside an RPC handler.
    ///
    /// Layout: version, sender, x, y, z, count, then per event the combatant, the channel, a
    /// flags byte and a mask of the damage kinds present followed by their values.
    /// </summary>
    public static class EventCodec
    {
        /// <summary>Raised whenever the layout or the meaning of a field changes.</summary>
        public const byte Version = 1;

        /// <summary>
        /// More events than half a second of a raid could produce. A packet claiming more is
        /// refused before anything is allocated for it.
        /// </summary>
        public const int MaxEvents = 2048;

        private const byte EstimatedFlag = 1;

        public static byte[] Encode(long sender, float x, float y, float z, IReadOnlyList<WireEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Version);
                writer.Write(sender);
                writer.Write(x);
                writer.Write(y);
                writer.Write(z);
                writer.Write((ushort)Math.Min(events.Count, MaxEvents));

                for (int index = 0; index < events.Count && index < MaxEvents; index++)
                {
                    WireEvent shared = events[index];
                    ushort mask = 0;
                    for (int kind = 0; kind < DamageKinds.Count; kind++)
                    {
                        if (shared.ByKind[kind] != 0f)
                        {
                            mask |= (ushort)(1 << kind);
                        }
                    }

                    writer.Write(shared.CombatantId);
                    writer.Write((byte)shared.Channel);
                    writer.Write(shared.Estimated ? EstimatedFlag : (byte)0);
                    writer.Write(mask);

                    for (int kind = 0; kind < DamageKinds.Count; kind++)
                    {
                        if ((mask & (1 << kind)) != 0)
                        {
                            writer.Write(shared.ByKind[kind]);
                        }
                    }
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Reads a packet. Returns false with a reason for anything that is not a packet this
        /// version understands; never throws.
        /// </summary>
        public static bool TryDecode(byte[]? data, out Batch? batch, out string error)
        {
            batch = null;
            error = string.Empty;

            if (data == null || data.Length == 0)
            {
                error = "empty payload";
                return false;
            }

            try
            {
                using (var stream = new MemoryStream(data, writable: false))
                using (var reader = new BinaryReader(stream))
                {
                    byte version = reader.ReadByte();
                    if (version != Version)
                    {
                        error = $"unknown version {version}, this mod speaks {Version}";
                        return false;
                    }

                    long sender = reader.ReadInt64();
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    int count = reader.ReadUInt16();

                    if (count > MaxEvents)
                    {
                        error = $"{count} events in one packet, more than the {MaxEvents} allowed";
                        return false;
                    }

                    var events = new List<WireEvent>(count);
                    for (int index = 0; index < count; index++)
                    {
                        long combatant = reader.ReadInt64();
                        byte channel = reader.ReadByte();
                        byte flags = reader.ReadByte();
                        ushort mask = reader.ReadUInt16();

                        var byKind = new float[DamageKinds.Count];
                        for (int kind = 0; kind < DamageKinds.Count; kind++)
                        {
                            if ((mask & (1 << kind)) != 0)
                            {
                                byKind[kind] = reader.ReadSingle();
                            }
                        }

                        events.Add(new WireEvent(combatant, (CombatChannel)channel, byKind, (flags & EstimatedFlag) != 0));
                    }

                    batch = new Batch(version, sender, x, y, z, events);
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                error = "the packet ends in the middle of an event";
                return false;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
