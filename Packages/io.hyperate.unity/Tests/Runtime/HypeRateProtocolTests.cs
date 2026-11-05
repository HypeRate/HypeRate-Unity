using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void SimulationUsesTheNormalClientEvents()
        {
            var gameObject = new GameObject("HypeRate simulation test");

            try
            {
                var client = gameObject.AddComponent<HypeRateClient>();
                var receivedHeartRate = 0;
                var connected = false;
                var disconnected = false;
                client.HeartRateChanged += value => receivedHeartRate = value;
                client.Connected += () => connected = true;
                client.Disconnected += () => disconnected = true;

                client.BeginSimulation(84);

                Assert.That(client.State, Is.EqualTo(HypeRateConnectionState.Simulating));
                Assert.That(client.IsConnected, Is.True);
                Assert.That(client.IsSimulating, Is.True);
                Assert.That(client.CurrentHeartRate, Is.EqualTo(84));
                Assert.That(receivedHeartRate, Is.EqualTo(84));
                Assert.That(connected, Is.True);

                client.SetSimulatedHeartRate(132);
                Assert.That(receivedHeartRate, Is.EqualTo(132));

                client.EndSimulation();
                Assert.That(client.State, Is.EqualTo(HypeRateConnectionState.Disconnected));
                Assert.That(client.IsConnected, Is.False);
                Assert.That(disconnected, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void WidgetAcceptsHeartRateValues()
        {
            var gameObject = new GameObject("HypeRate widget test");

            try
            {
                var widget = gameObject.AddComponent<HypeRateHeartRateWidget>();
                widget.PushHeartRate(96);

                Assert.That(widget.CurrentHeartRate, Is.EqualTo(96));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
