# Ore Finder

Points you to the ore veins around you in Valheim 1.0, and to dungeon
entrances, monster spawners, ancient roots and rare pickables. Every second the
mod looks for ore within a configurable radius (20 m by default) and for the
other targets within 40 m. The first time something comes into range it gets a screen arrow
with its name and distance, a coloured light, a vertical beam and a glow, for
10 seconds by default, plus a pin on the map. Each find is shown once, so
walking past a deposit you already know does not nag you.

Only real ore counts: copper and tin deposits, silver veins, iron scrap piles,
flametal. Plain rocks, obsidian and black marble are ignored unless you list
them. Press F9 to turn the finder on and off.

Client-side: install it on every player's game that should use it. Nothing is
needed on the server, and players without the mod can play on the same server.

Source, issues and releases: [github.com/egor-muindor/ValheimMods](https://github.com/egor-muindor/ValheimMods).

## Features

- **Screen arrow**: an arrow with the ore name and distance is drawn over the
  vein while it is in view. When the vein is behind you or outside the screen,
  the arrow sits at the screen edge and points towards it.
- **Vein highlight**: a pulsing point light in the ore's colour (orange for
  copper, blue-grey for tin, white for silver, rust for iron, red for flametal),
  a 30 m vertical beam and an emissive tint on the vein itself. Everything fades
  out at the end of the highlight.
- **Once per vein**: a vein is highlighted the first time it enters the radius
  and not again, until you leave the world or run `orefinder reset`.
- **Ore list**: look for every ore, or only the ones you name.
- **Map pin**: a dot pin named after the ore, saved with your map, for every
  found vein. Nothing is added when another pin is already nearby, so your own
  pins and re-found veins do not pile up.
- **Your own names**: call the ores what you like, for example one letter each
  so the map stays readable. Off by default.
- **Hidden ores need the Wishbone**: silver veins and the scrap piles the
  Wishbone reacts to are only found while you carry it, like in the game.
- **More than ores**: entrances of crypts, caves and mines (with their own map
  pin icon), monster spawners (greydwarf nests, bone and body piles, surtling
  spawners and other respawning spawn points, with a hammer pin), ancient roots
  for the sap extractor, pickables you list (dragon eggs, Jotun puffs, Magecap,
  Fiddlehead, Volture eggs by default) and trees you list by their wood.
- **Each group on its own switch**: ores, dungeon entrances, roots and spawners
  can be turned on and off separately, in the config, from the console
  (`orefinder dungeons off`) or with their own hotkeys.
- **Toggle key**: F9 by default, configurable, with modifiers if you like. The
  state is saved to the config file.

## Installation

Requires [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).

- With a mod manager (r2modman, Thunderstore Mod Manager): install as usual.
- By hand: unzip the release and copy the `plugins/OreFinder` folder into
  `BepInEx/plugins`, so the mod ends up at `BepInEx/plugins/OreFinder/OreFinder.dll`.

The config file `BepInEx/config/muindor.OreFinder.cfg` is created on first launch.

## Configuration

### General

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Enable the finder. The toggle key flips this setting in game and saves it. |
| `ToggleKey` | `F9` | Key that turns the finder on and off in game. Modifiers are allowed, e.g. `F9 + LeftControl`. Ignored while typing in the chat or the console. |
| `OresToggleKey` | unset | Key that turns the ore search (`FindOres`) on and off on its own, leaving the other targets as they are. |
| `DungeonsToggleKey` | unset | Key that turns the dungeon entrance search (`Dungeons`) on and off on its own. |
| `SpawnersToggleKey` | unset | Key that turns the spawner search (`Spawners`) on and off on its own. |
| `IsDebug` | `false` | Log every vein that is found, with its prefab and the item that made it count as ore. |

### Detection

| Key | Default | Description |
|-----|---------|-------------|
| `Radius` | `20` | Search radius in metres around the player, 1 to 200. |
| `ScanInterval` | `1` | Seconds between two scans, 0.1 to 30. |
| `FindOres` | `true` | Look for ores at all. Off = only the targets from the Targets section are found; `Ores` still says which ores count. `orefinder ores on|off` and `OresToggleKey` flip this setting. |
| `Ores` | empty | Which ores to look for, comma-separated. Empty = every ore. Otherwise item names (`CopperOre`, `TinOre`, `SilverOre`, `IronScrap`, `FlametalOre`, `FlametalOreNew`) or object prefab names (`rock4_copper`, `silvervein`, `MineRock_Obsidian`). |

### Highlight

| Key | Default | Description |
|-----|---------|-------------|
| `Duration` | `10` | Seconds a newly found vein stays highlighted, 1 to 120. |
| `ScreenMarker` | `true` | Draw the arrow with the ore name and distance. |
| `Beam` | `true` | Draw the vertical beam above the vein. |
| `Light` | `true` | Light up the vein and the ground around it with a coloured, pulsing light. |
| `Glow` | `true` | Tint the vein's own material with an emissive glow. Only works for materials whose shader has an emission colour. |
| `Message` | `true` | Show a top-left message with the ore name and distance when a vein is found. |

### Targets

| Key | Default | Description |
|-----|---------|-------------|
| `Dungeons` | `true` | Find the entrances of crypts, caves and mines: any door with an Enter prompt (burial chambers, sunken crypts, troll caves, frost caves, infested mines, and whatever a game update adds). |
| `Roots` | `true` | Find ancient roots, the sap extractor spots in the Mistlands. |
| `Spawners` | `true` | Find monster spawners: greydwarf nests, evil bone piles, body piles and the like, plus the invisible spawn points that respawn their creature (surtling spawners at fire geysers, ...). Spawn points that fire only once are skipped. |
| `Pickables` | `Pickable_DragonEgg, Pickable_Mushroom_JotunPuffs, Pickable_Mushroom_Magecap, Pickable_Fiddlehead, Pickable_VoltureEgg` | Pickables to find, by the object's prefab name or by the item it gives (`DragonEgg`, `Thistle`, `Cloudberry`), comma-separated. Other useful ones: `Pickable_Thistle`, `CloudberryBush`, `Pickable_BogIronOre`, `Pickable_MountainCaveCrystal`. Already picked ones are skipped until they regrow. Empty = none. |
| `Trees` | empty | Trees to find, by the wood they drop (`YggdrasilWood`, `Blackwood`, `Frostwood`, `ElderBark`, `FineWood`, ...), comma-separated. Empty = none. |
| `TargetRadius` | `40` | Search radius in metres for everything except ores, 1 to 200. `Radius` is for ores. |

### Hidden

| Key | Default | Description |
|-----|---------|-------------|
| `WishboneNeeded` | `InInventory` | Hidden ores are the ones the game marks for the Wishbone: silver veins and the scrap piles with a beacon. `InInventory`: found only while the Wishbone is anywhere in your inventory. `Equipped`: only while it is equipped, like the game's own finder. `NotNeeded`: always found. Hidden veins skipped for lack of the Wishbone are found later once you carry it. |
| `WishboneItem` | `Wishbone` | Item that counts as the Wishbone, by prefab name or `$item_` name. Change it for a modded finder item. |

### Names

| Key | Default | Description |
|-----|---------|-------------|
| `CustomNames` | `false` | Show your own names from `Names` instead of the game's names (Copper deposit, Silver vein, ...) in the screen marker, the message and the map pin. |
| `Names` | `CopperOre=C, TinOre=T, SilverOre=S, IronScrap=I, FlametalOre=F, FlametalOreNew=F, GoldOre=B, Obsidian=O, Pickable_Mushroom_Magecap=Mc, Pickable_Fiddlehead=Fh, $item_ancientroot=YR` | Your names as `key=name` pairs separated by commas. The key is the ore item (`CopperOre`, `GoldOre`), the pickable's prefab or item (`Pickable_DragonEgg`, `DragonEgg`), the wood of a tree (`YggdrasilWood`), a dungeon's location key (`$location_forestcrypt`), a spawner's prefab (`Spawner_GreydwarfNest`) or the object prefab (`rock4_copper`, `silvervein`); the name is anything you like, spaces included. Targets without a pair get the initials of their name: Burial Chambers = `BC`, Sunken Crypt = `SC`, Dragon egg = `DE`, Magecap = `Ma`, Greydwarf spawner = `GS`. |

### Map

| Key | Default | Description |
|-----|---------|-------------|
| `MapPin` | `true` | Add a dot pin named after the ore to the map when a vein is found. The pin is saved with your map like one you placed yourself. |
| `MapPinSpacing` | `10` | Do not add a pin when any other pin (yours, the mod's, a death marker, ...) is within this many metres, 0 to 100. `0` = always add. Moving or momentary pins (players, shouts, pings, events) do not count. |
| `OrePin` | `Dot` | Map pin icon for ores: `Fire`, `House`, `Hammer`, `Dot` or `Portal`, the five icons you can place yourself. |
| `DungeonPin` | `House` | Map pin icon for dungeon entrances. |
| `SpawnerPin` | `Hammer` | Map pin icon for spawners. |
| `OtherPin` | `Dot` | Map pin icon for roots, pickables and trees. |

### What counts as ore

With an empty `Ores` list, a mineable object is ore when one of the items it
drops has the word *Ore* or *Scrap* in its name: `CopperOre`, `TinOre`,
`SilverOre`, `IronScrap`, `FlametalOre`, `FlametalOreNew`, and the Deep North's
`GoldOre` (Petrified Tissue). Anything a game update adds with such a name is
picked up automatically. `LeatherScraps` and
`ShieldCore` do not qualify, and neither do `Stone`, `Obsidian`, `BlackMarble`
or `Softtissue`.

With a list, an object is ore when one of its drops or its own prefab name is
in the list. Names are case-insensitive and `$item_` localisation keys are
accepted. The list replaces the rule: to get every ore plus obsidian, list all
of them:

```
Ores = CopperOre, TinOre, SilverOre, IronScrap, FlametalOre, FlametalOreNew, Obsidian
```

## Console command

```
orefinder status   # show the active settings and counters
orefinder on       # turn the finder on (same as the toggle key)
orefinder off      # turn it off
orefinder reset    # forget the veins already shown, so they are highlighted again
orefinder reload   # re-read the config file

orefinder ores off        # stop looking for ores, keep the other targets
orefinder dungeons on     # look for dungeon entrances again
orefinder spawners        # flip the spawner search
orefinder roots off
```

The group commands change `FindOres`, `Dungeons`, `Spawners` and `Roots` in
the config, like the hotkeys do. Pickables and trees are lists; empty the list
to turn them off.

## How it works

Once per `ScanInterval` the mod walks the objects the game has loaded around
the player (`ZNetScene`'s instance table) and keeps the ones within `Radius`.
Each object's prefab is classified once, through its drop table: `MineRock5`
(copper, silver, flametal), `MineRock` (tin, obsidian) and `DropOnDestroyed`
(scrap piles). An untouched copper or silver deposit is a plain `Destructible`
that turns into the mineable `MineRock5` on the first hit, so the finder also
looks at what a destructible spawns when destroyed. Veins are remembered by
their network id and position: a partly mined deposit is still the same vein,
and so is the rock that replaces an intact deposit after the first hit.

Hidden ores are recognised the way the game does it: the object (or what it
turns into) carries a `Beacon` component, which is what the Wishbone's finder
effect listens for. The Wishbone check looks at the inventory once per scan.

The other targets are recognised by their components too, so nothing is
hardcoded to a list of prefabs: a root is a `ResourceRoot`, a spawner is a
`SpawnArea` (nests, piles) or a `CreatureSpawner` with a respawn time, a
pickable is a `Pickable` whose item is in the list (its picked state is read
from the object, so picked ones wait until they regrow), and a tree is a
`TreeBase` whose log chain (`TreeLog` and its sub-logs) drops a listed wood.

Dungeon entrances take a detour. A door is a `Teleport` trigger, but the game
does not spawn it as a network object: a location is spawned in two parts,
every `ZNetView` on its own and everything else as one plain object under the
location's `LocationProxy` (which is a network object). The doors are in the
second part, so they never appear in the instance table. The finder looks at
every proxy within `TargetRadius` + 50 m (a proxy stands at the location's
centre, the door can be some way out), takes the teleports under it that are
not inside the interior (the game keeps interiors above 3000 m; the exits are
there) and marks each door by its own position. The door is named after its
enter text (Burial Chambers, Sunken Crypt, ...) or, without one, after the
location.

The highlight does not touch the vein's game object: the light and beam are
separate objects, and the glow is a material property block that is cleared
when the highlight ends. Map pins go through `Minimap.AddPin` like your own
pins and are saved with the map. The screen markers are drawn with IMGUI, so no canvas
or font asset is needed. The only Harmony patch is `Terminal.InitTerminal`, for
the console command.

## Compatibility

- Works alongside any mod: nothing of the game is patched except the console
  command registration.
- Modded ores are found when their ore item's prefab name contains the word
  Ore or Scrap; otherwise add them to `Ores`.
- The toggle key is checked through the game's input layer (`ZInput`), so it
  works with the same keys the game accepts.

## License

MIT
