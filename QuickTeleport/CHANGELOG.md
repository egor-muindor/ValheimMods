# Changelog

## 1.0.1 - 2026-09-12

- Fix: the screen could stay black forever after a portal teleport, with "Local player destroyed" in the log and no error. The mod reported the new position to the server before the player's network object had been moved there, and the game then unloaded the player as an object outside the loaded area. The player's network object is now moved first.

## 1.0.0 - 2026-09-12

First release, built against the Valheim 1.0.12 assemblies.

- Auto mode: the teleport ends as soon as the screen is black and the destination is loaded; no fixed 2 s / 8 s waits.
- Multiplier mode: the vanilla timing (fade, move delay, portal minimum, floor timeout) runs N times faster.
- Configurable fade duration.
- The optional waits of the old OdinPlus QuickTeleport: skip waiting for objects, skip waiting for the area.
- The new position is reported to the server right after the move (vanilla does it every 2 s), and a settle wait in Auto mode lets the buildings arrive before the teleport ends.
- Debug log with the timeline of every teleport; console command `quickteleport reload|status`.
