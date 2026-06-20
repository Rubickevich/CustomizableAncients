# Cuztomizable Ancients

Every Ancient. Every relic. Full multiplayer support. Full mod support.

Cuztomizable Ancients adds a visual, drag-and-drop editor for Ancient relic rewards to the
character-select lobby.

## Features

- Configure every vanilla or modded Ancient independently.
- Add, remove, or empty an Ancient's reward options.
- Assign one or several relic pools to each option.
- Enable, disable, and add individual relics through an icon-based editor.
- Create custom pools without changing vanilla shop, event, or Ancient drops.
- Browse non-native and modded pools and relics.
- Reset any Ancient to its default configuration.
- Synchronize the host's complete configuration when a multiplayer run starts.

## Usage

Open the configuration panel from character select and choose an Ancient from the icon tabs.
Drag pools from **Available pools** onto an option to add them. Drag them back to remove them,
or drag a pool into the configuration area to edit its relics.

The host controls configuration in multiplayer. The complete configuration is sent to every
player when the run begins.

## Requirements

- Slay the Spire 2
- BaseLib v3.2.1 or newer

## Mod integration

Modded Ancients, relics, and `RelicPoolModel` implementations are discovered automatically.
Other mods can additionally register default pool placements so their content works immediately
without requiring manual setup.

See the [integration guide](docs/INTEGRATION.md) for dependency setup, API examples, and runtime
behavior.

## Building

Set `STS2_PATH` to the Slay the Spire 2 installation directory, then build the project:

```powershell
dotnet build .\CuztomizableAncients\CuztomizableAncients.csproj -c Release
```
