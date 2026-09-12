# Ore Finder

Points you to the ore veins around you in Valheim 1.0, and to dungeon
entrances, ancient roots and rare pickables. Every second the mod looks for
ore within a configurable radius (20 m by default) and for the other targets
within 40 m. The first time something comes into range it gets a screen arrow
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
  pin icon), ancient roots for the sap extractor, pickables you list (dragon
  eggs, Jotun puffs, Magecap, Fiddlehead, Volture eggs by default) and trees you
  list by their wood.
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
| `IsDebug` | `false` | Log every vein that is found, with its prefab and the item that made it count as ore. |

### Detection

| Key | Default | Description |
|-----|---------|-------------|
| `Radius` | `20` | Search radius in metres around the player, 1 to 200. |
| `ScanInterval` | `1` | Seconds between two scans, 0.1 to 30. |
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
| `Pickables` | `DragonEgg, MushroomJotunPuffs, MushroomMagecap, Fiddlehead, VoltureEgg` | Pickable items to find, by item name, comma-separated (`Thistle`, `CloudBerries`, ... work too). Already picked ones are skipped until they regrow. Empty = none. |
| `Trees` | empty | Trees to find, by the wood they drop (`YggdrasilWood`, `Blackwood`, `ElderBark`, `FineWood`, ...), comma-separated. Empty = none. |
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
| `Names` | `CopperOre=C, TinOre=T, SilverOre=S, IronScrap=I, FlametalOre=F, FlametalOreNew=F, Obsidian=O, MushroomMagecap=Mc, Fiddlehead=Fh, $item_ancientroot=YR` | Your names as `key=name` pairs separated by commas. The key is the ore item, the pickable item (`DragonEgg`), the wood of a tree (`YggdrasilWood`), a dungeon's location key (`$location_forestcrypt`) or the object prefab (`rock4_copper`, `silvervein`); the name is anything you like, spaces included. Targets without a pair get the initials of their name: Burial Chambers = `BC`, Sunken Crypt = `SC`, Dragon egg = `DE`, Magecap = `Ma`. |

### Map

| Key | Default | Description |
|-----|---------|-------------|
| `MapPin` | `true` | Add a dot pin named after the ore to the map when a vein is found. The pin is saved with your map like one you placed yourself. |
| `MapPinSpacing` | `10` | Do not add a pin when any other pin (yours, the mod's, a death marker, ...) is within this many metres, 0 to 100. `0` = always add. Moving or momentary pins (players, shouts, pings, events) do not count. |
| `OrePin` | `Dot` | Map pin icon for ores: `Fire`, `House`, `Hammer`, `Dot` or `Portal`, the five icons you can place yourself. |
| `DungeonPin` | `House` | Map pin icon for dungeon entrances. |
| `OtherPin` | `Dot` | Map pin icon for roots, pickables and trees. |

### What counts as ore

With an empty `Ores` list, a mineable object is ore when one of the items it
drops has the word *Ore* or *Scrap* in its name: `CopperOre`, `TinOre`,
`SilverOre`, `IronScrap`, `FlametalOre`, `FlametalOreNew`. Anything a game
update adds with such a name is picked up automatically. `LeatherScraps` and
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
```

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
hardcoded to a list of prefabs: a dungeon entrance is a `Teleport` with an
enter text (the exits inside have none), a root is a `ResourceRoot`, a pickable
is a `Pickable` whose item is in the list (its picked state is read from the
object, so picked ones wait until they regrow), and a tree is a `TreeBase`
whose log chain (`TreeLog` and its sub-logs) drops a listed wood.

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
