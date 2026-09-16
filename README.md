# ValheimMods

BepInEx mods for Valheim 1.0.

| Mod | Description |
|-----|-------------|
| [DeathTweaks](DeathTweaks/README.md) | Keep, drop or destroy items on death, keep food, tune skill loss, choose the respawn point. |
| [QuickTeleport](QuickTeleport/README.md) | Faster portal and dungeon teleports: finish as soon as the destination is loaded, or run vanilla timing N times faster. |
| [TidyChests](TidyChests/README.md) | Terraria-style quick stack: a Stash button moves materials into nearby chests that already hold them; a hotkey highlights the chests holding the item under the cursor. |
| [OreFinder](OreFinder/README.md) | Points you to ore veins, dungeon entrances, monster spawners, ancient roots and rare pickables: screen arrow with name and distance, light beam and map pin, once per find; hotkey toggle, each group on its own switch. |

## Optional server configuration

Every mod above changes how the game plays, so a server may want all its players
on the same settings. Install the mod on a dedicated server (or on the machine
hosting the session) and turn `Server.ConfigPriority` on in its config file:
every client that also has the mod then runs on the server's values for the
settings marked `[synced]`, instead of its own.

It stays optional on both sides. None of these mods is part of the game's
version check, so it never blocks a connection: players without the mod are
unaffected, and a player with it can still join a server that does not have it,
or one that leaves `ConfigPriority` off, and keeps their own settings. The
client's own config file is never written to - the server's values live in
memory for as long as the connection lasts.

Which settings travel is a per-mod decision, listed in each mod's README.
Broadly: rules of play do, keys and the look of the HUD do not. The
implementation is shared, in [`Shared/ServerConfig`](Shared/ServerConfig), and
compiled into each mod rather than shipped as a second assembly.

## Building

Requirements:

- .NET SDK 8 or newer (the projects target `net462`; the reference assemblies are restored from NuGet, so no .NET Framework or Mono installation is needed).
- The game's managed assemblies.

Game assemblies are not part of the repository. Copy the contents of
`<Valheim>/valheim_Data/Managed` into `lib/Managed`, or point the
`VALHEIM_MANAGED_DIR` environment variable (or the `ValheimManagedDir` MSBuild
property) at an existing `Managed` folder.

```sh
dotnet build -c Release
dotnet test
```

Each mod is written to `<Mod>/bin/Release/<Mod>.dll`. A Release build also
produces `dist/<Mod>-<version>.zip`: a Thunderstore-ready package (manifest and
icon live in `<Mod>/Package`) with the plugin under `plugins/<Mod>/`, which is
also the layout for manual installs. Set `VALHEIM_PLUGINS_DIR` to your
`BepInEx/plugins` folder to have every build copied there automatically.
Machine-specific settings can also go into a gitignored `Environment.props` at
the repository root:

```xml
<Project>
  <PropertyGroup>
    <ValheimManagedDir>/path/to/Valheim/valheim_Data/Managed</ValheimManagedDir>
    <ValheimPluginsDir>/path/to/Valheim/BepInEx/plugins</ValheimPluginsDir>
  </PropertyGroup>
</Project>
```

## Publishing to Thunderstore

The packages are listed under the [Muindor](https://thunderstore.io/c/valheim/p/Muindor/)
team: [DeathTweaks](https://thunderstore.io/c/valheim/p/Muindor/DeathTweaks),
[QuickTeleport](https://thunderstore.io/c/valheim/p/Muindor/QuickTeleport),
[TidyChests](https://thunderstore.io/c/valheim/p/Muindor/TidyChests) and
[OreFinder](https://thunderstore.io/c/valheim/p/Muindor/OreFinder).
`<Mod>/thunderstore.toml` holds the listing metadata (categories, community,
dependencies); the zip comes from the Release build. To publish a new version:

1. Bump `<Version>` in `<Mod>.csproj`, `version_number` in
   `<Mod>/Package/manifest.json` and `versionNumber` in `<Mod>/thunderstore.toml`
   (the build fails if they differ), add a `CHANGELOG.md` entry, commit and tag
   `<Mod>-v<version>`.
2. Run `scripts/publish-thunderstore.sh <Mod>` (add `--dry-run` to only build
   and print what would be uploaded). It needs [tcli](https://github.com/thunderstore-io/thunderstore-cli)
   (`dotnet tool install -g tcli`) and a token in `TCLI_AUTH_TOKEN`, or a
   1Password reference in a gitignored `.publish.env`:

   ```sh
   THUNDERSTORE_TOKEN_OP_REF="op://<vault>/<item>/password"
   ```

## How updates are handled

The game assembly is publicized at build time and every patched member is
referenced with `nameof()`. When a game update renames or removes something the
mod touches, the build fails at the exact spot instead of the mod failing
silently in game.

## License

[MIT](LICENSE)
