# Tidy Chests

Terraria-style quick stack for Valheim 1.0. A **Stash** button in the inventory
moves your materials, food, ammo and trophies into the nearby chests that
already hold that kind of item (10 m by default). Equipment, tools, weapons,
equipped items, quest items and the slots of your quick slot mod stay where they
are. And when you wonder where you put something, point at it in the inventory
and press **T**: the inventory closes and every chest in range that holds it
lights up, with an arrow and the count on screen. **Ctrl+O** lists everything the
chests around you hold, with a search box. And in co-op, the materials your team
mates drop in those chests unlock their recipes for you, without hunting down
every new stack to touch it.

Client-side: install it on every player's game that should use it. Nothing is
needed on the server, and players without the mod can play on the same server.

Source, issues and releases: [github.com/egor-muindor/ValheimMods](https://github.com/egor-muindor/ValheimMods).

## Features

- **Stash button**: every stackable regular item in the inventory goes to the
  nearest chest within the radius that already contains an item with the same
  name. Existing stacks are topped up first, then empty slots are used, and what
  is left continues to the next chest. Chests, reinforced and black metal chests,
  carts and ship cargo all count. A chest that holds none of your items
  is never touched, so your sorting stays yours.
- **What stays**: equipped items; tools, weapons, armour and other equipment;
  quest items; the hotbar (optional); quick, equipment, food, ammo and misc
  slots of Extra Slots, every slot of EquipmentAndQuickSlots, the quiver rows of
  Better Archery; anything in the blacklist; item types not in the allowed list.
- **Find key** (`T` by default): with the inventory open, point at an item and
  press the key. Every chest in range that holds the item gets a pulsing light,
  an emissive tint and a screen marker with the count and the distance, for
  8 seconds by default. The inventory closes so you can look around; if no chest
  holds the item, a message says so and the inventory stays open. Works for
  items in an open chest and in the crafting panel too: point at an ingredient
  of the selected recipe, at the recipe's icon or at an entry of the recipe list
  to see which chests hold that item.
- **Chest list** (`Ctrl+O` by default): every item in the chests within the scan
  radius, one row per kind, with the total, how many chests hold it and how far
  the nearest one is. Type to filter by name; click a row to close the list and
  light up every chest holding that item. Escape or the same key closes it.
- **Learning from chests**: the items lying in the chests around you count as
  found, so their recipes unlock. Valheim normally unlocks a recipe only once the
  material has been in your own inventory, which in a party means hunting down
  every stack a team mate gathered. Trophies count too. **This cannot be undone**:
  turning `LearnFromChests` off later does not lock a recipe again.
- **Localized**: the button label and the messages follow the game language
  (English and Russian included, English elsewhere).
- **Multiplayer**: with [MultiUserChest](https://thunderstore.io/c/valheim/p/MSchmoecker/MultiUserChest/)
  items go through its requests, so shared chests behave. Without it, chests that
  another player has open, carts in use and ship cargo (in multiplayer) are
  skipped, and the mod takes ownership of a chest before writing to it, like
  opening it would.

## Installation

Requires [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).

- With a mod manager (r2modman, Thunderstore Mod Manager): install as usual.
- By hand: unzip the release and copy the `plugins/TidyChests` folder into
  `BepInEx/plugins`, so the mod ends up at `BepInEx/plugins/TidyChests/TidyChests.dll`.

The config file `BepInEx/config/muindor.TidyChests.cfg` is created on first launch.

## Configuration

### General

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Enable the mod: the Stash button, the find key, the chest list and the learning from chests. |
| `IsDebug` | `false` | Log every item decision and every chest that is considered when stashing. |

### Stash

| Key | Default | Description |
|-----|---------|-------------|
| `Radius` | `10` | Chests within this many metres of the player are used, by the button and by the find key, 1 to 20. |
| `IncludeHotbar` | `false` | Also stash items from the first inventory row. |
| `ItemTypes` | `Material, Consumable, Ammo, AmmoNonEquipable, Trophy, Misc, Fish` | Item types that may be stashed. Remove `Ammo` to keep arrows, add `Torch` to stash torches, and so on. Valid names are listed in the config file. |
| `Blacklist` | empty | Items that are never stashed, comma-separated prefab names or item names: `Wood, $item_coal, Resin`. |
| `ShowMessage` | `true` | Show a message with the result after stashing. |

### Find

| Key | Default | Description |
|-----|---------|-------------|
| `FindKey` | `T` | With the inventory open, point at an item (in the inventory, or an ingredient or recipe in the crafting panel) and press this key. Modifiers are allowed, e.g. `T + LeftControl`. |
| `HighlightDuration` | `8` | Seconds the found chests stay highlighted, 1 to 60. |
| `ScreenMarker` | `true` | Draw an arrow with the item count and the distance over each found chest, at the screen edge when it is out of view. |
| `Light` | `true` | Light up each found chest with a pulsing light. |
| `Glow` | `true` | Tint each found chest with an emissive glow (only for materials whose shader has an emission colour). |
| `CloseInventory` | `true` | Close the inventory when chests are found. |

### Button

| Key | Default | Description |
|-----|---------|-------------|
| `ShowButton` | `true` | Show the Stash button in the inventory. `tidychests stash` in the console works without it. |
| `ButtonOffset` | `41, -56` | Position of the button relative to the weight display of the inventory panel, in UI pixels (x right, y up). Adjust if another UI mod puts something there. |
| `ButtonSize` | `120, 38` | Width and height of the button in UI pixels. |

### Scan

Used by the chest list and by the learning from chests.

| Key | Default | Description |
|-----|---------|-------------|
| `ScanRadius` | `50` | Chests within this many metres are read, 5 to 90. Above roughly 90 m the chests are no longer loaded, so nothing more is found. |
| `ScanInterval` | `5` | Seconds between two scans, 1 to 60. Chests whose contents did not change are skipped, so a short interval is cheap. |

### Knowledge

| Key | Default | Description |
|-----|---------|-------------|
| `LearnFromChests` | `true` | Count the items in the chests in range as found, so their recipes unlock. Cannot be undone; see above. |

### Browser

| Key | Default | Description |
|-----|---------|-------------|
| `BrowserKey` | `O + LeftControl` | Opens and closes the chest list. Modifiers are allowed; set it to `None` to disable the panel. |
| `BrowserSize` | `520, 560` | Width and height of the panel in UI pixels. |

## Console command

```
tidychests stash    # stash now, without the button
tidychests scan     # scan the chests in range for items you do not know yet
tidychests status   # show the active settings
tidychests reload   # re-read the config file
```

## How it works

Items are moved with the same inventory calls the vanilla "stack all" button
uses (`Inventory.AddItem` + `RemoveItem` for whole stacks, the positional
`MoveItemToThis` of drag and drop for parts of a stack), so chest-sharing mods
that intercept these calls keep working. Which item goes where is decided by a
small planner on snapshots of the inventory and of the chests in range, and the
same rules run in the unit tests.

Reading the chests costs nothing on the network. The game already sends every
client the contents of the chests around it (`ZDOMan` replicates the ZDO data of
everything in the active area) and re-reads them once a second
(`Container.CheckForChanges`), so the chest list and the learning only look at
what is already in memory, and a chest whose data revision did not change is not
even re-read. Known materials are local player data, saved in the character file,
not in a network object. Neither feature sends a single extra packet or takes
ownership of a chest; only the Stash button writes.

| Feature | Patched member |
|---------|----------------|
| Container list | `Container.Awake` and `Container.OnDestroyed` postfixes keep a list of loaded containers |
| Stash button | `InventoryGui.Show` postfix clones the "take all" button into the player panel |
| Button label, messages | `Localization.SetupLanguage` postfix adds the mod's words for the loaded language |
| Console command | `Terminal.InitTerminal` postfix |
| Chest list input | `Player.TakeInput` postfix stops the player moving, `GameCamera.UpdateMouseCapture` postfix keeps the cursor free, `Menu.Update` prefix lets Escape close the list instead of opening the game menu |

Items are learned through the game's own `Player.AddKnownItem`, a couple per
frame, so trophies, the unlock messages and the recipe list stay vanilla's
business and a storeroom full of unknown materials unlocks over a second instead
of freezing a frame.

The find key is read in a plain `Update`; the hovered inventory item is found
the way the game finds it for the tooltip, and the crafting panel's ingredient
icons, recipe icon and recipe list entries are tested against the pointer the
same way. If a patch throws, it logs the error and lets
vanilla run.

## Compatibility

- Extra Slots, EquipmentAndQuickSlots 3.x, Better Archery: their slots are
  detected through the mods' own APIs and never stashed.
- MultiUserChest: supported, see above.
- Craft From Containers: complementary, not overlapping. It spends the resources
  in nearby chests on a recipe you already know; this mod unlocks the recipe in
  the first place. Nothing here touches the resource cost of a craft.
- Other quick stack mods (QuickStackStore, QuickerStack, ...) should run
  alongside; they add their own buttons and hotkeys. Not tested together.
- Auga or other UI replacements without a "take all" button: the Stash button is
  not shown; use `tidychests stash` or bind it to a key with a hotkey mod.

## License

MIT
