using System;
using System.Collections;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace HypeRate.SDK
{
    [AddComponentMenu("HypeRate/HypeRate Client")]
    [DisallowMultipleComponent]
    [HelpURL("https://hyperate.io/api")]
    public sealed class HypeRateClient : MonoBehaviour
    {
        [Serializable]
        public sealed class HeartRateUnityEvent : UnityEvent<int>
        {
        }

        [Serializable]
        public sealed class ErrorUnityEvent : UnityEvent<string>
        {
        }

        [Header("Connection")]
        [SerializeField] private bool connectOnStart = true;
        [SerializeField] private bool autoReconnect = true;
        [SerializeField, Min(5f)] private float heartbeatInterval = 10f;
        [SerializeField, Min(1f)] private float maximumReconnectDelay = 30f;

        [Header("Events")]
        [SerializeField] private HeartRateUnityEvent onHeartRateChanged = new HeartRateUnityEvent();
        [SerializeField] private UnityEvent onConnected = new UnityEvent();
        [SerializeField] private UnityEvent onDisconnected = new UnityEvent();
        [SerializeField] private ErrorUnityEvent onError = new ErrorUnityEvent();

        private HypeRateWebSocket socket;
        private Coroutine heartbeatRoutine;
        private Coroutine reconnectRoutine;
        private bool intentionalDisconnect;
        private bool quitting;
        private bool destroyed;
        private bool simulationActive;
        private bool disconnectEventRaised = true;
        private int connectionGeneration;
        private int reconnectAttempt;
        private int messageReference;
        private string runtimeApiToken = string.Empty;
        private string runtimeDeviceId = string.Empty;
        private string runtimeEndpoint = HypeRateSettings.DefaultEndpoint;
        private string activeDeviceId = string.Empty;

        public event Action<int> HeartRateChanged;
        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> Error;

        public HypeRateConnectionState State { get; private set; } = HypeRateConnectionState.Disconnected;
        public int CurrentHeartRate { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public bool IsConnected => State == HypeRateConnectionState.Connected || simulationActive;
        public bool IsSimulating => simulationActive;
        public bool HasConfiguration
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(runtimeApiToken) && !string.IsNullOrWhiteSpace(runtimeDeviceId))
                {
                    return true;
                }

#if UNITY_EDITOR
                var editorSettings = HypeRateSettings.LoadEditorSettings();
                return editorSettings != null && editorSettings.IsConfigured;
#else
                return false;
#endif
            }
        }
        public HeartRateUnityEvent OnHeartRateChanged => onHeartRateChanged;
        public UnityEvent OnConnected => onConnected;
        public UnityEvent OnDisconnected => onDisconnected;
        public ErrorUnityEvent OnError => onError;

        public void Configure(string apiToken, string deviceId, string endpoint = HypeRateSettings.DefaultEndpoint)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                throw new ArgumentException("A HypeRate API token is required.", nameof(apiToken));
            }

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("A HypeRate Device ID is required.", nameof(deviceId));
            }

            var normalizedEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? HypeRateSettings.DefaultEndpoint
                : endpoint.Trim();

            if (!TryCreateWebSocketUri(normalizedEndpoint, out _))
            {
                throw new ArgumentException("The HypeRate endpoint must be a valid ws:// or wss:// URL.", nameof(endpoint));
            }

            runtimeApiToken = apiToken.Trim();
            runtimeDeviceId = deviceId.Trim();
            runtimeEndpoint = normalizedEndpoint;
        }

        public void ClearConfiguration()
        {
            runtimeApiToken = string.Empty;
            runtimeDeviceId = string.Empty;
            runtimeEndpoint = HypeRateSettings.DefaultEndpoint;
        }

        public void BeginSimulation(int initialHeartRate = 72)
        {
            if (initialHeartRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialHeartRate), "Heart rate must be greater than zero.");
            }

            if (destroyed || quitting)
            {
                return;
            }

            var wasConnected = socket != null || simulationActive || !disconnectEventRaised;
            intentionalDisconnect = true;
            connectionGeneration++;
            StopHeartbeatRoutine();
            StopReconnectRoutine();

            var currentSocket = socket;
            socket = null;
            currentSocket?.CancelConnection();
            currentSocket?.Dispose();

            if (wasConnected)
            {
                RaiseDisconnectedOnce();
            }

            simulationActive = true;
            intentionalDisconnect = false;
            disconnectEventRaised = false;
            LastError = string.Empty;
            SetState(HypeRateConnectionState.Simulating);
            Connected?.Invoke();
            onConnected.Invoke();
            SetSimulatedHeartRate(initialHeartRate);
        }

        public void SetSimulatedHeartRate(int heartRate)
        {
            if (!simulationActive)
            {
                throw new InvalidOperationException("Call BeginSimulation before sending simulated heart-rate values.");
            }

            if (heartRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heartRate), "Heart rate must be greater than zero.");
            }

            PublishHeartRate(heartRate);
        }

        public void EndSimulation()
        {
            if (!simulationActive)
            {
                return;
            }

            simulationActive = false;
            CurrentHeartRate = 0;
            SetState(HypeRateConnectionState.Disconnected);
            RaiseDisconnectedOnce();
        }

        private void Start()
        {
            if (connectOnStart)
            {
                Connect();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying && !quitting && !destroyed)
            {
                Disconnect();
            }
        }

        private void OnDestroy()
        {
            destroyed = true;
            intentionalDisconnect = true;
            simulationActive = false;
            connectionGeneration++;
            StopHeartbeatRoutine();
            StopReconnectRoutine();

            var currentSocket = socket;
            socket = null;
            currentSocket?.CancelConnection();
            currentSocket?.Dispose();
        }

        private void OnApplicationQuit()
        {
            quitting = true;
            intentionalDisconnect = true;
            simulationActive = false;
            connectionGeneration++;
            StopHeartbeatRoutine();
            StopReconnectRoutine();

            var currentSocket = socket;
            socket = null;
            currentSocket?.CancelConnection();
            currentSocket?.Dispose();
        }

        public void Connect()
        {
            _ = ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            if (destroyed || quitting)
            {
                return;
            }

            if (socket != null && (socket.IsOpen || socket.IsConnecting))
            {
                return;
            }

            if (simulationActive)
            {
                EndSimulation();
            }

            if (!TryResolveConfiguration(out var configuration, out var configurationError))
            {
                ReportError(configurationError);
                return;
            }

            if (!TryCreateWebSocketUri(configuration.Endpoint, out var endpoint))
            {
                ReportError("The configured HypeRate endpoint must be a valid ws:// or wss:// URL.");
                return;
            }

            StopReconnectRoutine();
            intentionalDisconnect = false;
            disconnectEventRaised = false;
            LastError = string.Empty;
            SetState(reconnectAttempt > 0 ? HypeRateConnectionState.Reconnecting : HypeRateConnectionState.Connecting);

            var generation = ++connectionGeneration;
            activeDeviceId = configuration.DeviceId;
            var connectionUrl = BuildConnectionUrl(endpoint.ToString(), configuration.ApiToken);
            var newSocket = new HypeRateWebSocket();
            socket = newSocket;

            newSocket.Opened += () => _ = HandleOpenAsync(newSocket, generation);
            newSocket.MessageReceived += message => HandleMessage(newSocket, generation, message);
            newSocket.Error += message => HandleError(newSocket, generation, message);
            newSocket.Closed += code => HandleClose(newSocket, generation, code);

            try
            {
                await newSocket.ConnectAsync(new Uri(connectionUrl));
            }
            catch (Exception exception)
            {
                if (IsCurrentConnection(newSocket, generation))
                {
                    ReportError(exception.Message);
                    HandleClose(newSocket, generation, null);
                }
            }
        }

        public void Disconnect()
        {
            _ = DisconnectAsync();
        }

        public async Task DisconnectAsync()
        {
            intentionalDisconnect = true;
            StopReconnectRoutine();
            StopHeartbeatRoutine();

            if (simulationActive)
            {
                EndSimulation();
                return;
            }

            var currentSocket = socket;
            socket = null;
            connectionGeneration++;

            if (currentSocket == null)
            {
                SetState(HypeRateConnectionState.Disconnected);
                RaiseDisconnectedOnce();
                return;
            }

            SetState(HypeRateConnectionState.Disconnecting);

            try
            {
                if (currentSocket.IsOpen)
                {
                    if (!string.IsNullOrWhiteSpace(activeDeviceId))
                    {
                        await currentSocket.SendTextAsync(HypeRateProtocol.CreateLeave(activeDeviceId, NextReference()));
                    }

                    await currentSocket.CloseAsync("Client disconnect");
                }
                else if (currentSocket.IsConnecting)
                {
                    currentSocket.CancelConnection();
                }
            }
            catch (Exception exception)
            {
                if (!destroyed)
                {
                    ReportError($"Could not close the HypeRate connection cleanly: {exception.Message}");
                }
            }
            finally
            {
                currentSocket.Dispose();
                if (!destroyed)
                {
                    SetState(HypeRateConnectionState.Disconnected);
                    RaiseDisconnectedOnce();
                }
            }
        }

        private async Task HandleOpenAsync(HypeRateWebSocket source, int generation)
        {
            if (destroyed || !IsCurrentConnection(source, generation))
            {
                return;
            }

            reconnectAttempt = 0;
            SetState(HypeRateConnectionState.Connected);
            Connected?.Invoke();
            onConnected.Invoke();
            StartHeartbeatRoutine(generation);

            if (string.IsNullOrWhiteSpace(activeDeviceId))
            {
                ReportError("The HypeRate Device ID is unavailable for this connection.");
                return;
            }

            await SendTextAsync(source, HypeRateProtocol.CreateJoin(activeDeviceId, NextReference()));
        }

        private void HandleMessage(HypeRateWebSocket source, int generation, string json)
        {
            if (destroyed || !IsCurrentConnection(source, generation) || string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            if (!HypeRateProtocol.TryParseHeartRate(json, out var heartRate))
            {
                return;
            }

            PublishHeartRate(heartRate);
        }

        private void PublishHeartRate(int heartRate)
        {
            CurrentHeartRate = heartRate;
            HeartRateChanged?.Invoke(heartRate);
            onHeartRateChanged.Invoke(heartRate);
        }

        private void HandleError(HypeRateWebSocket source, int generation, string message)
        {
            if (destroyed || !IsCurrentConnection(source, generation) || intentionalDisconnect)
            {
                return;
            }

            ReportError(string.IsNullOrWhiteSpace(message) ? "Unknown WebSocket error." : message);
        }

        private void HandleClose(HypeRateWebSocket source, int generation, WebSocketCloseStatus? closeCode)
        {
            if (destroyed || !IsCurrentConnection(source, generation))
            {
                return;
            }

            socket = null;
            StopHeartbeatRoutine();
            source.Dispose();
            SetState(HypeRateConnectionState.Disconnected);
            RaiseDisconnectedOnce();

            if (!intentionalDisconnect && !quitting && autoReconnect && isActiveAndEnabled)
            {
                ScheduleReconnect(closeCode);
            }
        }

        private IEnumerator HeartbeatLoop(int generation)
        {
            var wait = new WaitForSecondsRealtime(Mathf.Max(5f, heartbeatInterval));

            while (generation == connectionGeneration)
            {
                yield return wait;

                if (generation != connectionGeneration || socket == null || !socket.IsOpen)
                {
                    yield break;
                }

                _ = SendTextAsync(socket, HypeRateProtocol.CreateHeartbeat(NextReference()));
            }
        }

        private IEnumerator ReconnectAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            reconnectRoutine = null;

            if (!intentionalDisconnect && !quitting && isActiveAndEnabled)
            {
                _ = ConnectAsync();
            }
        }

        private void ScheduleReconnect(WebSocketCloseStatus? closeCode)
        {
            if (reconnectRoutine != null)
            {
                return;
            }

            reconnectAttempt++;
            var delay = Mathf.Min(Mathf.Pow(2f, reconnectAttempt - 1), Mathf.Max(1f, maximumReconnectDelay));
            SetState(HypeRateConnectionState.Reconnecting);
            LastError = $"Connection closed ({closeCode}). Reconnecting in {delay:0.#} seconds.";
            reconnectRoutine = StartCoroutine(ReconnectAfterDelay(delay));
        }

        private async Task SendTextAsync(HypeRateWebSocket target, string message)
        {
            try
            {
                if (target != null && target.IsOpen)
                {
                    await target.SendTextAsync(message);
                }
            }
            catch (Exception exception)
            {
                ReportError($"Could not send a HypeRate message: {exception.Message}");
            }
        }

        private void StartHeartbeatRoutine(int generation)
        {
            StopHeartbeatRoutine();
            heartbeatRoutine = StartCoroutine(HeartbeatLoop(generation));
        }

        private void StopHeartbeatRoutine()
        {
            if (heartbeatRoutine == null)
            {
                return;
            }

            StopCoroutine(heartbeatRoutine);
            heartbeatRoutine = null;
        }

        private void StopReconnectRoutine()
        {
            if (reconnectRoutine == null)
            {
                return;
            }

            StopCoroutine(reconnectRoutine);
            reconnectRoutine = null;
        }

        private void ReportError(string message)
        {
            if (destroyed)
            {
                return;
            }

            LastError = message;
            SetState(HypeRateConnectionState.Error);
            Debug.LogError($"[HypeRate] {message}", this);
            Error?.Invoke(message);
            onError.Invoke(message);
        }

        private void RaiseDisconnectedOnce()
        {
            if (disconnectEventRaised)
            {
                return;
            }

            disconnectEventRaised = true;
            Disconnected?.Invoke();
            onDisconnected.Invoke();
        }

        private void SetState(HypeRateConnectionState state)
        {
            State = state;
        }

        private bool IsCurrentConnection(HypeRateWebSocket source, int generation)
        {
            return !destroyed && source != null && ReferenceEquals(source, socket) && generation == connectionGeneration;
        }

        private int NextReference()
        {
            if (messageReference == int.MaxValue)
            {
                messageReference = 0;
            }

            return ++messageReference;
        }

        private static string BuildConnectionUrl(string endpoint, string apiToken)
        {
            var separator = endpoint.Contains("?") ? "&" : "?";
            return $"{endpoint.TrimEnd('/')}{separator}token={Uri.EscapeDataString(apiToken.Trim())}";
        }

        private bool TryResolveConfiguration(out ConnectionConfiguration configuration, out string error)
        {
            if (!string.IsNullOrWhiteSpace(runtimeApiToken) && !string.IsNullOrWhiteSpace(runtimeDeviceId))
            {
                configuration = new ConnectionConfiguration(runtimeApiToken, runtimeDeviceId, runtimeEndpoint);
                error = string.Empty;
                return true;
            }

#if UNITY_EDITOR
            var editorSettings = HypeRateSettings.LoadEditorSettings();
            if (editorSettings != null && editorSettings.IsConfigured)
            {
                configuration = new ConnectionConfiguration(
                    editorSettings.ApiToken,
                    editorSettings.DeviceId,
                    editorSettings.Endpoint);
                error = string.Empty;
                return true;
            }

            error = "HypeRate is not configured for Editor testing. Open Tools > HypeRate > Setup.";
#else
            error = "HypeRate has no runtime configuration. Call Configure before connecting.";
#endif
            configuration = default;
            return false;
        }

        private static bool TryCreateWebSocketUri(string value, out Uri endpoint)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out endpoint) &&
                   (endpoint.Scheme == "ws" || endpoint.Scheme == "wss");
        }

        private readonly struct ConnectionConfiguration
        {
            public ConnectionConfiguration(string apiToken, string deviceId, string endpoint)
            {
                ApiToken = apiToken.Trim();
                DeviceId = deviceId.Trim();
                Endpoint = string.IsNullOrWhiteSpace(endpoint)
                    ? HypeRateSettings.DefaultEndpoint
                    : endpoint.Trim();
            }

            public string ApiToken { get; }
            public string DeviceId { get; }
            public string Endpoint { get; }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PrepareClientsForEditorPlayMode()
        {
#if UNITY_6000_6_OR_NEWER
            var clients = FindObjectsByType<HypeRateClient>();
#elif UNITY_2023_1_OR_NEWER
            var clients = FindObjectsByType<HypeRateClient>(FindObjectsSortMode.None);
#else
            var clients = FindObjectsOfType<HypeRateClient>();
#endif
            foreach (var client in clients)
            {
                client.PrepareForPlayMode();
            }
        }

        private void PrepareForPlayMode()
        {
            var currentSocket = socket;
            socket = null;
            currentSocket?.CancelConnection();
            currentSocket?.Dispose();

            StopAllCoroutines();
            heartbeatRoutine = null;
            reconnectRoutine = null;
            destroyed = false;
            quitting = false;
            intentionalDisconnect = false;
            simulationActive = false;
            disconnectEventRaised = true;
            reconnectAttempt = 0;
            CurrentHeartRate = 0;
            LastError = string.Empty;
            State = HypeRateConnectionState.Disconnected;
            connectionGeneration++;

            if (connectOnStart && isActiveAndEnabled)
            {
                Connect();
            }
        }
#endif
    }
}
