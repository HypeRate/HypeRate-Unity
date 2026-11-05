# Getting started

## Requirements

- Unity 2022.3 or newer
- HypeRate API access and a WebSocket API token
- The Device/Session ID shown in the HypeRate app

API availability and commercial terms depend on the project. See the
[official HypeRate API page](https://hyperate.io/api) for current details.

The preview supports the Unity Editor and .NET WebSocket-capable standalone and mobile targets. WebGL needs a browser-specific transport and is not included yet.

## Setup

Open **Tools > HypeRate > Setup**, enter the token and Device ID, save the local settings, and add a HypeRate Client to the active scene.

The client connects automatically in Play Mode. Its Inspector displays the connection state, latest heart rate, and actionable errors.

## Inspector workflow

Use the `On Heart Rate Changed` Unity Event on the HypeRate Client component to call a method whenever a new BPM value arrives. The target method should accept one `int` parameter.

## C# workflow

```csharp
using HypeRate.SDK;
using UnityEngine;

public sealed class HeartRateExample : MonoBehaviour
{
    [SerializeField] private HypeRateClient client;

    private void OnEnable()
    {
        client.HeartRateChanged += OnHeartRateChanged;
    }

    private void OnDisable()
    {
        client.HeartRateChanged -= OnHeartRateChanged;
    }

    private void OnHeartRateChanged(int heartRate)
    {
        Debug.Log($"Heart rate: {heartRate} BPM");
    }
}
```

The client automatically joins the configured heart-rate channel, sends Phoenix heartbeats, reconnects with exponential backoff, and dispatches callbacks on Unity's main thread.

## Develop without a device

Add `HypeRateSimulator` next to a `HypeRateClient` to produce realistic test
values in Play Mode. Resting, Workout, Intense and Custom profiles publish
through the client's normal `HeartRateChanged` event and Unity Event, so gameplay
code does not need a separate test path.

```csharp
var simulator = gameObject.AddComponent<HypeRateSimulator>();
simulator.StartSimulation();
```

The optional **Pulse Lab** Package Manager sample demonstrates the same workflow
in a ready-to-run 3D scene.

## Editor test credentials

The setup window creates
`Assets/HypeRateGenerated/Editor/HypeRateSettings.asset` and excludes the
generated folder from Git. Because the asset is inside an `Editor` folder, Unity
does not include it in player builds.

## Player builds

Configure the client in memory before connecting:

```csharp
client.Configure(apiToken, deviceId);
client.Connect();
```

Disable **Connect On Start** when another component supplies the configuration.
Do not put a long-lived token in source code, a scene, or a Resources asset.
Load it from your own runtime configuration or backend appropriate to your
project. Any credential delivered to a client can ultimately be extracted and
should be scoped accordingly.

## Upgrading from the legacy SDK

See [migration-from-1.x.md](migration-from-1.x.md) before importing version 2
into a project that already contains the original SDK.
