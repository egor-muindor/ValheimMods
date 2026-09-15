# Changelog

## 1.1.0 - 2026-09-15

- Learning from chests (`LearnFromChests`, on by default): the items lying in the chests around you count as found, so their recipes unlock. Meant for co-op, where a team mate gathers a material you have never held and vanilla keeps the recipe locked until you touch the stack yourself. Trophies count too. This cannot be undone: turning the option off later does not lock a recipe again.
- Chest list (`Ctrl+O` by default): every item in the chests in range, one row per kind, with the total, how many chests hold it and the distance to the nearest one. Type to filter by name; click a row to close the list and light up every chest holding that item; Escape or the same key closes it.
- New `Scan` settings shared by both: `ScanRadius` (50 m, 5 to 90) and `ScanInterval` (5 s).
- Neither feature sends anything over the network. The chest contents are already replicated to every client by the game, and a chest whose data revision did not change is not even re-read; known materials are local player data.
- Console command `tidychests scan`, and `tidychests status` now reports what has been learned.

## 1.0.1 - 2026-09-12

- The find key also works in the crafting panel: point at an ingredient of the selected recipe, at the recipe's icon or at an entry of the recipe list and press it to highlight the chests that hold that item.

## 1.0.0 - 2026-09-12

First release, built against the Valheim 1.0.12 assemblies.

- Stash button in the inventory: every stackable regular item goes into the nearest chests (10 m by default, 1 to 20) that already hold that item, Terraria style. Existing stacks are topped up first, then empty slots, then the next chest.
- Equipment, tools, weapons, equipped items, quest items, the hotbar (optional) and the slots of Extra Slots, EquipmentAndQuickSlots and Better Archery are never stashed; an item blacklist and a list of allowed item types are configurable.
- Find key (T by default): point at an item in the inventory and press it; the inventory closes and every chest in range that holds the item is lit up, tinted and marked on screen with the count and distance.
- Button label and messages localized (English, Russian) through the game's language setting.
- Works with MultiUserChest; without it, chests in use by other players are skipped and ownership is claimed before writing.
- Console command `tidychests stash|reload|status`.
