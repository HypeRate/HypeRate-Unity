using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HypeRate
{
    public class HypeRate
    {
        private HypeRate() { }

        private static HypeRate _instance;

        private ClientWebSocket webSocket = new ClientWebSocket();

        private Channels _channels = new();

        public delegate void OnMessageReceived(string result);
        public OnMessageReceived onMessageReceivedCallback;

        public static HypeRate GetInstance()
        {
            if (_instance == null)
            {
                _instance = new HypeRate();
            }
            return _instance;
        }

        public async Task ConnectToServer(string websocketToken, string hyperateURL = "wss://app.hyperate.io/socket/websocket")
        {
            Uri serverUri = new Uri(hyperateURL + "?token=" + websocketToken);
            await webSocket.ConnectAsync(serverUri, CancellationToken.None);

            Debug.Log("Connection open!");
            // try to join all channels that where defined before. This is the case on a reconnect
            foreach (var channelName in _channels.GetChannelsToJoin())
            {
                var refArg = _channels.AddJoiningChannel(channelName);
                await SendMessage(Network.GetJoinPacket(channelName, refArg));
            }
            CreateMessageReciever();
        }

        private void CreateMessageReciever()
        {
            _ = Task.Run(async () =>
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        string message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        Debug.Log("Received message: " + message);
                        try {
                            onMessageReceivedCallback(message);
                        }
                        catch (Exception) {
                            // do nothing
                        }
                    }
                    while (!result.EndOfMessage);
                }
            });
        }

        public async Task CloseConnection()
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
            _channels.HandleReconnect();
            Debug.Log("Connection closed!");
        }

        // Send a message to the WebSocket server
        public async Task SendMessage(string message)
        {
            ArraySegment<byte> sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
            Debug.Log("Sent message: " + message);
        }

        public async Task JoinHeartbeatChannel(string deviceId)
        {
            var channelName = String.Format("hr:{0}", deviceId);
            var refArg = _channels.AddJoiningChannel(channelName);

            await SendMessage(Network.GetJoinPacket(channelName, refArg));
        }

        public async Task LeaveHeartbeatChannel(string deviceId)
        {
            var channelName = String.Format("hr:{0}", deviceId);
            var refArg = _channels.AddLeavingChannel(channelName);

            await SendMessage(Network.GetLeavePacket(channelName, refArg));
        }

        public async Task JoinClipsChannel(string deviceId)
        {
            var channelName = String.Format("clips:{0}", deviceId);
            var refArg = _channels.AddJoiningChannel(channelName);

            await SendMessage(Network.GetJoinPacket(channelName, refArg));
        }

        public async Task LeaveClipsChannel(string deviceId)
        {
            var channelName = String.Format("clips:{0}", deviceId);
            var refArg = _channels.AddLeavingChannel(channelName);

            await SendMessage(Network.GetLeavePacket(channelName, refArg));
        }

        public static ChannelType DetermineChannelType(string input)
        {
            if (input.StartsWith("hr:"))
            {
                return ChannelType.Heartrate;
            }

            if (input.StartsWith("clips:"))
            {
                return ChannelType.Clip;
            }

            return ChannelType.Unknown;
        }

        public static string ExtractDeviceIdFromChannelName(string input)
        {
            if (input.StartsWith("hr:"))
            {
                return input[3..];
            }

            if (input.StartsWith("clips:"))
            {
                return input[6..];
            }

            return input;
        }
    }
}