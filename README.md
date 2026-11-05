# HypeRate for Unity

Developer-friendly access to real-time HypeRate heart-rate data in Unity.

The package handles the WebSocket connection, Phoenix channel join, heartbeat,
main-thread dispatch, errors, and automatic reconnects. Developers can consume
heart-rate updates through Unity Events in the Inspector or typed C# events.

## Requirements

- Unity 2022.3 or newer
- HypeRate API access and a WebSocket API token
- The Device/Session ID shown in the HypeRate app

API availability and commercial terms depend on the project. See the
[official HypeRate API page](https://hyperate.io/api) for current details.

## Install in another Unity project

Open **Window > Package Management > Package Manager**, click **+**, choose
**Install package from git URL**, and enter:

```text
https://github.com/HypeRate/HypeRate-Unity.git?path=/Packages/io.hyperate.unity
```

For production projects, install a tagged release instead of the moving default
branch, for example:

```text
https://github.com/HypeRate/HypeRate-Unity.git?path=/Packages/io.hyperate.unity#v2.0.0
```

## Setup

1. Open **Tools > HypeRate > Setup**.
2. Enter your HypeRate API token and Device ID.
3. Click **Save Settings**.
4. Click **Add HypeRate Client to Scene**.
5. Enter Play Mode.

The Inspector displays connection state, errors, and the latest BPM value. Use
the `On Heart Rate Changed` Unity Event for a no-code workflow, or subscribe in
C#:

```csharp
using HypeRate.SDK;

client.HeartRateChanged += heartRate =>
{
    UnityEngine.Debug.Log($"{heartRate} BPM");
};
```

The setup window stores test credentials below
`Assets/HypeRateGenerated/Editor`. They are excluded from Git and player builds.
For a player build, configure the client at runtime before connecting:

```csharp
using HypeRate.SDK;

client.Configure(apiToken, deviceId);
client.Connect();
```

Do not ship long-lived credentials in source code or scene assets. Obtain them
from your own runtime configuration or backend appropriate to your project.

## Developing this repository

The included demo project now targets Unity 6000.6.8f1. The package itself keeps
Unity 2022.3 as its minimum supported version.

Package source and detailed documentation live in
[`Packages/io.hyperate.unity`](Packages/io.hyperate.unity).
