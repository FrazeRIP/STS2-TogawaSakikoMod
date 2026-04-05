# Architecture (STS1-style Alignment, STS2-safe)

This repo uses Slay the Spire 2 (C#, Godot, BaseLib) and is being reorganized to mirror the STS1 domain-oriented structure.

## Source roots

- Runtime code: `TogawaSakiko/TogawaSakikoCode`
- Assets/localization: `TogawaSakiko/TogawaSakiko`

## Important invariants

Do not relocate these files without updating build/export pipeline:

- `TogawaSakiko/TogawaSakiko.csproj`
- `TogawaSakiko/TogawaSakiko.json`
- `TogawaSakiko/project.godot`

## Domain structure policy

Code is organized by feature domain (Cards, Relics, Powers, Potions, Actions, Events, Patches, etc.) to keep parity with STS1 contributor expectations.

## Resource path policy

- `Util/ResourcePaths.cs` is the canonical source of asset path construction.
- `Extensions/StringExtensions.cs` remains as a compatibility API.

## Migration mode

The project currently supports both legacy and new folder conventions for resources.
