# Quick Teleport

Faster portal and dungeon teleports for Valheim 1.0. By default the teleport
ends as soon as the screen is black and the destination is loaded, instead of
after the fixed 8 seconds vanilla waits for portals. Alternatively, run the
vanilla timing a configurable number of times faster.

A from-scratch replacement for OdinPlus' QuickTeleport (last updated for an
older game version), with the same optional "skip loading" behaviour and a
safer default for dedicated servers.

Client-side: install it on every player's game that should use it. Nothing is
needed on the server, and players without the mod can play on the same server.

Source, issues and releases: [github.com/egor-muindor/ValheimMods](https://github.com/egor-muindor/ValheimMods).

## Features

- **Auto mode** (default): no fixed waits. The screen fades to black, you are
  moved, the mod waits for the destination to load and for the server to stop
  sending new objects, and the screen fades back. A loaded base takes about
  two and a half seconds instead of ten.
- **Multiplier mode**: everything vanilla does, N times faster (the fade, the
  2 s move delay, the 8 s portal minimum, the 15 s floor timeout). `1` is
  vanilla timing plus the early position report described below.
- Configurable fade duration. The player is never moved before the screen is
  fully black, so lowering it makes teleports almost instant.
- The two unsafe options of the old QuickTeleport, off by default: do not wait
  for objects, or do not wait for the area at all.
- Dungeon entrances (crypts, caves) benefit too: they skip the 2 s wait.

## Installation

Requires [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).

- With a mod manager (r2modman, Thunderstore Mod Manager): install as usual.
- By hand: unzip the release and copy the `plugins/QuickTeleport` folder into
  `BepInEx/plugins`, so the mod ends up at `BepInEx/plugins/QuickTeleport/QuickTeleport.dll`.

The config file `BepInEx/config/muindor.QuickTeleport.cfg` is created on first launch.

## Configuration

### General

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Enable the mod. Off = vanilla teleports. |
| `IsDebug` | `false` | Log the timeline of every teleport (screen black, area loaded, settled, finished). |

### Teleport

| Key | Default | Description |
|-----|---------|-------------|
| `Mode` | `Auto` | `Auto`: no fixed waits, the teleport ends as soon as the screen is black and the destination is loaded. `Multiplier`: the vanilla timing divided by `SpeedMultiplier`. |
| `SpeedMultiplier` | `4` | Multiplier mode only, 1 to 100. `1` = vanilla, `4` = four times faster. |
| `FadeDuration` | `1` | Seconds for the screen to fade to black and back (vanilla 1), 0.05 to 5. In Multiplier mode it is divided by `SpeedMultiplier` too. |

### Loading

| Key | Default | Description |
|-----|---------|-------------|
| `WaitForAreaLoad` | `true` | Wait until the destination is loaded (vanilla). `false`: end the teleport right after the fade, like the old QuickTeleport "Skip Loading Area". You may float or fall until the world appears. |
| `WaitForObjects` | `true` | Also wait for the objects of the destination (buildings, trees) to spawn, not only for the terrain (vanilla). `false`: like the old QuickTeleport "Skip Loading Objects". You may end up under a building floor. Ignored when `WaitForAreaLoad` is `false`. |
| `SettleTime` | `0.5` | Auto mode with `WaitForObjects` only, 0 to 5. After the destination is loaded, wait this many seconds without new objects arriving from the server before ending the teleport. Never waits past the vanilla minimum measured from the start of the teleport (8 s for portals, 2 s for dungeons), so with the default 1 s fade a dungeon settles for at most 1 s. `0` disables. |

### Why the settle wait

The game only knows about objects the server has already sent, and the server
sends the objects around the position the client reports, which vanilla does
only every 2 seconds. Vanilla's fixed 8 seconds are what hides that delay. The
mod reports the new position the moment you are moved, then in Auto mode
watches the number of objects in the destination zones and ends the teleport
once it has stopped changing for `SettleTime`. On a local game this adds half
a second; on a busy server it waits for the buildings to arrive, up to the
vanilla 8 seconds. Raise `SettleTime` if you arrive before your base does.

## Console command

```
quickteleport status   # show the active settings
quickteleport reload   # re-read the config file
```

## How it works

The vanilla teleport is left intact: `Player.UpdateTeleport` still moves the
player, switches the environment and looks for a floor. The mod only decides
how fast its clock runs and when the destination counts as loaded. The player
is moved only once the loading screen is fully opaque (or, if another mod
hides it, when vanilla would have moved), and in Auto mode the vanilla
"no floor, drop to terrain height" fallback keeps its 15 s.

| Feature | Patched member |
|---------|----------------|
| Teleport clock | `Player.TeleportTo` postfix (start tracking), `Player.UpdateTeleport` prefix (writes the mod's timer into `m_teleportTimer`) and postfix (reports the new position to the server right after the move, notices the end of the teleport). |
| Destination readiness and settle wait | `ZNetScene.IsAreaReady` prefix/postfix, only for the call made from `Player.UpdateTeleport`. Respawn is untouched. |
| Fade | `Hud.GetFadeDuration` postfix while teleporting and while the screen fades back out. Death and sleep fades are untouched. |
| Console command | `Terminal.InitTerminal` postfix |

If a patch throws, it logs the error and lets vanilla run.

## Compatibility

- Works with any portal mod that goes through `Player.TeleportTo`, including
  ones that allow all items through portals.
- Mods that replace `Player.UpdateTeleport` outright will bypass this mod.
- The 2 s cooldown between teleports is vanilla and unchanged.

## License

MIT
