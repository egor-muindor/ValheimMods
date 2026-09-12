# Changelog

## 1.0.0 - 2026-09-12

First release, built against the Valheim 1.0.12 assemblies.

- Scans a configurable radius (default 20 m) around the player every second and highlights each ore vein the first time it comes into range.
- Screen arrow with the ore name and distance: over the vein while it is in view, at the screen edge pointing towards it otherwise.
- The vein itself gets a coloured pulsing light, a vertical beam and an emissive tint for a configurable time (default 10 s).
- Ores are recognised by what they drop, so plain rocks are ignored; the `Ores` setting narrows the search to a list of items or prefabs.
- Toggle key (default F9), saved to the config; console command `orefinder status|on|off|reset|reload`.
