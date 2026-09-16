using UnityEngine;

namespace CombatStats.Collect
{
    /// <summary>
    /// Turning the characters of a hit into the rows of the meter.
    ///
    /// A player is keyed by the user id half of their character's ZDOID. That id is the session id
    /// of the client the character belongs to, which is also the peer id every other client knows
    /// them by, so the same player has the same key on every machine without sending a single
    /// extra byte. A creature that belongs to nobody is keyed by a negative number derived from
    /// its prefab name, which cannot collide with a session id.
    /// </summary>
    internal static class Attribution
    {
        /// <summary>The key of a target, unique enough to hang the damage-over-time table on.</summary>
        public static long TargetKey(ZDOID id)
        {
            unchecked
            {
                return (id.UserID * 486187739L) + id.ID;
            }
        }

        /// <summary>The combatant behind a character, if the meter has a row for it.</summary>
        public static bool TryCombatant(Character? character, bool countPets, out long id, out string name)
        {
            id = 0L;
            name = string.Empty;

            if (character == null)
            {
                return false;
            }

            if (character.IsPlayer())
            {
                id = character.GetZDOID().UserID;
                name = character is Player player ? player.GetPlayerName() : Localize(character.m_name);
                return true;
            }

            if (!countPets)
            {
                return false;
            }

            Player? owner = OwnerOf(character);
            if (owner != null)
            {
                id = owner.GetZDOID().UserID;
                name = owner.GetPlayerName();
                return true;
            }

            // Only something that belongs to a player has a row. Without this a raid would fill
            // the meter with the names of whatever is attacking it.
            if (!character.IsTamed())
            {
                return false;
            }

            id = CreatureKey(character.m_name);
            name = Localize(character.m_name);
            return true;
        }

        /// <summary>The player a tamed creature or a summon follows, when there is one.</summary>
        public static Player? OwnerOf(Character character)
        {
            BaseAI ai = character.GetBaseAI();
            if (!(ai is MonsterAI monster))
            {
                return null;
            }

            GameObject follow = monster.GetFollowTarget();
            return follow != null ? follow.GetComponent<Player>() : null;
        }

        /// <summary>
        /// Where the keys of creatures start. A session id is
        /// <c>(long)"host:domain".GetHashCode() + Random.Range(1, int.MaxValue)</c>
        /// (<c>Utils.GenerateUID</c>), so it can be negative and reaches about -2^31: creature
        /// keys have to live well below that to stay out of its way.
        /// </summary>
        private const long CreatureFloor = -(1L << 40);

        /// <summary>
        /// The key of a creature that belongs to a player but is not a player: the same on every
        /// client, because it only depends on the name, and far out of the range a session id can
        /// reach.
        /// </summary>
        public static long CreatureKey(string name)
        {
            unchecked
            {
                return CreatureFloor - (uint)(name ?? string.Empty).GetStableHashCode();
            }
        }

        /// <summary>True for a key handed out by <see cref="CreatureKey"/>.</summary>
        public static bool IsCreature(long id)
        {
            return id <= CreatureFloor;
        }

        /// <summary>The player's own name for a creature, or the raw token when the game has none.</summary>
        public static string Localize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "?";
            }

            Localization localization = Localization.instance;
            return localization != null ? localization.Localize(name) : name;
        }
    }
}
