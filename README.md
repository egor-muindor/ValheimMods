# ValheimMods

BepInEx mods for Valheim 1.0.

| Mod | Description |
|-----|-------------|
| [DeathTweaks](DeathTweaks/README.md) | Keep, drop or destroy items on death, keep food, tune skill loss, choose the respawn point. |

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

The plugin is written to `DeathTweaks/bin/Release/DeathTweaks.dll`. Set
`VALHEIM_PLUGINS_DIR` to your `BepInEx/plugins` folder to have every build
copied there automatically. Machine-specific settings can also go into a
gitignored `Environment.props` at the repository root:

```xml
<Project>
  <PropertyGroup>
    <ValheimManagedDir>/path/to/Valheim/valheim_Data/Managed</ValheimManagedDir>
    <ValheimPluginsDir>/path/to/Valheim/BepInEx/plugins</ValheimPluginsDir>
  </PropertyGroup>
</Project>
```

## How updates are handled

The game assembly is publicized at build time and every patched member is
referenced with `nameof()`. When a game update renames or removes something the
mod touches, the build fails at the exact spot instead of the mod failing
silently in game.

## License

[MIT](LICENSE)
