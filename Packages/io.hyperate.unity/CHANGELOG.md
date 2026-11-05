# Changelog

## [2.0.0-pre.1] - 2026-11-05

- Rebuilt the SDK as an installable Unity Package Manager package.
- Added the HypeRate setup window and custom client Inspector.
- Added automatic channel join, heartbeat, reconnect, and main-thread callbacks.
- Added Unity Events and typed C# events for heart-rate updates.
- Added an Editor-friendly heart-rate simulator with Resting, Workout, Intense,
  and Custom profiles.
- Added the optional Heart Rate Widget sample, with a compact BPM card and
  history graph driven by the same events used for live HypeRate data.
- Added consistent connection indicators to the widget, setup window, and
  client Inspector: red while offline, yellow while connecting, and green when
  connected. Simulation remains blue so it cannot be mistaken for live data.
- Added Editor-only test credential handling, runtime configuration, documentation,
  sample code, and protocol tests.
- Changed the public namespace to `HypeRate.SDK`.

This is a breaking update from the original Asset Store SDK. See the migration
guide before updating an existing project.
