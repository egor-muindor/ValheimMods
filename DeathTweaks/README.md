# Death Tweaks

Configurable death behaviour for Valheim 1.0: decide which items are kept,
dropped or destroyed, keep your food, tune or disable skill loss, and choose
where you respawn.

A from-scratch reimplementation of aedenthorn's DeathTweaks for the current
game version. The feature set and the config keys are the same, so an existing
`aedenthorn.DeathTweaks.cfg` can be reused by renaming it to
`muindor.DeathTweaks.cfg`.

Client-side: install it on every player's game that should use it. Nothing is
needed on the server, and players without the mod can play on the same server.

Source, issues and releases: [github.com/egor-muindor/ValheimMods](https://github.com/egor-muindor/ValheimMods).

## Features

- Keep, drop or destroy items by item type or prefab name.
- Keep equipped items, hotbar items, teleportable items, or everything.
- Keep items in [EquipmentAndQuickSlots](https://valheim.thunderstore.io/package/RandyKnapp/EquipmentAndQuickSlots/) quick slots (3.x, through its public API).
- Drop items on the ground instead of into a tombstone.
- Keep active food after respawn.
- Disable death effects.
- Change the skill loss fraction, disable skill loss, or remove the skill-loss protection after a recent death.
- Respawn at the start location or at fixed coordinates.

Kept equipped items stay equipped after respawn.

## Installation

Requires [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).

- With a mod manager (r2modman, Thunderstore Mod Manager): install as usual.
- By hand: unzip the release and copy the `plugins/DeathTweaks` folder into
  `BepInEx/plugins`, so the mod ends up at `BepInEx/plugins/DeathTweaks/DeathTweaks.dll`.

The config file `BepInEx/config/muindor.DeathTweaks.cfg` is created on first launch.

## Configuration

### General

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Enable the mod. |
| `IsDebug` | `false` | Log every item decision on death. |

### Toggles

| Key | Default | Description |
|-----|---------|-------------|
| `KeepAllItems` | `false` | Keep everything. Overrides all other item options. |
| `DestroyAllItems` | `false` | Destroy everything except quest items. Overrides all other item options except `KeepAllItems`. |
| `KeepEquippedItems` | `false` | Keep equipped items. Overrides the item lists. |
| `KeepHotbarItems` | `false` | Keep items in the first inventory row. Overrides the item lists. |
| `KeepQuickSlotItems` | `false` | Keep items in EquipmentAndQuickSlots quick slots. Overrides the item lists. |
| `KeepTeleportableItems` | `false` | Keep items that can go through portals. Does not override the item lists. |
| `UseTombStone` | `true` | Put dropped items in a tombstone. When `false` they are scattered on the ground. |
| `CreateDeathEffects` | `true` | Create the ragdoll and particle effects. |
| `KeepFoodLevels` | `false` | Keep active food after respawn. |

### ItemLists

All lists are comma-separated. Type names are the `ItemDrop.ItemData.ItemType`
values (`Material`, `Consumable`, `OneHandedWeapon`, `Bow`, `Shield`, `Helmet`,
`Chest`, `Ammo`, `Customization`, `Legs`, `Hands`, `Trophy`, `TwoHandedWeapon`,
`Torch`, `Misc`, `Shoulder`, `Utility`, `Tool`, `Attach_Atgeir`, `Fish`,
`TwoHandedWeaponLeft`, `AmmoNonEquipable`, `Trinket`). Item names are prefab
names such as `Iron`, `CopperOre`, `TrophyBoar`; the `$item_*` name token is
accepted too. Matching is case-insensitive.

| Key | Description |
|-----|-------------|
| `KeepItemTypes` / `KeepItems` | Items to keep. |
| `DropItemTypes` / `DropItems` | Items to drop even when `KeepTeleportableItems` would keep them. |
| `DestroyItemTypes` / `DestroyItems` | Items to destroy. Overrides the keep and drop lists. |

### Rule precedence

For every item, the first matching rule decides:

1. World modifier *Keep inventory on death* or `KeepAllItems`: keep.
2. Quest item: keep.
3. Equipped and world modifier *Keep equipment on death*: keep.
4. `DestroyAllItems`: destroy.
5. `KeepEquippedItems`, `KeepHotbarItems`, `KeepQuickSlotItems`: keep.
6. Destroy lists: destroy.
7. Keep lists: keep.
8. Drop lists: drop.
9. `KeepTeleportableItems`: keep.
10. Otherwise: drop.

The world modifiers *Delete items on death* and *Delete unequipped items on
death* then turn drops into destroys, as in vanilla. Items the mod keeps are
never affected by them.

### Skills

| Key | Default | Description |
|-----|---------|-------------|
| `ReduceSkills` | `true` | Lower skills on death. When `false`, skills are never lowered or reset, even with the *Reset skills on death* world modifier. |
| `SkillReduceFactor` | `0.25` | Fraction of each skill lost on a hard death. Multiplied by the *Skill reduction rate* world modifier, like vanilla. |
| `NoSkillProtection` | `false` | Every death is a hard death; the skill-loss protection after a recent death is disabled. |

### Spawn

| Key | Default | Description |
|-----|---------|-------------|
| `SpawnAtStart` | `false` | Respawn at the start location after death. Takes precedence over `UseFixedSpawnCoordinates`. |
| `UseFixedSpawnCoordinates` | `false` | Respawn at `FixedSpawnCoordinates` after death. |
| `FixedSpawnCoordinates` | `0,0,0` | World coordinates for the option above. A point below the terrain or under water is lifted above it; a point with no terrain at all (outside the world) falls back to the vanilla respawn. |

Logging in and skipping the intro always use the vanilla spawn logic; only
respawns after a death are affected.

## Console command

```
deathtweaks status   # show whether the mod is enabled and the active item rules
deathtweaks reload   # re-read the config file
```

## Compatibility

- **EquipmentAndQuickSlots 3.x** is fully supported. Its own *Don't drop
  quick slots / equipment on death* settings are applied first; `KeepQuickSlotItems`
  only matters for quick slot items that EquipmentAndQuickSlots would drop.
  Tombstone size and item positions are handled by that mod's own patches.
- **World modifiers** for death are honoured, see the rule precedence above.
- **Upgradeable pockets** (extra inventory rows) need no special handling.
- Mods that replace `Player.OnDeath` or `Player.CreateTombStone` outright will
  bypass this mod.

## How it works

The vanilla death sequence is left intact. Each feature patches the smallest
vanilla method that owns the behaviour:

| Feature | Patched member |
|---------|----------------|
| Item keep/drop/destroy | `Player.CreateTombStone` prefix and finalizer, only for the call made from `Player.OnDeath`. Kept items are taken out of the inventory list while vanilla builds the grave with `Inventory.MoveInventoryToGrave`, then put back. |
| Death effects | `Player.CreateDeathEffects` prefix |
| Keep food | `Player.OnDeath` prefix/postfix |
| Skill loss | `Skills.OnDeath` prefix/postfix (swaps `m_DeathLowerFactor`), `Skills.Clear` prefix, `Player.HardDeath` prefix |
| Respawn point | `Game.FindSpawnPoint` prefix, only when the game flags the respawn as following a death |
| Console command | `Terminal.InitTerminal` postfix |

If a patch throws, it logs the error and lets vanilla run.

## License

MIT
