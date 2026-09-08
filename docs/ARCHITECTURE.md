# Architecture (STS1-style Alignment, STS2-safe)

> Current-state review, 2026-09-07: Current architecture is C#/.NET 9 with native models, Harmony, and Godot; no BaseLib runtime dependency. Only NativeCode is compiled. NativeModelCatalog, NativeStableIds, NativeAssetPaths and native presentation factories own registration, IDs and resource routing. The source roots and helpers below are historical. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

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
