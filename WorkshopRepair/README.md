# WorkshopRepair

Repairs in Valheim 1.0 without the clicking and the walking around.

- **One click repairs everything.** The repair button of a crafting station mends every damaged
  item it can in one go, instead of one item per click. One sound per station, one message for
  the lot, and the same Crafting skill gain as clicking through them one by one.
- **The whole workshop helps.** A station also repairs the items that belong to another station
  standing within 30 m of you. Open the workbench with a forge nearby and your bronze and iron
  gear comes back as good as new together with the wooden club - no need to walk from station to
  station.

A nearby station only helps when it could do the repair itself: the right kind for the item,
upgraded far enough for it, and usable right now - a forge needs its roof, a station that needs a
fire needs it lit. The station you are using has to be one that repairs at all; the button shows
up where it does in vanilla, and nowhere else.

Client-side. Other players need nothing, and neither does the server.

## Settings

`BepInEx/config/muindor.WorkshopRepair.cfg`, or ConfigurationManager if it is installed:

| Setting | Default | What it does |
|---------|---------|--------------|
| `Enabled` | `true` | With this off the repair button works exactly as in vanilla. `[synced]` |
| `RepairAllAtOnce` | `true` | One click repairs every item. Off: one item per click, as in vanilla. `[synced]` |
| `UseNearbyStations` | `true` | Other stations nearby repair the items that belong to them. `[synced]` |
| `NearbyRadius` | `30` | How far from you, in metres, such a station may stand (2-100). `[synced]` |
| `IsDebug` | `false` | Log every repaired item and the station that repaired it. |

## Optional server configuration

Install the mod on a dedicated server (or on the machine hosting the session) and turn
`Server.ConfigPriority` on in its config file: every client that also has the mod then runs on
the server's values for the settings marked `[synced]`, instead of its own - all of them but
`IsDebug`. It stays optional on both sides: a player without the mod can still join, and a player
with it can join a server that does not have it and keep their own settings. The client's config
file is never written to; the server's values last as long as the connection.

## Compatibility

The mod patches `InventoryGui.CanRepair` (a postfix that lets a nearby station say yes) and
`InventoryGui.RepairOneItem` (a prefix that repairs everything, only while `RepairAllAtOnce` is
on). Other repair mods that replace the same button may disagree with it; turn one of the two
features off if they do.
