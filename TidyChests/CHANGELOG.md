# Changelog

## 1.0.0 - 2026-09-12

First release, built against the Valheim 1.0.12 assemblies.

- Stash button in the inventory: every stackable regular item goes into the nearest chests (10 m by default, 1 to 20) that already hold that item, Terraria style. Existing stacks are topped up first, then empty slots, then the next chest.
- Equipment, tools, weapons, equipped items, quest items, the hotbar (optional) and the slots of Extra Slots, EquipmentAndQuickSlots and Better Archery are never stashed; an item blacklist and a list of allowed item types are configurable.
- Find key (T by default): point at an item in the inventory and press it; the inventory closes and every chest in range that holds the item is lit up, tinted and marked on screen with the count and distance.
- Button label and messages localized (English, Russian) through the game's language setting.
- Works with MultiUserChest; without it, chests in use by other players are skipped and ownership is claimed before writing.
- Console command `tidychests stash|reload|status`.
