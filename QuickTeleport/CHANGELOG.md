# Changelog

## 1.1.0 - 2026-09-16

- Optional server configuration. A server that also runs QuickTeleport and turns the new `Server.ConfigPriority` on decides every setting except `IsDebug` for the players who have the mod, so everyone on it travels at the same speed. The settings that travel are marked `[synced]` in the config file.
- The mod stays optional on both sides: it is not part of the game's version check, players without it are unaffected, and it keeps working on servers that do not have it or that leave `ConfigPriority` off.
- The client's config file is never written to. The server's values live in memory for as long as the connection lasts and are dropped when it ends; a setting changed on a running server is sent to the connected players right away.
- `quickteleport status` and `quickteleport reload` say where the active settings come from.

## 1.0.3 - 2026-09-12

- Dungeon entrances (crypts, caves) no longer wait for `SettleTime`: the interior is loaded together with the entrance, so nothing is left to arrive from the server. With the default fade they now take a fraction of a second.

## 1.0.2 - 2026-09-12

- The default `FadeDuration` is 0.1 s instead of 1 s. Config files created by an older version keep their value; lower it by hand.
- README: a "Making it faster" section on the settings each player can change locally, and realistic timings for a dedicated server.

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
