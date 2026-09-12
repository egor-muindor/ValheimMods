# Changelog

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
