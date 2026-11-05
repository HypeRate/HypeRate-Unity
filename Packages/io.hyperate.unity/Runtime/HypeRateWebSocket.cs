using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HypeRate.SDK
{
    internal sealed class HypeRateWebSocket : IDisposable
    {
        private readonly SynchronizationContext synchronizationContext;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        private ClientWebSocket socket;
        private CancellationTokenSource cancellation;
        private bool closedRaised;
        private bool disposed;

        public HypeRateWebSocket()
        {
            synchronizationContext = SynchronizationContext.Current;
        }

        public event Action Opened;
        public event Action<string> MessageReceived;
        public event Action<string> Error;
        public event Action<WebSocketCloseStatus?> Closed;

        public bool IsOpen => socket != null && socket.State == WebSocketState.Open;
        public bool IsConnecting => socket != null && socket.State == WebSocketState.Connecting;

        public async Task ConnectAsync(Uri uri)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException(
                "WebGL requires a browser WebSocket transport and is not supported in this preview release.");
#else
            if (socket != null && socket.State != WebSocketState.Closed)
            {
                throw new InvalidOperationException("The WebSocket is already active.");
            }

            closedRaised = false;
            cancellation = new CancellationTokenSource();
            socket = new ClientWebSocket();

            try
            {
                await socket.ConnectAsync(uri, cancellation.Token).ConfigureAwait(false);
                Post(() => Opened?.Invoke());
                await ReceiveLoopAsync(cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // An intentional disconnect cancels the receive loop.
            }
            catch (Exception exception)
            {
                Post(() => Error?.Invoke(exception.Message));
            }
            finally
            {
                var closeStatus = socket?.CloseStatus;
                RaiseClosed(closeStatus);
                socket?.Dispose();
                socket = null;
            }
#endif
        }

        public async Task SendTextAsync(string message)
        {
            if (string.IsNullOrEmpty(message) || !IsOpen)
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            await sendLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (IsOpen)
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        cancellation.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async Task CloseAsync(string reason)
        {
            var currentSocket = socket;
            var currentCancellation = cancellation;
            if (currentSocket == null)
            {
                return;
            }

            try
            {
                if (currentSocket.State == WebSocketState.Open ||
                    currentSocket.State == WebSocketState.CloseReceived)
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                    {
                        await currentSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            reason,
                            timeout.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The close handshake timed out; cancellation below ends the receive loop.
            }
            finally
            {
                currentCancellation?.Cancel();
            }
        }

        public void CancelConnection()
        {
            cancellation?.Cancel();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            socket?.Dispose();
            socket = null;
            sendLock.Dispose();
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[4096];

            while (socket != null && socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                using (var messageBuffer = new MemoryStream())
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            if (socket.State == WebSocketState.CloseReceived)
                            {
                                await socket.CloseOutputAsync(
                                    WebSocketCloseStatus.NormalClosure,
                                    "Close acknowledged",
                                    CancellationToken.None).ConfigureAwait(false);
                            }

                            return;
                        }

                        if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                        {
                            messageBuffer.Write(buffer, 0, result.Count);
                        }
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text && messageBuffer.Length > 0)
                    {
                        var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        Post(() => MessageReceived?.Invoke(message));
                    }
                }
            }
        }
#endif

        private void RaiseClosed(WebSocketCloseStatus? closeStatus)
        {
            if (closedRaised)
            {
                return;
            }

            closedRaised = true;
            Post(() => Closed?.Invoke(closeStatus));
        }

        private void Post(Action action)
        {
            if (synchronizationContext == null)
            {
                action();
                return;
            }

            synchronizationContext.Post(_ => action(), null);
        }
    }
}
