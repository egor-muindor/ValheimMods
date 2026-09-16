using System.Collections.Generic;
using CombatStats.Model;
using CombatStats.Net;
using Xunit;

namespace CombatStats.Tests
{
    /// <summary>
    /// The bytes a client sends when it shares what it saw. A packet arrives from another
    /// machine, so a malformed one must be refused rather than throw inside an RPC handler.
    /// </summary>
    public class EventCodecTests
    {
        private static WireEvent Event(long combatant, CombatChannel channel, DamageKind kind, float amount)
        {
            var byKind = new float[DamageKinds.Count];
            byKind[(int)kind] = amount;
            return new WireEvent(combatant, channel, byKind, estimated: false);
        }

        [Fact]
        public void ARoundTripKeepsEveryValue()
        {
            var events = new List<WireEvent>
            {
                Event(7, CombatChannel.DamageDealt, DamageKind.Slash, 31.5f),
                Event(9, CombatChannel.Healing, DamageKind.Blunt, 12f),
            };

            byte[] data = EventCodec.Encode(4242L, 1f, 2f, 3f, events);

            Assert.True(EventCodec.TryDecode(data, out Batch? batch, out string error), error);
            Assert.Equal(EventCodec.Version, batch!.Version);
            Assert.Equal(4242L, batch.Sender);
            Assert.Equal(1f, batch.X, 3);
            Assert.Equal(2f, batch.Y, 3);
            Assert.Equal(3f, batch.Z, 3);
            Assert.Equal(2, batch.Events.Count);
            Assert.Equal(7L, batch.Events[0].CombatantId);
            Assert.Equal(CombatChannel.DamageDealt, batch.Events[0].Channel);
            Assert.Equal(31.5f, batch.Events[0].ByKind[(int)DamageKind.Slash], 3);
            Assert.Equal(CombatChannel.Healing, batch.Events[1].Channel);
            Assert.Equal(12f, batch.Events[1].ByKind[(int)DamageKind.Blunt], 3);
        }

        [Fact]
        public void OnlyTheKindsThatCarryDamageAreWritten()
        {
            var one = new List<WireEvent> { Event(7, CombatChannel.DamageDealt, DamageKind.Slash, 10f) };

            var twoKinds = new float[DamageKinds.Count];
            twoKinds[(int)DamageKind.Slash] = 10f;
            twoKinds[(int)DamageKind.Fire] = 5f;
            var two = new List<WireEvent> { new WireEvent(7, CombatChannel.DamageDealt, twoKinds, estimated: false) };

            Assert.Equal(4, EventCodec.Encode(1L, 0f, 0f, 0f, two).Length - EventCodec.Encode(1L, 0f, 0f, 0f, one).Length);
        }

        [Fact]
        public void TheEstimateFlagSurvives()
        {
            var byKind = new float[DamageKinds.Count];
            byKind[(int)DamageKind.Fire] = 3f;
            var events = new List<WireEvent> { new WireEvent(7, CombatChannel.DamageDealt, byKind, estimated: true) };

            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, events);

            Assert.True(EventCodec.TryDecode(data, out Batch? batch, out _));
            Assert.True(batch!.Events[0].Estimated);
        }

        [Fact]
        public void AnEmptyBatchRoundTrips()
        {
            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, new List<WireEvent>());

            Assert.True(EventCodec.TryDecode(data, out Batch? batch, out string error), error);
            Assert.Empty(batch!.Events);
        }

        [Fact]
        public void AnUnknownVersionIsRejected()
        {
            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, new List<WireEvent>());
            data[0] = 99;

            Assert.False(EventCodec.TryDecode(data, out Batch? batch, out string error));
            Assert.Null(batch);
            Assert.Contains("version", error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ATruncatedPayloadIsRejectedWithoutThrowing()
        {
            var events = new List<WireEvent> { Event(7, CombatChannel.DamageDealt, DamageKind.Slash, 10f) };
            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, events);
            var cut = new byte[data.Length - 3];
            System.Array.Copy(data, cut, cut.Length);

            Assert.False(EventCodec.TryDecode(cut, out Batch? batch, out string error));
            Assert.Null(batch);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void EveryChannelThisVersionHasIsAccepted()
        {
            var events = new List<WireEvent> { Event(7, CombatChannel.ObjectDamage, DamageKind.Chop, 20f) };

            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, events);

            Assert.True(EventCodec.TryDecode(data, out Batch? batch, out string error), error);
            Assert.Equal(CombatChannel.ObjectDamage, batch!.Events[0].Channel);
        }

        [Fact]
        public void AChannelThisVersionDoesNotHaveIsRejected()
        {
            var events = new List<WireEvent> { Event(7, CombatChannel.DamageDealt, DamageKind.Slash, 10f) };
            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, events);

            // version, sender, three floats, count, then the combatant of the first event.
            data[1 + 8 + 12 + 2 + 8] = 7;

            Assert.False(EventCodec.TryDecode(data, out Batch? batch, out string error));
            Assert.Null(batch);
            Assert.Contains("meter", error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ACountTheRestOfThePacketCannotHoldIsRefused()
        {
            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, new List<WireEvent>());
            int countOffset = 1 + 8 + 12;
            data[countOffset] = 0x00;
            data[countOffset + 1] = 0x08; // 2048 events, the most allowed, in an empty packet

            Assert.False(EventCodec.TryDecode(data, out Batch? batch, out string error));
            Assert.Null(batch);
            Assert.Contains("fit", error, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MoreEventsThanFitAreTruncatedAndTheCountSaysSo()
        {
            var events = new List<WireEvent>();
            for (int index = 0; index < EventCodec.MaxEvents + 10; index++)
            {
                events.Add(Event(index, CombatChannel.DamageDealt, DamageKind.Slash, 1f));
            }

            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, events);

            Assert.True(EventCodec.TryDecode(data, out Batch? batch, out string error), error);
            Assert.Equal(EventCodec.MaxEvents, batch!.Events.Count);
        }

        [Fact]
        public void EmptyBytesAreRejectedWithoutThrowing()
        {
            Assert.False(EventCodec.TryDecode(new byte[0], out _, out _));
            Assert.False(EventCodec.TryDecode(null, out _, out _));
        }

        [Fact]
        public void AnAbsurdEventCountIsRefusedBeforeAllocating()
        {
            byte[] data = EventCodec.Encode(1L, 0f, 0f, 0f, new List<WireEvent>());
            // The count is the last field of the header: version, sender, three floats.
            int countOffset = 1 + 8 + 12;
            data[countOffset] = 0xFF;
            data[countOffset + 1] = 0xFF;

            Assert.False(EventCodec.TryDecode(data, out _, out string error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }
}
