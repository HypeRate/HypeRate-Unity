namespace HypeRate
{
    public static class Network
    {
        public static string GetJoinPacket(string channelName, int @ref)
        {
            return "{\"topic\": \"" + channelName + "\", \"event\": \"phx_join\", \"payload\": {}, \"ref\": "+ @ref + "}";
        }

        public static string GetLeavePacket(string channelName, int @ref)
        {
            return "{\"topic\": \"" + channelName + "\", \"event\": \"phx_leave\", \"payload\": {}, \"ref\": " + @ref + "}";
        }

        public static string GetKeepAlivePacket()
        {
            return "{\"topic\": \"phoenix\",\"event\": \"heartbeat\",\"payload\": {},\"ref\": 0}";
        }
    }
}