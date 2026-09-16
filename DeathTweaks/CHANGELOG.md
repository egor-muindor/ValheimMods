# Changelog

## 1.1.0 - 2026-09-16

- Optional server configuration. A server that also runs DeathTweaks and turns the new `Server.ConfigPriority` on decides every setting except `IsDebug` for the players who have the mod, so a death means the same thing for everyone on it. The settings that travel are marked `[synced]` in the config file.
- The mod stays optional on both sides: it is not part of the game's version check, players without it are unaffected, and it keeps working on servers that do not have it or that leave `ConfigPriority` off.
- The client's config file is never written to. The server's values live in memory for as long as the connection lasts and are dropped when it ends; a setting changed on a running server is sent to the connected players right away.
- `deathtweaks status` and `deathtweaks reload` say where the active settings come from.

## 1.0.2 - 2026-09-12

- Quick slot detection is now based on the item's grid position through the slot mods' public APIs instead of their cached slot contents, which could report empty slots during death.
- With Extra Slots, `KeepQuickSlotItems` now covers quick, food, ammo and misc slots (everything except equipment slots), matching what players expect to keep.
- The slot mod API is resolved at startup and reported in the log; the debug log shows each item's grid position.

## 1.0.1 - 2026-09-12

- `KeepQuickSlotItems` now also works with Extra Slots (shudnal.ExtraSlots), not only EquipmentAndQuickSlots.
- A warning is logged at startup and on death when `KeepQuickSlotItems` is on but no supported quick slot mod is loaded; previously quick slot items were silently dropped.
- The debug log marks quick slot and hotbar items per item.

## 1.0.0 - 2026-09-12

First release, built against the Valheim 1.0.12 assemblies.

- Keep, drop or destroy items by item type or prefab name.
- Keep equipped, hotbar, teleportable or all items; keep EquipmentAndQuickSlots 3.x quick slot items through its public API.
- Drop items on the ground instead of into a tombstone.
- Keep active food after respawn.
- Disable death effects.
- Change or disable skill loss, disable the skill-loss protection after a recent death.
- Respawn at the start location or at fixed coordinates.
- Vanilla death world modifiers are honoured.
- Config keys match aedenthorn's DeathTweaks; rename the old `.cfg` to `muindor.DeathTweaks.cfg` to reuse it.
- Console command `deathtweaks reload|status`.
