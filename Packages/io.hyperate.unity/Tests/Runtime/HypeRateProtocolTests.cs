using NUnit.Framework;

namespace HypeRate.SDK.Tests
{
    public sealed class HypeRateProtocolTests
    {
        [Test]
        public void ParsesHeartRateUpdate()
        {
            const string json = "{\"event\":\"hr_update\",\"payload\":{\"hr\":76},\"ref\":null,\"topic\":\"hr:test\"}";

            var parsed = HypeRateProtocol.TryParseHeartRate(json, out var heartRate);

            Assert.That(parsed, Is.True);
            Assert.That(heartRate, Is.EqualTo(76));
        }

        [Test]
        public void IgnoresNonHeartRateMessages()
        {
            const string json = "{\"event\":\"phx_reply\",\"payload\":{\"status\":\"ok\"},\"topic\":\"hr:test\"}";

            Assert.That(HypeRateProtocol.TryParseHeartRate(json, out _), Is.False);
        }

        [Test]
        public void EscapesDeviceIdInJoinMessage()
        {
            var json = HypeRateProtocol.CreateJoin("test\\\"device", 42);

            StringAssert.Contains("hr:test\\\\\\\"device", json);
            StringAssert.Contains("phx_join", json);
            StringAssert.Contains("42", json);
        }
    }
}
