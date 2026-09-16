# CombatStats

A combat meter for Valheim 1.0: who dealt how much damage, of which damage type, over the last
30 seconds, 5, 10 or 30 minutes.

Two windows. A compact one appears under the minimap when a fight starts and fades out when it
ends: one row per player, the damage, the share of the total, and a bar split into the damage
kinds it is made of. A detail window has the whole picture - a tab per window length, the totals
for the window, and rows that open into their damage kinds.

Client-side. Other players need nothing, and neither does the server; when they do have the mod,
the numbers get better (see [Together with other players](#together-with-other-players)).

## Keys

| Key | What it does |
|-----|--------------|
| `F10` | Cycles the compact window: shown while fighting, always shown, off. |
| `Ctrl+D` | Opens and closes the detail window. `Esc` closes it as well. |

Both are configurable, and either can be set to `None` to turn it off.

## What it counts

Out of the box, damage dealt to creatures, by players.

The rest is off by default and each has its own switch, in the settings panel or in the config
file: damage taken, healing, damage to trees, ore and buildings, and what tamed creatures and
summons deal (charged to the player they follow, or to a row of their own). Only something that
belongs to a player counts there: wild creatures never get a row.

Some details worth knowing:

- The numbers are what the target really took, after resistances, armour and the world modifiers,
  measured on the client that owns the target. The killing blow counts in full, overkill included.
- Fire, poison and spirit are counted as they tick, not when the blow lands, so a target that dies
  early or walks into water never produces the rest of the damage - which is what really happened.
- The average is damage per hit, not per second: a quiet stretch inside a window does not dilute
  it. A blow carrying two damage kinds is one hit.
- Damage to trees, ore and buildings is measured as it is swung, before the target's own
  resistances, so those rows are marked with a `~` like any other estimate.

## Together with other players

The client that owns a creature is the only one that sees what it really took, so that client
shares what it recorded with the other players who have the mod. What that means in practice:

| Everyone in the fight has the mod | Every player's numbers are complete and exact for everyone. |
|-----------------------------------|------------------------------------------------------------|
| Some do not | Their damage still shows up, as long as a player with the mod owns the creature. |
| Nobody else does | You see your own damage, and everything happening on creatures your own client owns. Your blows on creatures owned by someone else are recorded as sent - before resistances and armour - and marked with a `~`. |

Packets are ignored when they come from farther away than `ShareRadius` (96 m), so a fight on the
other side of the map stays out of your meter. `ShareDamage` turns the sharing off entirely; you
then still see your own numbers.

A player without the mod is unaffected: the packets are named after the plugin, and a client that
does not know the name drops them. Nothing here is part of the game's version check.

## Settings

The ones that get changed often are in the game, behind the `settings` button of the detail
window: when the compact window shows itself, where it sits, how wide, how large and how solid,
how many rows it lists, the window it adds up, the size of the detail window, and the switches
for the extra meters.

The rest lives in `BepInEx/config/muindor.CombatStats.cfg`, and in ConfigurationManager if it is
installed:

| Setting | Default | What it does |
|---------|---------|--------------|
| `Enabled` | `true` | Record combat and show the windows. `[synced]` |
| `HistoryMinutes` | `30` | How far back the meter remembers; also the longest window. |
| `CountDamageTaken`, `CountHealing`, `CountObjectDamage`, `CountPets` | `false` | The extra meters. |
| `CompactKey`, `DetailKey` | `F10`, `Ctrl+D` | The keys. |
| `CompactMode` | `Auto` | `Auto`, `Always` or `Off`. |
| `CompactWindow` | `30` | Seconds the compact window adds up. |
| `CompactRows` | `5` | Players listed; your own row is always among them. |
| `CompactHideAfter` | `5` | Seconds of quiet before it fades out. |
| `CompactAnchor`, `CompactOffset` | `TopRight`, `(-28, -164)` | Where it sits; the default is under the minimap. |
| `CompactWidth`, `CompactScale`, `CompactOpacity` | `320`, `1`, `0.9` | How wide, how large, how solid. |
| `CompactCaption`, `CompactShare`, `CompactBars` | `true` | The caption line, the percentages, the bars. |
| `DetailSize` | `660 x 480` | Size of the detail window. |
| `DetailWindows` | `30, 300, 600, 1800` | The tabs it offers, in seconds. |
| `ShareDamage` | `true` | Send what this client sees to the other players with the mod. `[synced]` |
| `ShareRadius` | `96` | Ignore events sent from farther away than this. `[synced]` |
| `ShareInterval` | `0.5` | Seconds between two packets. |
| `Colors` | empty | Overrides for the damage kind colours: `Fire=#ff7733, Poison=#66aa44`. |
| `IsDebug` | `false` | Log every recorded event and every packet. |

## Optional server configuration

Install the mod on a dedicated server (or on the machine hosting the session) and turn
`Server.ConfigPriority` on in its config file: every client that also has the mod then runs on
the server's values for the settings marked `[synced]`, instead of its own. It stays optional on
both sides, and a player without the mod can still join.

Only `Enabled`, `ShareDamage` and `ShareRadius` travel - a server may decide whether its players
exchange combat data at all. The windows, the keys and the colours stay each player's own, and
the client's config file is never written to.

A dedicated server takes no part in the meter itself: with no screen and no player of its own it
records nothing and answers no greeting, so its clients keep estimating their blows on whatever
the server happens to own.

## Installing

Use a mod manager, or copy `plugins/CombatStats` into `BepInEx/plugins`.

Needs [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

## Compatibility

Nothing here changes how the game plays; it only reads hits as they are applied. The meter shares
its numbers over one routed RPC named after the plugin, which any client without the mod ignores.
