# STS1 → STS2 File-Structure Alignment Plan

## Goal

Make this STS2 repository feel structurally similar to the STS1 `FrazeRIP/STS-TogawaSakikoMod` repo **without breaking STS2-specific build/runtime requirements**.

The target is *structural familiarity* (folder layout, domain grouping, resource organization), not a literal toolchain port (STS1 is Maven/Java + ModTheSpire; STS2 here is C#/Godot/BaseLib).

---

## 1) Current-state comparison

### STS1 reference layout (observed)

- Java source root: `src/main/java/togawasakikomod/...`
- Domain-heavy package grouping under mod root:
  - `Actions`, `annotations`, `cards`, `character`, `dungeons`, `effects`, `events`, `helpers`, `intents`,
    `modifiers`, `monsters`, `others`, `patches`, `potions`, `powers`, `relics`, `rewards`, `rooms`, `saveable`,
    `scenes`, `screens`, `util`
- Resource root: `src/main/resources/togawasakikomod/...`
  - `audio`, `images`, `localization`
  - root `ModTheSpire.json`

### STS2 current layout (this repo)

- C# source root: `TogawaSakiko/TogawaSakikoCode/...`
  - currently: `Cards`, `Character`, `Extensions`, `Potions`, `Powers`, `Relics`, `MainFile.cs`
- Resource root: `TogawaSakiko/TogawaSakiko/...`
  - `images`, `localization`
- Build manifests/config:
  - `TogawaSakiko.csproj`, `TogawaSakiko.json`, `project.godot`, `export_presets.cfg`, `Sts2PathDiscovery.props`

---

## 2) Alignment strategy

### Principle A — Keep STS2 build assumptions stable

Do **not** move or rename:

- `TogawaSakiko/TogawaSakiko.json`
- `TogawaSakiko/project.godot`
- `TogawaSakiko/TogawaSakiko.csproj`
- existing resource root `TogawaSakiko/TogawaSakiko/`

These are tightly coupled to current packaging/export targets.

### Principle B — Make source/resource subtrees mirror STS1 mental model

Within STS2 constraints, create STS1-like domain folders under the existing C# and resource roots.

---

## 3) Proposed target structure (STS2, STS1-style)

```text
TogawaSakiko/
  TogawaSakikoCode/
    Core/
      MainFile.cs
      ModConfig.cs
      Ids.cs
    Actions/
    Annotations/
    Cards/
      Common/
      Uncommon/
      Rare/
      Special/
    Character/
    Dungeons/
    Effects/
    Events/
    Helpers/
    Intents/
    Modifiers/
    Monsters/
      Bosses/
      Elites/
      Normal/
    Others/
    Patches/
    Potions/
    Powers/
      Buffs/
      Debuffs/
    Relics/
      Starter/
      Common/
      Uncommon/
      Rare/
      Boss/
      Shop/
      Event/
    Rewards/
    Rooms/
    Saveables/
    Scenes/
    Screens/
    Util/
    Extensions/

  TogawaSakiko/
    images/
      cards/
      character/
      powers/
      relics/
      potions/
      ui/
      vfx/
      map/
      screens/
    audio/
    localization/
      eng/
      zhs/
```

Notes:

- `card_portraits` can be retained for compatibility, but adding `images/cards/` as a normalized home for new assets improves STS1 familiarity.
- Keep current path helpers; extend them for new folders rather than hard-switching all old paths at once.

---

## 4) Step-by-step migration plan

## Phase 0 — Preparation (no behavior change)

1. Add a repo architecture doc (`docs/ARCHITECTURE.md`) that states the target structure and migration rules.
2. Add a naming convention doc (`docs/NAMING.md`) for IDs, file names, and folder ownership.
3. Freeze new content placement rules for contributors during migration.

**Deliverable:** documentation only.

## Phase 1 — Source-tree normalization

1. Create new top-level domain folders under `TogawaSakikoCode/` (Actions, Effects, Events, Intents, Patches, Rewards, Rooms, Saveables, Screens, Util, etc.).
2. Move files only where natural equivalents exist now:
   - `MainFile.cs` → `Core/MainFile.cs`
   - keep namespace compatibility by updating namespaces incrementally.
3. Add `global using`/namespace update pass to prevent compile churn.

**Deliverable:** STS1-like source tree, no gameplay changes.

## Phase 2 — Resource-tree normalization

1. Add `audio/` root under `TogawaSakiko/TogawaSakiko/`.
2. Add `images/ui`, `images/vfx`, `images/map`, `images/screens`, and optional `images/cards` alias layout.
3. Keep old asset paths working; introduce additional helper methods for new folders.

**Deliverable:** STS1-like resources organization with backward-compatible asset loading.

## Phase 3 — Path helper and ID utility hardening

1. Split path logic into a dedicated utility (e.g., `Util/ResourcePaths.cs`).
2. Keep `StringExtensions` as compatibility shim and mark old helpers as preferred/legacy where needed.
3. Centralize string IDs in `Core/Ids.cs` (cards, relics, powers, keywords, events).

**Deliverable:** stable path + id layer that supports both old and new layout during transition.

## Phase 4 — Localization parity model

1. Preserve existing STS2 localization files.
2. Add a stricter folder contract mirroring STS1 language split (`eng`, `zhs`, etc.).
3. Add schema/check tooling (or CI script) that validates required keys per content type.

**Deliverable:** STS1-like localization workflow with STS2 file compatibility.

## Phase 5 — Domain expansion scaffolding

1. Add empty scaffold classes/interfaces in newly introduced domains (Events, Monsters, Rewards, Rooms, Saveables, Screens).
2. Add one end-to-end example per domain category to prove wiring.
3. Keep compile targets and packaging unchanged.

**Deliverable:** feature-ready skeleton resembling STS1 repo’s breadth.

## Phase 6 — Cleanup and lock-in

1. Remove dead paths and deprecated comments after all content migrates.
2. Update contributor guide and codegen instructions to require the new layout.
3. Add CI checks that reject files added to deprecated paths.

**Deliverable:** completed structural migration with guardrails.

---

## 5) Mapping table (STS1 concept → STS2 destination)

- `src/main/java/togawasakikomod/cards` → `TogawaSakiko/TogawaSakikoCode/Cards`
- `.../character` → `.../Character`
- `.../potions` → `.../Potions`
- `.../powers` → `.../Powers`
- `.../relics` → `.../Relics`
- `.../actions` → `.../Actions`
- `.../events` → `.../Events`
- `.../monsters` → `.../Monsters`
- `.../patches` → `.../Patches`
- `.../saveable` → `.../Saveables`
- `src/main/resources/togawasakikomod/images` → `TogawaSakiko/TogawaSakiko/images`
- `src/main/resources/togawasakikomod/audio` → `TogawaSakiko/TogawaSakiko/audio`
- `src/main/resources/togawasakikomod/localization` → `TogawaSakiko/TogawaSakiko/localization`

---

## 6) Risks and mitigations

1. **Namespace churn risk** when moving C# files.
   - Mitigation: batch-move by domain and compile after each domain move.
2. **Broken asset path risk** during resource normalization.
   - Mitigation: compatibility path helpers + staged asset copy before path cutover.
3. **Tooling mismatch risk** (STS1 Java assumptions vs STS2 C#/Godot build).
   - Mitigation: never mirror Maven artifacts; mirror only content-domain layout.
4. **Contributor confusion during transition.**
   - Mitigation: temporary migration doc + “new files go here” enforced in PR template.

---

## 7) Suggested execution order (practical)

1. Docs + folder scaffolding (small PR)
2. Core/source moves (small PRs by domain)
3. Resource helper extensions (small PR)
4. Resource directory expansion + sample asset moves (small PR)
5. Localization policy + validation script (small PR)
6. Final cleanup + CI guardrails (small PR)

This keeps each PR low-risk and reviewable while steadily converging toward an STS1-familiar structure.
