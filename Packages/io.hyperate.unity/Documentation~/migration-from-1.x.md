# Migrating from the original HypeRate SDK

Version 2 is a complete replacement for the original singleton-based SDK. Make
a backup or commit your project before updating.

## Remove the legacy package

Delete `Assets/HypeRate/HypeRate Heart Rate SDK` after moving any project-specific
scripts or scene changes out of that folder. Leaving the old SDK in place can
produce duplicate implementations and confusing setup options.

## Add the new client

1. Open **Tools > HypeRate > Setup**.
2. Enter the API token and Device ID used for Editor testing.
3. Click **Save Settings**.
4. Click **Add HypeRate Client to Scene**.

## Replace the old API

The old entry point:

```csharp
HypeRate.HypeRate.GetInstance()
```

is replaced by a `HypeRate.SDK.HypeRateClient` component. Subscribe to
`HeartRateChanged`, or configure the `On Heart Rate Changed` Unity Event in the
Inspector.

```csharp
using HypeRate.SDK;

client.HeartRateChanged += heartRate =>
{
    Debug.Log($"{heartRate} BPM");
};
```

Connection, channel join, heartbeat, main-thread dispatch, and reconnect logic
are handled by the component.

## Player credentials

Editor test credentials are not included in builds. Supply production
credentials at runtime and disable **Connect On Start** when configuration is
provided by another component:

```csharp
client.Configure(apiToken, deviceId);
client.Connect();
```
