# Changelog

## 1.0.2

Everything here came out of a review of the first release; nothing changes what the meter shows
when it is right.

- Rejoining a world left the mod deaf. The game builds a new routed RPC for every session and
  never clears the one it kept before, so the mod believed its handlers were still registered:
  from the second world on it sent packets and received none, and the previous world's numbers
  carried over. The channel now follows the object itself and starts the session clean.
- The greeting went out before any player had finished connecting, so nobody ever heard it and
  clients only learned of each other through the first damage packet - which double-counted that
  first half second. It is now sent whenever the company changes, and it carries whether the
  sender shares at all, so a client that keeps its numbers to itself is not mistaken for one that
  reports them.
- A batch from farther away than the share radius was dropped whole, which could lose your own
  hits: the owner of a creature may stand a long way from it. What a packet says about you is now
  always kept.
- With pet damage on, every wild creature got a row of its own: a raid filled the window with the
  names of what was attacking it. Only tamed creatures and summons count now.
- Damage over time was charged to the last attacker even when the game had discarded the blow (a
  dodge, a corpse, PvP being off), so a burn from a campfire could land in someone's row. The note
  is now taken only when the game really handed the fire, poison or spirit to a status effect.
- Players seen only through other clients' packets were named "..." for the rest of the session;
  they are now looked up in the player list the server keeps.
- A packet naming a meter this version does not have threw inside the RPC handler. It is refused,
  along with a count the packet could not possibly hold.
- Your own row can be collapsed in the detail window and stays collapsed.
- Installed on a dedicated server the mod no longer takes part in the meter - it has no player, so
  it recorded hits nobody would ever see and made its clients stop estimating theirs. Only the
  configuration channel runs there now.
- Damage to trees, ore and buildings is marked as an estimate, which is what it always was: it is
  measured as it is swung, before the target's resistances.
- Blows the game throws away as too small to matter are no longer counted, and the keys of
  creatures can no longer stray into the range a player's id can occupy.

## 1.0.1

- The buttons of the detail window were drawn without their labels, which left the window tabs,
  the `settings` button and the `close` button of the settings panel invisible. They were there
  and they worked; there was nothing to read on them.
- The column heads of the detail window are pinned to the same distances from the right edge as
  the numbers they name, instead of being spaced out by hand.

## 1.0.0

First release.

- A compact window that appears under the minimap when a fight starts and fades out when it ends:
  one row per player with the damage, the share of the total and a bar split into damage kinds.
- A detail window (`Ctrl+D`) with a tab per window length - 30 seconds, 5, 10 and 30 minutes - the
  totals for the window, and rows that open into their damage kinds.
- Damage is taken from the client that owns the target, so the numbers are the ones the target
  really took. Fire, poison and spirit are counted as they tick and charged to whoever started
  them.
- Clients with the mod share what they see, so a group has one complete picture. A blow at a
  creature owned by a client without the mod is recorded as sent and marked with a `~`.
- Damage taken, healing, damage to trees, ore and buildings, and pet damage can be turned on.
- A settings panel in game for what gets changed often; the rest is in the config file.
- Optional server configuration for `Enabled`, `ShareDamage` and `ShareRadius`.
