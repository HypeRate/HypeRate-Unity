# HypeRate for Unity

Receive live HypeRate heart-rate data in Unity without managing WebSockets, Phoenix channels, heartbeats, threads, or reconnect logic yourself.

The preview supports the Unity Editor and .NET WebSocket-capable standalone and mobile targets. WebGL support is planned separately.

## Quick start

1. Open **Tools > HypeRate > Setup**.
2. Enter your HypeRate API token and Device ID.
3. Click **Save Settings**.
4. Click **Add HypeRate Client to Scene**.
5. Enter Play Mode.

The component exposes both Inspector-friendly Unity Events and C# events:

```csharp
client.HeartRateChanged += heartRate =>
{
    Debug.Log($"{heartRate} BPM");
};
```

## Try it without a device

Import the **Pulse Lab** sample from Package Manager and press Play. Its built-in
simulator drives the same client state and heart-rate events as a live connection,
so UI, animation, audio and game logic can be developed without wearing a sensor.

For a small custom test scene, add both **HypeRate Client** and **HypeRate
Simulator** to one GameObject. Choose a Resting, Workout, Intense or Custom
profile in the Inspector.

The setup tool stores Editor test credentials under
`Assets/HypeRateGenerated/Editor`, which it adds to the project's `.gitignore`.
The asset is excluded from player builds.

For a player build, configure the client at runtime before connecting:

```csharp
using HypeRate.SDK;

client.Configure(apiToken, deviceId);
client.Connect();
```

Do not ship long-lived credentials in source code or scene assets. Obtain them
from your own runtime configuration or backend appropriate to your project.

See [Documentation~/getting-started.md](Documentation~/getting-started.md) for more details.
