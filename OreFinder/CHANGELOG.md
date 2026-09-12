# Changelog

## 1.1.0 - 2026-09-12

- New targets besides ores, each with the same arrow, light, beam and map pin: dungeon entrances (any door with an Enter prompt: burial chambers, sunken crypts, troll caves, frost caves, infested mines, ...), ancient roots, and pickables from the `Pickables` list (dragon eggs, Jotun puffs, Magecap, Fiddlehead, Volture eggs by default). Trees can be listed by their wood in `Trees` (empty by default). Non-ore targets use their own `TargetRadius` (40 m).
- Map pin icons per group: `OrePin` (dot), `DungeonPin` (house), `OtherPin` (dot), any of the five player pin icons.
- Custom names: targets without a pair in `Names` get the initials of their name (Burial Chambers = BC, Dragon egg = DE). `Names` now also carries `MushroomMagecap=Mc, Fiddlehead=Fh, $item_ancientroot=YR`.

## 1.0.3 - 2026-09-12

- New: hidden ores need the Wishbone. Veins the game marks for the Wishbone (silver veins, scrap piles with a beacon) are only found while the Wishbone is in your inventory; `WishboneNeeded` can require it equipped instead, or drop the requirement. Veins skipped for lack of the Wishbone are found later once you carry it.

## 1.0.2 - 2026-09-12

- New: your own names for the ores. `CustomNames` (off by default) switches the screen marker, the message and the map pin to the names from `Names`, which comes pre-filled with one-letter names: `CopperOre=C, TinOre=T, SilverOre=S, IronScrap=I, FlametalOre=F, FlametalOreNew=F, Obsidian=O`.

## 1.0.1 - 2026-09-12

- Fixed: intact copper and silver deposits were only found after the first hit. An untouched deposit is a plain destructible that turns into the mineable rock when hit; the finder now looks through that as well. The rock that appears after the first hit is recognised as the same vein and not highlighted again.
- New: a map pin (dot with the ore name, saved with your map) for every found vein, `MapPin` in the config. No pin is added when any other pin is within `MapPinSpacing` metres (10 by default).

## 1.0.0 - 2026-09-12

First release, built against the Valheim 1.0.12 assemblies.

- Scans a configurable radius (default 20 m) around the player every second and highlights each ore vein the first time it comes into range.
- Screen arrow with the ore name and distance: over the vein while it is in view, at the screen edge pointing towards it otherwise.
- The vein itself gets a coloured pulsing light, a vertical beam and an emissive tint for a configurable time (default 10 s).
- Ores are recognised by what they drop, so plain rocks are ignored; the `Ores` setting narrows the search to a list of items or prefabs.
- Toggle key (default F9), saved to the config; console command `orefinder status|on|off|reset|reload`.
