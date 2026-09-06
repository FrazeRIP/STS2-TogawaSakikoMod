# Native-First STS2 Port Workflow

Status: canonical execution workflow

Baseline date: 2026-09-06

Target game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Target engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This workflow supersedes the temporary BaseLib-backed migration phases in `PORT_REFRESH_PLAN.md`. The verified game/repository baseline, reverse-engineering procedure, detailed utility contracts, and later content requirements in that document remain active.

## Decision

BaseLib is the current community-standard convenience layer. The current Alchyr setup guide is explicitly written for BaseLib, all three current templates include it by default, and WatcherMod uses it. It is not an obsolete dependency.

It is nevertheless the wrong runtime architecture for this project's stated delivery requirement:

- A BaseLib mod requires BaseLib's manifest, DLL, and PCK to be installed and loaded separately.
- The current port inherits BaseLib models and uses BaseLib registration, localization, asset, command, and character helpers throughout its source.
- Repairing that version first would produce a useful comparison build, but it would also spend time restoring a framework that the release must remove.
- BaseLib reduces compatibility work across game updates. A self-contained mod accepts that maintenance responsibility itself.

Project decision:

- Do not build or run a temporary BaseLib-backed Togawa checkpoint.
- Do not include the BaseLib NuGet package, manifest dependency, DLL, PCK, or runtime API in the canonical project.
- Treat BaseLib and WatcherMod as read-only behavioral and compatibility references.
- Prefer the installed game's native models, commands, hooks, loader, localization, and PCK behavior.
- Implement only the missing compatibility surface required by Togawa Sakiko.
- If any MIT-licensed implementation is adapted rather than independently implemented, record the exact source and license in a third-party notice.

## Definition of self-contained

The release folder contains one mod:

```text
TogawaSakiko/
  TogawaSakiko.json
  TogawaSakiko.dll
  TogawaSakiko.pck
```

An optional PDB may be included in development output but not required at runtime.

The mod may reference assemblies supplied by the game, including `sts2.dll`, `0Harmony.dll`, and the game's Godot runtime. It must not require another mod or separately installed library at runtime.

Pinned build-only tooling is allowed when it is restored automatically and does not enter the release folder. This can include the Godot .NET SDK, analyzer, publicizer, and PCK packaging tooling. No external source checkout may be required to build the release.

## Repository transition strategy

Preserve the current partial port and all assets as migration evidence. Do not repair its BaseLib compile errors merely to make the legacy implementation run.

The canonical build will be reconstructed in the existing Godot project with two explicit source areas:

```text
TogawaSakiko/
  NativeCode/                 canonical source compiled by default
  TogawaSakikoCode/           current BaseLib-era source, retained as reference
  TogawaSakiko/               PCK source tree and localization/assets
```

Transition rules:

- Exclude `TogawaSakikoCode/**` from the canonical build before adding any replacement type.
- Keep the assembly name, manifest ID, resource root, and stable model IDs as `TogawaSakiko`.
- Port one content unit at a time into `NativeCode`, using the STS1 Java implementation as behavior truth and the old C# file only as migration evidence.
- Never compile legacy and replacement definitions of the same model together.
- Keep incomplete native content abstract, development-only, or unregistered so reflection and reward pools cannot expose it.
- Do not move or remove the legacy source during reconstruction. Its final disposition requires a separate decision after parity is complete.

## Owned architecture

Keep the framework private to this mod. Do not recreate a general-purpose BaseLib.

```text
NativeCode/
  Bootstrap/       one initializer, script registration, startup diagnostics
  Compatibility/   version-gated Harmony patches for proven game gaps
  Content/         explicit model catalog and native pool registration
  Ids/             stable ID mapping and save compatibility assertions
  Assets/          resource paths and Togawa-only presentation overrides
  Localization/    native JSON key validation
  Commands/        Togawa-specific operations over native game commands
  Tracking/        combat-scoped power ledger and related state
  Character/       character, pools, selection entry, and visual scene bridge
  Cards/
  Powers/
  Relics/
  Potions/
```

Architecture rules:

- Use one `[ModInitializer]` and pass `Assembly.GetExecutingAssembly()` to script registration and Harmony.
- Keep Harmony targets in a small explicit patch catalog. Every patch must verify that its target exists for the recorded game version.
- Use native commands instead of directly mutating combat or run collections.
- Prefer explicit registration over broad reflection magic owned by the mod.
- Keep game-version compatibility code separate from content behavior.
- Never use a global patch where a Togawa model type check or native hook can narrow the effect.
- Log the mod version, detected game version, registered content counts, and successful compatibility patches once at startup.

## BaseLib replacement boundary

| Current dependency | Native-first replacement |
| --- | --- |
| `ModInitializer` bootstrap around BaseLib | One game-native initializer, Godot script registration, and one Harmony instance |
| `CustomContentDictionary` and `[Pool]` | Explicit Togawa model catalog plus native `ModHelper.AddModelToPool` registration |
| BaseLib ID prefixing | Owned stable-ID mapping that preserves existing `TOGAWASAKIKO-...` IDs |
| `ConstructedCardModel` / `CustomCardModel` | Direct `CardModel` inheritance plus small Togawa-only variable and asset helpers |
| `CustomPowerModel` | Direct `PowerModel` inheritance and Togawa-only icon path handling |
| `CustomRelicModel` | Direct `RelicModel` inheritance and native/owned icon path handling |
| `CustomPotionModel` | Direct `PotionModel` inheritance and a narrow Togawa potion-image path override if required |
| Custom character and pool model classes | Direct game model inheritance, native pool APIs, and narrowly scoped character registry/UI patches |
| `CardLoc`, `PowerLoc`, and other inline localization providers | Native English and Simplified Chinese JSON in the PCK |
| `CommonActions` | Native `DamageCmd`, `BlockCmd`, `PowerCmd`, `CardPileCmd`, selection, draw, and other command APIs |
| BaseLib asset/path extensions | Small deterministic `res://TogawaSakiko/...` path helpers with existence validation |
| Custom frame, banner, energy, and selection UI patches | Togawa-only interfaces and patches for the exact presentation features used |
| BaseLib save extensions | Native model/run serialization first; add an owned versioned save payload only when a feature proves it is necessary |
| BaseLib hooks | Native game hooks and `ModHelper` subscriptions; Harmony only for missing behavior |
| BaseLib multiplayer helpers | Native deterministic commands and authority rules; add no custom network protocol until required |

## Development workflow

### Local configuration

- Commit a documented example properties file.
- Keep machine-specific game, data, MegaDot, staging-output, and optional local-mod paths in an ignored properties file.
- Point `GodotPath` to `MegaDot_v4.5.1-stable_mono_win64.exe`, not the `GodotSharp` directory.
- Pin every build package version.
- Reference `sts2.dll` and `0Harmony.dll` directly from the selected installed game build.
- Make a normal build side-effect free: it compiles to project output and does not overwrite the live game mod folder.

### Build and package commands

The intended command split is:

1. Restore/build: compile C# only against the recorded game assembly.
2. Import: run MegaDot headlessly with `--import` after asset, localization, scene, material, shader, or audio changes.
3. Publish: export `TogawaSakiko.pck` to an isolated staging folder and copy the matching DLL and manifest there.
4. Validate: inspect filenames, manifest values, PCK paths, assembly references, and dependency absence.
5. Deploy test: explicitly copy the validated staging folder into an isolated local mod set.
6. Launch test: run the game with only required test mods; the self-contained gate uses Togawa alone.

Build must not silently deploy. Deployment must be an explicit command or target.

### Runtime order to preserve

```text
Game discovers TogawaSakiko.json
  -> loads TogawaSakiko.dll
  -> mounts TogawaSakiko.pck into res://
  -> invokes the single initializer
  -> registers mod C# scripts and narrow patches
  -> game merges localization
  -> game constructs model types and assigns stable IDs
  -> Togawa registers eligible content in native pools
  -> game starts normal UI/run flow
```

## Native-first execution checklist

### Phase N0: Complete the evidence baseline

- [x] Record the installed game, assembly, PCK, and MegaDot versions and hashes.
- [x] Decompile the installed `sts2.dll` and trace loader, registration, commands, hooks, and save order.
- [x] Inventory the STS1 source, partial STS2 source, assets, localization, and current BaseLib footprint.
- [x] Confirm that BaseLib is the current community-default workflow but conflicts with this release boundary.
- [ ] Extract the installed PCK with GDRE Tools into a versioned directory outside Git.
- [x] Record the exact native scenes, node topology, resources, and dimensions required by the first character vertical slice.
- [ ] Produce the complete STS1-to-STS2 parity inventory.
- [ ] Classify each old C# BaseLib usage as native API, owned helper, narrow patch, or content rewrite.

Completion evidence: every source content item and every BaseLib-era dependency has an explicit destination before bulk migration begins.

### Phase N1: Prove a clean no-BaseLib loader and package

- [x] Add ignored local path configuration and a committed example.
- [x] Make build, publish, staging, and deploy separate explicit operations.
- [x] Pin the Godot SDK and all build-only package versions.
- [x] Exclude the BaseLib-era source tree from the canonical compilation without removing it.
- [x] Remove BaseLib from canonical package references and generated dependency assets.
- [x] Create a current manifest with no dependencies and the verified minimum game version.
- [x] Implement one initializer using the executing assembly.
- [x] Register mod-owned Godot C# scripts before any scene instantiation.
- [x] Add startup diagnostics and a version guard without registering gameplay models.
- [x] Import and export a minimal PCK containing one native localization/resource probe.
- [x] Validate that the staged folder contains only the expected Togawa files.
- [x] Validate that the DLL has no BaseLib assembly reference and the manifest has no BaseLib dependency.
- [x] Launch the game with BaseLib absent and confirm Togawa appears in the mod list with no Togawa exception.
- [x] Launch once with BaseLib installed but not declared and confirm Togawa behavior is unchanged.

Completion evidence: a clean install loads the DLL and PCK with exactly one initializer and zero BaseLib presence.

Execution record: `PHASE_N1_NATIVE_BOOTSTRAP.md`.

### Phase N2: Prove the native character vertical slice

- [x] Freeze expected IDs for the character, pools, starter cards, starter relic, and first power.
- [x] Implement and test the owned ID policy before registering content.
- [x] Add explicit model catalog and native pool registration.
- [x] Add native English and Simplified Chinese localization validation.
- [x] Implement direct card, power, relic, potion, and pool base types only to the extent required by the slice.
- [x] Implement the minimum custom-character registry patch.
- [x] Implement the minimum character-selection entry and Togawa resource paths.
- [x] Implement one real character scene or an explicitly approved static first-pass scene.
- [x] Port Strike, Defend, the starter relic, one power-dependent card, and that power from STS1 behavior.
- [x] Start a Togawa run, enter and finish combat, receive rewards, save, quit, and reload.
- [x] Verify vanilla character selection and a short vanilla run still work.

Completion evidence: the first playable slice works with stable IDs and no BaseLib installed.

Execution record: `PHASE_N2_NATIVE_CHARACTER_SLICE.md`.

### Phase N3: Implement shared behavior contracts

- [ ] Implement persistent deck add and synchronized deck/combat removal through native commands.
- [ ] Implement the signed current/previous-round power ledger for every player and enemy.
- [ ] Implement safe power cloning through the native clone lifecycle.
- [ ] Implement Hype using the native block-clear hook and one narrow explicit-loss patch.
- [ ] Implement post-victory card gain through `AfterCombatVictory` and the following native save.
- [ ] Execute every acceptance scenario in `PORT_REFRESH_PLAN.md` for these utilities.
- [ ] Add automated tests for pure selection/state logic and real-game tests for hook/command ordering.

Completion evidence: all utility acceptance scenarios pass in a no-BaseLib game session.

### Phase N4: Complete presentation and content inventory

- [ ] Finish character selection, in-combat visuals, top-panel icon, map marker, energy UI, card frames, rest site, merchant, and multiplayer-facing assets.
- [ ] Validate all 95 small/large card-art pairs against stable IDs and runtime paths.
- [ ] Port every power, relic, and potion icon into the parity inventory.
- [ ] Port exact English and Simplified Chinese localization into native JSON.
- [ ] Render every planned model in a development gallery without enabling behaviorless content in normal pools.

Completion evidence: every planned visual/localization entry resolves without missing-resource or missing-key logs.

### Phase N5: Port cards and powers by dependency

- [ ] Implement native-command-only cards first in starter, common, uncommon, rare, token/special, and curse order.
- [ ] Implement base and upgraded behavior together.
- [ ] Port required powers immediately before their dependent cards.
- [ ] Port custom commands and narrow patches only after proving no native API covers the behavior.
- [ ] Verify target, cost, variables, pile destination, ownership, extra turns, and multiplayer determinism.
- [ ] Enable each card in normal pools only after behavior, art, localization, and upgrade checks pass.
- [ ] Leave any non-parity implementation disabled and record its exact blocker.

Completion evidence: all 95 cards are parity-complete and enabled or explicitly disabled with no route into normal play.

### Phase N6: Port relics and potions

- [ ] Port all 12 STS1 relics in dependency order.
- [ ] Port all seven STS1 potions with native targeting and consumption behavior.
- [ ] Implement Monochrome Hairband through the verified post-victory hook/save order.
- [ ] Verify deck-mutating relics through the shared deck command.
- [ ] Verify save/reload, reward pools, duplication/removal, counters, icons, and ownership.

Completion evidence: relic and potion parity is complete through a full character run.

### Phase N7: Port the custom act, events, and enemies

- [ ] Re-audit current act, room, event, encounter, monster, intent, map, victory, and unlock registries.
- [ ] Build The Oblivion as a vertical slice before bulk encounters.
- [ ] Port the two STS1 events and all save/reload branches.
- [ ] Port enemies, elites, bosses, intents, pools, VFX, music, and scenes in dependency order.
- [ ] Keep unfinished rooms, events, and encounters out of generation.
- [ ] Verify map generation, room history, act transition, encounter restart, rewards, and final-victory saves.

Completion evidence: the custom act can be entered, saved, reloaded, completed, and exited without BaseLib.

### Phase N8: Harden and release

- [ ] Run a clean new-game-to-victory test on the recorded game version.
- [ ] Run the complete save/reload matrix.
- [ ] Test with BaseLib absent and with a representative unrelated mod set.
- [ ] Audit model IDs, startup diagnostics, full-run logs, missing assets, localization, and patch targets.
- [ ] Decide and document multiplayer support, then execute the declared support matrix.
- [ ] Build and publish from a clean checkout using only documented local configuration.
- [ ] Verify the release archive contains one self-contained Togawa folder and no undeclared assembly dependency.
- [ ] Record the exact supported game version and known limitations.

Completion evidence: clean build, clean package, clean no-BaseLib load, stable save/reload, full-run proof, and reviewed release archive.

## Non-negotiable gates

The native-first foundation is not complete unless all of these are true:

- No `BaseLib` reference appears in the canonical project, NuGet assets, assembly references, manifest, staged package, or runtime requirement.
- Exactly one initializer runs.
- DLL and PCK are produced from the same source revision.
- Existing `TOGAWASAKIKO-...` model IDs remain stable or have a tested migration.
- All live models have valid native localization and resources.
- No unfinished content can enter normal generation.
- Build does not deploy implicitly.
- Save/reload proves state instead of assuming serialization works.
- Every Harmony patch is narrow, version-audited, and covered by a runtime scenario.
- Vanilla character selection and a short vanilla run remain functional.

## Immediate next work

Execute Phase N3. Preserve the Phase N1 package gates and the Phase N2 playable character regression while implementing the five shared behavior contracts and their acceptance scenarios. Do not expand into bulk card content until those contracts pass in a no-BaseLib game session.

## Current references

- Community setup: https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup
- Community modding basics: https://github.com/Alchyr/ModTemplate-StS2/wiki/Modding-Basics
- Current templates: https://github.com/Alchyr/ModTemplate-StS2
- BaseLib reference: https://github.com/Alchyr/BaseLib-StS2
- WatcherMod reference: https://github.com/lamali292/WatcherMod
