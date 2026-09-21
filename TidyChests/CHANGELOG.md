# Changelog

## 1.3.0 - 2026-09-21

Asked for in #1. Tested as 1.3.0-beta.1.

- Locks: items and slots the Stash button must leave alone, set from the inventory. Point at an item and press **Shift+L** to lock or unlock it: a locked item is never stashed, wherever it lies and whatever else lies around it. Point at a slot and press **Shift+K** to lock the slot itself: whatever is put there stays. Both keys are configurable.
- Hold **Shift** (configurable) with the inventory open to see the locks: a grey padlock in the corner of a locked item, a red one on a locked slot. Pointing at a slot while holding it shows a tooltip with the state of both locks and the keys.
- The locks are saved in `[Locks] LockedItems` and `[Locks] LockedSlots` as they are pressed, so they survive a restart and can be edited by hand. A slot is `column:row` counted from 0, the hotbar being row 0.
- `tidychests status` and the debug log report the locks; a kept item is logged as `LockedItem` or `LockedSlot`.

## 1.2.2 - 2026-09-21

- On Linux the cursor stayed pinned to the centre of the screen while the chest list was open: vanilla re-locked the mouse every frame and the mod undid it after the warp had already happened, which Windows hides by deferring the warp and Linux does not. Vanilla's mouse capture is now skipped while the list is open, so the cursor is released once and stays free. Reported and fixed by @Chulii.

## 1.2.1 - 2026-09-16

- The chest list has a header: click **Name** or **Count** to sort by that column, click the active one again to flip it. The list starts with the largest stacks at the top, and the choice is saved in `[Browser] Sort`.
- Items can be pinned. Click the diamond on the left of a row and it stays at the top of the list, above everything else, in the same order the rest is sorted in. The pins are saved in `[Browser] Favorites` as item names, so they survive a restart and a language change.
- A pinned item stays listed with a count of 0 once no chest in range holds it any more, so a material running out is visible instead of silently disappearing from the list. Such a row is dimmed and does nothing when clicked, since there is nothing to highlight.
- Pinning is ignored while something is typed in the search box: a search answers with its best matches first. The sort still decides the order among equally good matches.
- `tidychests status` reports the sort and how many items are pinned.

## 1.2.0 - 2026-09-16

- Optional server configuration. A server that also runs TidyChests and turns the new `Server.ConfigPriority` on decides `Enabled`, `Radius`, `IncludeHotbar`, `ItemTypes`, `Blacklist`, `ScanRadius`, `ScanInterval` and `LearnFromChests` for the players who have the mod, so everyone on it reaches as far and stashes by the same rules. The settings that travel are marked `[synced]` in the config file.
- Keys, the Stash button, the panels and the highlights stay each player's own and are never sent.
- The mod stays optional on both sides: it is not part of the game's version check, players without it are unaffected, and it keeps working on servers that do not have it or that leave `ConfigPriority` off.
- The client's config file is never written to. The server's values live in memory for as long as the connection lasts and are dropped when it ends; a setting changed on a running server is sent to the connected players right away.
- `tidychests status` and `tidychests reload` say where the active settings come from.

## 1.1.1 - 2026-09-15

Fixes to the chest list, from the first in-game test of 1.1.0.

- The character no longer walks, attacks or looks around while the list is open. Walking and the mouse look are gated by a second check, `PlayerController.TakeInput`, which 1.1.0 did not patch, so typing a name into the search box moved the player.
- The mouse wheel scrolls the list three rows per step instead of crawling, and no longer zooms the camera at the same time. Only the sign of the wheel is used, because the value the game reports per step has no fixed scale.
- The list has a scrollbar, copied from the crafting panel's.
- The search box keeps the keyboard when the keyboard layout is switched with a shortcut, or after the scrollbar is dragged, and what was typed is no longer selected when the focus comes back.

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
