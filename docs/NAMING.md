# Naming Conventions

## IDs

- Keep IDs stable and deterministic.
- Resource filenames should match normalized ID form used by code (`RemovePrefix().ToLowerInvariant()`).

## Code folders

- Place classes in the closest functional domain folder.
- Prefer STS1-style domain names (`Actions`, `Events`, `Monsters`, `Patches`, etc.) when adding new systems.

## Resources

- Keep existing legacy asset folders working (`card_portraits`, `relics`, `powers`, `charui`).
- New content may use STS1-style directories (`images/cards`, `images/ui`, `images/vfx`, etc.) via `ResourcePaths` helpers.

## Localization

- Keep language files under `TogawaSakiko/localization/<lang>/`.
- Ensure every gameplay entity has matching localization entries.
