# Changelog

## 1.3.0 - 2026-09-16

- Optional server configuration. A server that also runs OreFinder and turns the new `Server.ConfigPriority` on decides `Enabled` and the whole of `Detection`, `Targets`, `Hidden`, `Names` and `Map` for the players who have the mod, so a party looks for the same things and a shared map ends up with one set of pin names and icons instead of one per player. The settings that travel are marked `[synced]` in the config file.
- The toggle keys, the `Highlight` section and `IsDebug` stay each player's own and are never sent. While the server decides `Enabled` or a group switch, the toggle keys and `orefinder on|off|ores|dungeons|spawners|roots` say so and change nothing.
- The mod stays optional on both sides: it is not part of the game's version check, players without it are unaffected, and it keeps working on servers that do not have it or that leave `ConfigPriority` off.
- The client's config file is never written to. The server's values live in memory for as long as the connection lasts and are dropped when it ends; a setting changed on a running server is sent to the connected players right away.
- `orefinder status` and `orefinder reload` say where the active settings come from.

## 1.2.0 - 2026-09-12

- Fixed: dungeon entrances were never found. The doors are not network objects: the game spawns them, with the rest of a location's non-networked parts, as one object under the location's proxy, so the scan of loaded network objects never saw them. The finder now looks under every location proxy nearby and marks each door by its own position. Location proxies are looked at from `TargetRadius` + 50 m, because a proxy stands at the location's centre and the door can be some way out. A door without an enter text is named after its location.
- New: monster spawners. `Spawners` (on by default) finds greydwarf nests, evil bone piles, body piles and the like (`SpawnArea`), plus the invisible spawn points that respawn their creature (`CreatureSpawner` with a respawn time: surtling spawners at fire geysers, the respawning points in ruins and camps). Spawn points that fire only once are skipped. They get a red highlight and a hammer map pin (`SpawnerPin`); the name is the creature's ("Greydwarf spawner"), initials `GS` with custom names.
- New: each group on its own switch. `FindOres` turns the ore search off while the other targets stay on; `orefinder ores|dungeons|roots|spawners [on|off]` flips a group from the console (no on/off = toggle); `OresToggleKey`, `DungeonsToggleKey` and `SpawnersToggleKey` (unset by default) do the same with a key. Turning a group off removes its highlights at once.
- Changed: `orefinder status` lists the groups that are on ("targets: ores, dungeon entrances, ...") and the ore list separately.
- Changed: no map pin for a find inside a dungeon (the interior's coordinates point at nothing on the map); the arrow, light and beam still show.

## 1.1.1 - 2026-09-12

- Defaults checked against the game's ID list: `Pickables` now lists the pickable objects by their exact prefab names (`Pickable_DragonEgg`, `Pickable_Mushroom_JotunPuffs`, `Pickable_Mushroom_Magecap`, `Pickable_Fiddlehead`, `Pickable_VoltureEgg`); item names still work too. The short names for Magecap and Fiddlehead are keyed the same way.
- Deep North: the new ore item `GoldOre` (Petrified Tissue, smelts into Bloodgold) is found by the ore rule; its short name is `B`. `Frostwood` (Timberwood) is a valid entry for `Trees`.

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
