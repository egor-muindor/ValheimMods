using System.Text.RegularExpressions;
using CombatStats.Model;
using Xunit;

namespace CombatStats.Tests
{
    /// <summary>The damage types the game deals in, and how the meter names and colours them.</summary>
    public class DamageKindTests
    {
        [Fact]
        public void EveryKindTheGameSplitsAHitIntoIsCounted()
        {
            Assert.Equal(11, DamageKinds.Count);
            Assert.Equal(11, DamageKinds.All.Length);
        }

        [Fact]
        public void EveryKindHasAKeyANameAndAColour()
        {
            foreach (DamageKind kind in DamageKinds.All)
            {
                Assert.StartsWith("$inventory_", DamageKinds.LocalizationKey(kind));
                Assert.Matches(new Regex("^#[0-9a-f]{6}$"), DamageKinds.ColorHex(kind));
                Assert.False(string.IsNullOrWhiteSpace(DamageKinds.EnglishName(kind)));
            }
        }

        [Fact]
        public void EveryKindIsItsOwnIndexIntoAnArray()
        {
            for (int index = 0; index < DamageKinds.All.Length; index++)
            {
                Assert.Equal(index, (int)DamageKinds.All[index]);
            }
        }

        [Fact]
        public void FirePoisonAndSpiritTickOverTime()
        {
            Assert.True(DamageKinds.IsOverTime(DamageKind.Fire));
            Assert.True(DamageKinds.IsOverTime(DamageKind.Poison));
            Assert.True(DamageKinds.IsOverTime(DamageKind.Spirit));

            Assert.False(DamageKinds.IsOverTime(DamageKind.Slash));
            Assert.False(DamageKinds.IsOverTime(DamageKind.Frost));
            Assert.False(DamageKinds.IsOverTime(DamageKind.Lightning));
            Assert.False(DamageKinds.IsOverTime(DamageKind.Generic));
        }

        [Fact]
        public void ChoppingAndMiningAreToolDamage()
        {
            Assert.True(DamageKinds.IsToolDamage(DamageKind.Chop));
            Assert.True(DamageKinds.IsToolDamage(DamageKind.Pickaxe));
            Assert.False(DamageKinds.IsToolDamage(DamageKind.Blunt));
        }

        [Fact]
        public void NoTwoKindsShareAColour()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (DamageKind kind in DamageKinds.All)
            {
                Assert.True(seen.Add(DamageKinds.ColorHex(kind)), $"{kind} repeats a colour");
            }
        }
    }
}
