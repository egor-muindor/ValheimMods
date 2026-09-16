namespace CombatStats.Model
{
    /// <summary>
    /// What a recorded event is about. Each channel has its own ring of buckets and its own
    /// switch in the config; only <see cref="DamageDealt"/> is on by default.
    ///
    /// The values travel on the wire, so they are fixed.
    /// </summary>
    public enum CombatChannel : byte
    {
        /// <summary>Damage a player (or their pet) dealt to a creature.</summary>
        DamageDealt = 0,

        /// <summary>Damage a player took, from anything.</summary>
        DamageTaken = 1,

        /// <summary>Health a player regained: food, potions, magic.</summary>
        Healing = 2,

        /// <summary>Damage dealt to trees, ore, buildings and other things that are not creatures.</summary>
        ObjectDamage = 3,
    }
}
