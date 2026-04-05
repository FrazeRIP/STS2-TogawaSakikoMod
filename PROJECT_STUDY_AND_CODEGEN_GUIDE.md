# TogawaSakiko Mod — Project Study & Code Generation Instruction Guide

## 1) Project at a glance

This repository is an **early-stage Slay the Spire 2 character mod scaffold** for a custom character named `TogawaSakiko`.

- Tech stack: **C# / .NET 9**, Godot .NET SDK, STS2 BaseLib, Harmony patches.
- Packaging: outputs both a `.dll` and `.pck`, with post-build copy steps into the STS2 `mods` folder.
- Status: mostly template/placeholder content; localization entries for cards/powers/relics are mostly empty.

## 2) Repository structure and purpose

- `TogawaSakiko/TogawaSakikoCode/` — core C# mod code.
  - `MainFile.cs`: mod initializer (`[ModInitializer]`) and Harmony `PatchAll()` entrypoint.
  - `Character/`: character model and card/relic/potion pool definitions.
  - `Cards/`, `Relics/`, `Powers/`, `Potions/`: abstract base classes for mod content.
  - `Extensions/StringExtensions.cs`: canonical path-build helpers for image resources.
- `TogawaSakiko/TogawaSakiko/` — game resources embedded in package.
  - `localization/eng/*.json`: localization keys by domain.
  - `images/...`: card portraits, power/relic icons, character UI assets.
- `TogawaSakiko/TogawaSakiko.csproj` — build configuration, dependency wiring, and deployment/export targets.
- `TogawaSakiko/Sts2PathDiscovery.props` — OS-dependent STS2 and mods/data path discovery.
- `TogawaSakiko/TogawaSakiko.json` — mod manifest consumed by STS2.
- `TogawaSakiko/project.godot`, `export_presets.cfg` — Godot project and export settings.

## 3) Runtime architecture summary

### 3.1 Initialization and patching

`MainFile.Initialize()` is the runtime entrypoint and applies all Harmony patches via `PatchAll()` under mod id `TogawaSakiko`.

### 3.2 Character model and pools

`Character/TogawaSakiko.cs` defines a `PlaceholderCharacterModel`:

- Character id: `TogawaSakiko`
- Name color: white (`ffffff`)
- Starting HP: `70`
- Starting deck and relics currently use Ironclad starter cards and Burning Blood (template defaults)
- Binds pool models:
  - `TogawaSakikoCardPool`
  - `TogawaSakikoRelicPool`
  - `TogawaSakikoPotionPool`
- Provides custom character UI icon paths.

### 3.3 Content base classes and conventions

- `TogawaSakikoCard`:
  - Tagged into `TogawaSakikoCardPool` with `[Pool]`
  - Derives card image names from `Id.Entry.RemovePrefix().ToLowerInvariant()`.
- `TogawaSakikoRelic` and `TogawaSakikoPower`:
  - Same id-based image derivation convention.
  - Fallback to default placeholder image when specific file does not exist.
- `TogawaSakikoPotion`:
  - Abstract pool-bound base class only (no custom behavior yet).

### 3.4 Localization and assets

Localization files exist and are wired, but card/power/relic/keyword content is mostly empty JSON placeholders.

Images include placeholder defaults:

- `card_portraits/card.png`, `card_portraits/big/card.png`
- `powers/power.png`, `powers/big/power.png`
- `relics/relic.png`, `relics/big/relic.png`, `relics/relic_outline.png`
- character UI icons/energy icons

## 4) Build and deploy pipeline

`TogawaSakiko.csproj`:

- Uses `Godot.NET.Sdk/4.5.1` and `net9.0`.
- References game DLLs (`0Harmony.dll`, `sts2.dll`) from `$(Sts2DataDir)`.
- Uses BaseLib and analyzers.
- Includes build targets to:
  - copy DLL + manifest to `$(ModsPath)` after build,
  - copy quick packed `.pck` when available,
  - export Godot `.pck` after publish when `GodotPath` exists.

`CheckDependencyPaths` target hard-fails when `Sts2DataDir` or `GodotPath` are not configured/found.

## 5) Current gaps and technical debt

1. **Template starter gameplay still present**
   - Ironclad strikes/defends and Burning Blood remain as starting loadout.
2. **No concrete custom cards/relics/powers/potions implemented**
   - Only abstract base classes exist.
3. **Localization incomplete**
   - Most content files are empty.
4. **Asset set mostly placeholders**
   - Real per-id assets missing.
5. **Likely build friction for new contributors**
   - Hard dependency on local STS2 + Godot executable path setup.

## 6) Instruction guide for AI code generation in this repo

Use this section as the canonical instruction contract for future codegen.

### 6.1 Core principles

1. **Preserve BaseLib/STS2 conventions**
   - Keep `Id.Entry.RemovePrefix().ToLowerInvariant()` naming strategy when deriving resource paths.
2. **Additive changes over broad rewrites**
   - Extend existing class hierarchy and pool architecture instead of replacing it.
3. **Keep gameplay and localization in sync**
   - Any new card/relic/power/potion must include localization keys and image placeholders.
4. **Prefer explicit, deterministic ids**
   - Stable class names and id naming for analyzer friendliness.

### 6.2 Required checklist per new gameplay object

#### New Card

- Create concrete card class in `TogawaSakikoCode/Cards/` inheriting `TogawaSakikoCard`.
- Register/ensure discoverability per framework conventions used by BaseLib attributes.
- Add `cards.json` localization entries (name/description).
- Add image(s):
  - `TogawaSakiko/images/card_portraits/<id>.png`
  - optionally `.../big/<id>.png`.

#### New Relic

- Create class in `Relics/` inheriting `TogawaSakikoRelic`.
- Add `relics.json` localization (name/description/flavor if needed).
- Add images:
  - `images/relics/<id>.png`
  - optional `images/relics/<id>_outline.png`
  - optional `images/relics/big/<id>.png`.

#### New Power

- Create class in `Powers/` inheriting `TogawaSakikoPower`.
- Add `powers.json` localization.
- Add images:
  - `images/powers/<id>.png`
  - optional `images/powers/big/<id>.png`.

#### New Potion

- Create class in `Potions/` inheriting `TogawaSakikoPotion`.
- Ensure potion pool includes/permits it as required by framework logic.
- Add localization and image resources if potion presentation requires them.

### 6.3 Character-level change checklist

When editing `Character/TogawaSakiko.cs`:

- Keep `CharacterId` stable unless intentionally performing a migration.
- If changing starter deck/relic:
  - ensure all referenced models exist and are loadable,
  - ensure localization exists for all newly referenced custom content.
- If changing UI asset paths:
  - verify files exist under `images/charui`.

### 6.4 Localization rules

- Never leave dangling keys without implementation references.
- Keep english localization valid JSON with no comments/trailing commas.
- Populate at minimum:
  - character title/description fields,
  - names + descriptions for each concrete card/relic/power/potion.
- Use consistent key prefixes: `TOGAWASAKIKO-TOGAWA_SAKIKO...` for character-level strings.

### 6.5 Asset naming rules

- File names should match normalized id entry used by existing path code:
  - lowercase,
  - prefix removed by `RemovePrefix()`,
  - `.png` extension.
- Prefer adding specific asset files rather than relying on fallback placeholders.

### 6.6 Build safety rules

Before proposing completion, codegen should verify:

1. `.csproj` still includes localization as `AdditionalFiles`.
2. No resource path helper was bypassed with hard-coded inconsistent paths.
3. New files are under the correct resource subtree.
4. Mod manifest (`TogawaSakiko.json`) remains valid and coherent.

### 6.7 Suggested implementation order for large feature work

1. Add localization skeleton keys.
2. Add concrete gameplay classes.
3. Add images/placeholders for each id.
4. Wire character/pool access and starter content.
5. Run build/check workflow and fix analyzer issues.

### 6.8 Prompt template for future AI tasks

Use this prompt template when asking an AI to implement features in this repo:

> Implement `<feature>` for the TogawaSakiko STS2 mod.
> Constraints:
> 1) Follow existing class hierarchy and resource path helper conventions.
> 2) Add/maintain localization JSON entries for all new gameplay objects.
> 3) Add matching image file placeholders using id-based lowercase naming.
> 4) Keep manifest and project build targets intact.
> 5) Provide a brief change summary and list of any missing external prerequisites (STS2 path, Godot path).

### 6.9 Anti-patterns to avoid

- Do not hardcode absolute local machine paths outside existing configurable props/csproj properties.
- Do not introduce inconsistent naming between class id and asset filename.
- Do not add gameplay classes without localization.
- Do not remove fallback behavior in relic/power icon loaders unless replacing with guaranteed complete asset generation.

## 7) Recommended next implementation milestones

1. Replace Ironclad starter loadout with first-pass custom TogawaSakiko cards/relic.
2. Fill localization files (`cards.json`, `relics.json`, `powers.json`, `card_keywords.json`).
3. Add real assets matching id naming conventions.
4. Add at least one playable synergy loop (e.g., unique resource mechanic).
5. Add smoke validation script/docs for environment setup and publish flow.

---

This document is intentionally practical and codegen-oriented so both human contributors and AI agents can make consistent, low-regression updates.
