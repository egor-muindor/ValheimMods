using System.Collections.Generic;
using Muindor.ServerConfig;
using Xunit;

namespace Muindor.ServerConfig.Tests
{
    public class ConfigPayloadTests
    {
        private static ConfigPayload RoundTrip(ConfigPayload payload)
        {
            Assert.True(ConfigPayload.TryParse(payload.Serialize(), out ConfigPayload? parsed, out string error), error);
            return parsed!;
        }

        [Fact]
        public void KeepsVersionPriorityAndValues()
        {
            var payload = new ConfigPayload("1.2.3", priority: true, new[]
            {
                new KeyValuePair<string, string>("General.Enabled", "true"),
                new KeyValuePair<string, string>("Skills.SkillReduceFactor", "0.25"),
            });

            ConfigPayload parsed = RoundTrip(payload);

            Assert.Equal("1.2.3", parsed.ModVersion);
            Assert.True(parsed.Priority);
            Assert.Equal(2, parsed.Values.Count);
            Assert.Equal("General.Enabled", parsed.Values[0].Key);
            Assert.Equal("true", parsed.Values[0].Value);
            Assert.Equal("Skills.SkillReduceFactor", parsed.Values[1].Key);
            Assert.Equal("0.25", parsed.Values[1].Value);
        }

        [Fact]
        public void CarriesNoValuesWhenThePriorityIsOff()
        {
            ConfigPayload parsed = RoundTrip(new ConfigPayload("1.0.0", priority: false));

            Assert.False(parsed.Priority);
            Assert.Empty(parsed.Values);
        }

        [Theory]
        // The ore and name lists are free text: separators, equals signs and backslashes all occur.
        [InlineData("CopperOre=C, TinOre=T")]
        [InlineData("a\\b")]
        [InlineData("line one\nline two")]
        [InlineData("carriage\rreturn")]
        [InlineData("")]
        [InlineData("=")]
        [InlineData("\\n")]
        public void KeepsValuesThatLookLikeTheFormatItself(string value)
        {
            var payload = new ConfigPayload("1.0.0", priority: true, new[]
            {
                new KeyValuePair<string, string>("Names.Names", value),
                new KeyValuePair<string, string>("General.Enabled", "true"),
            });

            ConfigPayload parsed = RoundTrip(payload);

            Assert.Equal(2, parsed.Values.Count);
            Assert.Equal(value, parsed.Values[0].Value);
            Assert.Equal("true", parsed.Values[1].Value);
        }

        [Fact]
        public void KeepsKeysThatContainASeparator()
        {
            var payload = new ConfigPayload("1.0.0", priority: true, new[]
            {
                new KeyValuePair<string, string>("Odd.Key=With=Equals", "value=with=equals"),
            });

            ConfigPayload parsed = RoundTrip(payload);

            Assert.Equal("Odd.Key=With=Equals", parsed.Values[0].Key);
            Assert.Equal("value=with=equals", parsed.Values[0].Value);
        }

        [Fact]
        public void RejectsAPacketFromSomethingElse()
        {
            Assert.False(ConfigPayload.TryParse("hello\nworld\nagain", out ConfigPayload? parsed, out string error));
            Assert.Null(parsed);
            Assert.Contains("unknown format", error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RejectsAnEmptyPacket(string? text)
        {
            Assert.False(ConfigPayload.TryParse(text, out ConfigPayload? parsed, out string error));
            Assert.Null(parsed);
            Assert.NotEmpty(error);
        }

        [Fact]
        public void RejectsATruncatedPacket()
        {
            Assert.False(ConfigPayload.TryParse(ConfigPayload.Header + "\n1.0.0", out ConfigPayload? parsed, out string error));
            Assert.Null(parsed);
            Assert.Contains("truncated", error);
        }

        [Fact]
        public void RejectsAnUnreadablePriorityFlag()
        {
            Assert.False(ConfigPayload.TryParse(ConfigPayload.Header + "\n1.0.0\nmaybe\n", out ConfigPayload? parsed, out string error));
            Assert.Null(parsed);
            Assert.Contains("priority flag", error);
        }

        [Fact]
        public void RejectsALineWithoutASeparator()
        {
            Assert.False(ConfigPayload.TryParse(ConfigPayload.Header + "\n1.0.0\ntrue\nGeneral.Enabled\n", out ConfigPayload? parsed, out string error));
            Assert.Null(parsed);
            Assert.Contains("separator", error);
        }

        [Fact]
        public void RejectsAnEmptyKey()
        {
            Assert.False(ConfigPayload.TryParse(ConfigPayload.Header + "\n1.0.0\ntrue\n=value\n", out ConfigPayload? parsed, out string error));
            Assert.Null(parsed);
            Assert.Contains("empty key", error);
        }

        [Fact]
        public void SurvivesATransportThatAddsCarriageReturns()
        {
            string text = new ConfigPayload("1.0.0", priority: true, new[]
            {
                new KeyValuePair<string, string>("General.Enabled", "false"),
            }).Serialize().Replace("\n", "\r\n");

            Assert.True(ConfigPayload.TryParse(text, out ConfigPayload? parsed, out string error), error);
            Assert.Equal("1.0.0", parsed!.ModVersion);
            Assert.Equal("false", parsed.Values[0].Value);
        }
    }
}
