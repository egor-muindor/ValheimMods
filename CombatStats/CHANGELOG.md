# Changelog

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
