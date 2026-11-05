# Pulse Lab

Open `PulseLab.unity` and enter Play Mode. The sample starts with the Workout
simulation profile, so it works immediately without credentials or a wearable.

Use the on-screen controls to switch between Rest, Workout and Intense profiles.
The BPM value, biometric sphere, pulse waveform, room lighting and particles all
react through the same `HypeRateClient` events used by a live connection.

To try live data:

1. Open **Tools > HypeRate > Setup** and save your Editor test credentials.
2. Enter Play Mode in the Pulse Lab scene.
3. Click **Connect Live Data**.

The sample creates its materials at runtime. Its custom Built-in Render Pipeline
sphere shader falls back to pipeline-compatible glass materials in URP and HDRP,
so the demo needs no additional shader package.
