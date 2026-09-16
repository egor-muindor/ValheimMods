using System.Collections.Generic;

namespace CombatStats.Collect
{
    /// <summary>
    /// Who set a target on fire, poisoned it or hit it with spirit damage.
    ///
    /// The game strips those three out of the blow and hands them to a status effect, whose ticks
    /// arrive later with no attacker on them. This table keeps the attacker for as long as a burn
    /// can reasonably last, so the ticks land in the right row.
    /// </summary>
    public sealed class DotAttribution
    {
        private readonly Dictionary<long, Mark> _marks = new Dictionary<long, Mark>();

        private readonly double _timeToLive;

        public DotAttribution(double timeToLive)
        {
            _timeToLive = timeToLive;
        }

        /// <summary>Targets currently remembered.</summary>
        public int Count => _marks.Count;

        /// <summary>A blow carrying fire, poison or spirit landed on <paramref name="targetId"/>.</summary>
        public void Remember(double now, long targetId, long attackerId)
        {
            _marks[targetId] = new Mark(attackerId, now + _timeToLive);
        }

        /// <summary>The attacker to charge a tick on <paramref name="targetId"/> to, if it is still known.</summary>
        public bool TryResolve(double now, long targetId, out long attackerId)
        {
            if (_marks.TryGetValue(targetId, out Mark mark) && mark.Expires >= now)
            {
                attackerId = mark.AttackerId;
                return true;
            }

            attackerId = 0L;
            return false;
        }

        /// <summary>The target is gone (it died, or it left the client's part of the world).</summary>
        public void Forget(long targetId)
        {
            _marks.Remove(targetId);
        }

        /// <summary>Drops what has expired. Called on a timer, not per hit.</summary>
        public void Prune(double now)
        {
            List<long>? expired = null;
            foreach (KeyValuePair<long, Mark> pair in _marks)
            {
                if (pair.Value.Expires < now)
                {
                    expired ??= new List<long>();
                    expired.Add(pair.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (long targetId in expired)
            {
                _marks.Remove(targetId);
            }
        }

        /// <summary>Everything is forgotten; used when a session ends.</summary>
        public void Clear()
        {
            _marks.Clear();
        }

        private readonly struct Mark
        {
            public Mark(long attackerId, double expires)
            {
                AttackerId = attackerId;
                Expires = expires;
            }

            public long AttackerId { get; }

            public double Expires { get; }
        }
    }
}
