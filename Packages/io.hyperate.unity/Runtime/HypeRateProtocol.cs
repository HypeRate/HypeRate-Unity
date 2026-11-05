using System;
using UnityEngine;

namespace HypeRate.SDK
{
    internal static class HypeRateProtocol
    {
        [Serializable]
        private sealed class EmptyPayload
        {
        }

        [Serializable]
        private sealed class OutgoingMessage
        {
            public string topic;
            public string @event;
            public EmptyPayload payload = new EmptyPayload();
            public int @ref;
        }

        [Serializable]
        private sealed class IncomingPayload
        {
            public int hr;
            public string status;
        }

        [Serializable]
        private sealed class IncomingMessage
        {
            public string topic;
            public string @event;
            public IncomingPayload payload;
        }

        public static string CreateJoin(string deviceId, int reference)
        {
            return CreateChannelMessage($"hr:{deviceId}", "phx_join", reference);
        }

        public static string CreateLeave(string deviceId, int reference)
        {
            return CreateChannelMessage($"hr:{deviceId}", "phx_leave", reference);
        }

        public static string CreateHeartbeat(int reference)
        {
            return CreateChannelMessage("phoenix", "heartbeat", reference);
        }

        public static bool TryParseHeartRate(string json, out int heartRate)
        {
            heartRate = 0;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var message = JsonUtility.FromJson<IncomingMessage>(json);
                if (message == null || message.payload == null)
                {
                    return false;
                }

                if (message.@event != "hr_update" && message.@event != "hr_feed")
                {
                    return false;
                }

                if (message.payload.hr <= 0)
                {
                    return false;
                }

                heartRate = message.payload.hr;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string CreateChannelMessage(string topic, string eventName, int reference)
        {
            return JsonUtility.ToJson(new OutgoingMessage
            {
                topic = topic,
                @event = eventName,
                @ref = reference
            });
        }
    }
}
