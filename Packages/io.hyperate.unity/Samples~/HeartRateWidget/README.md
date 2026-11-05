# Heart Rate Widget

Open `HeartRateWidget.unity` and enter Play Mode. If credentials were saved under
**Tools > HypeRate > Setup**, the scene connects to HypeRate and displays the
real pulse automatically. Without credentials it falls back to the Workout
simulation profile, so the sample still works immediately.

The sample contains only three components on one GameObject:

- **HypeRate Client** receives live or simulated values.
- **HypeRate Simulator** provides test values while developing.
- **Heart Rate Widget** renders the BPM card and history graph.
- **Widget Demo** chooses live data when configured and simulation otherwise.

Live and simulated values use the same `HeartRateChanged` event, so the widget
requires no changes when switching between them.

The status dot is red while offline, yellow while connecting or reconnecting,
green for a live connection, and blue when the fallback simulator is active.
