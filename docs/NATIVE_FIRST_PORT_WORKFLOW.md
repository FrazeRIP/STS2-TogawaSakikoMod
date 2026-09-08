# Native-First STS2 Port Workflow

> Current-state review, 2026-09-07: N1-N7 gameplay and September 7 feedback are implemented. Current starting choices are Another Mask, The Third Movement and Blazing Hairband using supplied cavern art; Amnesia is historical. The user authorized this checkpoint commit, push and merge to main, superseding the earlier wait-for-phrase instruction. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Status: canonical execution workflow

Baseline date: 2026-09-06

Target game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Target engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This workflow supersedes the temporary BaseLib-backed migration phases in `PORT_REFRESH_PLAN.md`. The verified game/repository baseline, reverse-engineering procedure, detailed utility contracts, and later content requirements in that document remain active.

Scope boundary: the nine cards disabled in STS1 are skipped and excluded from completion. Custom acts, events, enemies, encounters, intents, and their exclusive ending, cutscene, music, and presentation assets are also outside this port and must remain absent from canonical runtime code, generated localization, the PCK allowlist, and completion totals.

User-approved exception, 2026-09-07: Sakiko's normal starting room is now Ocean of Memories, with one generated full-screen static image and three fixed choices: Amnesia removes Monochrome Hairband only; The Third Movement grants the original three-use relic while keeping Monochrome; Blazing Hairband replaces Monochrome. This single opening-room addition supersedes the event exclusion above for its own code, localization, scene, and `images/events/ocean_of_memories.png` only. The former STS1 custom events, acts, bosses, and endings remain excluded. Amnesia does not replace Defends or modify the starting deck.

Subsequent user revision, 2026-09-07: the supplied Neow cavern artwork replaces the ocean image, and the opening now describes Sakiko waking in an unknown place and receiving a gift from Neow before being directed to seek answers at the tower's summit. Another Mask replaces the Amnesia option: obtain the new white mask relic, replace Monochrome Hairband, and replace the starting Defends with Desire. Carrying the mask changes Sakiko's combat sprite; Master of Melodia takes visual precedence and restores the appropriate base or masked sprite on exit. These instructions supersede the earlier Amnesia-only behavior above.

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
- [x] Produce the complete STS1-to-STS2 parity inventory.
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

- [x] Implement persistent deck add and synchronized deck/combat removal through native commands.
- [x] Implement the signed current/previous-round power ledger for every player and enemy.
- [x] Implement safe power cloning through the native clone lifecycle.
- [x] Implement Hype using the native block-clear hook and one narrow explicit-loss patch.
- [x] Implement post-victory card gain through `AfterCombatVictory` and the following native save.
- [x] Execute every applicable acceptance scenario in `PORT_REFRESH_PLAN.md` for these utilities.
- [x] Add automated tests for pure selection/state logic and real-game tests for hook/command ordering.

Completion evidence: all utility acceptance scenarios pass in a no-BaseLib game session.

Execution record: `PHASE_N3_SHARED_BEHAVIOR_CONTRACTS.md`. Multiplayer-conditional scenarios remain explicitly unverified because multiplayer support is not enabled.

### Phase N4: Complete presentation and content inventory

- [x] Finish character selection, in-combat visuals, top-panel icon, map marker, energy UI, card frames, rest site, merchant, and multiplayer-facing assets.
- [x] Validate all 95 small/large card-art pairs against stable IDs and runtime paths.
- [x] Port every power, relic, and potion icon into the parity inventory.
- [x] Port exact English and Simplified Chinese localization into native JSON.
- [x] Render every planned model in a development gallery without enabling behaviorless content in normal pools.

Completion evidence: every planned visual/localization entry resolves without missing-resource or missing-key logs.

Inventory record: `FULL_PORT_PARITY_INVENTORY.md` and `FULL_PORT_PARITY_INVENTORY.json`. The source sync is reproducible through `tools/Sync-Sts1PresentationAssets.ps1`; it proves 94 original card-art pairs, one exact-resolution generated placeholder pair, 25 in-scope player-card-relevant power icon pairs, 11 relic icon sets, and six potion layer sets. The sync also removes and refuses to recopy every excluded act/event/enemy asset and custom-enemy compatibility power asset. Runtime presentation, localization, gallery, package, save/reload, and vanilla regression evidence is recorded in `PHASE_N4_COMPLETE_PRESENTATION_AND_INVENTORY.md`.

### Phase N5: Port cards and powers by dependency

- [x] Implement native-command-only cards first in starter, common, uncommon, rare, token/special, and curse order.
- [x] Implement base and upgraded behavior together.
- [x] Port required powers immediately before their dependent cards.
- [x] Port custom commands and narrow patches only after proving no native API covers the behavior.
- [x] Verify target, cost, variables, pile destination, ownership, extra turns, and single-player determinism; the declared multiplayer support decision remains an N7 gate.
- [x] Enable each card in normal pools only after behavior, art, localization, and upgrade checks pass.
- [x] Keep every STS1-disabled card outside normal play and exclude it from completion accounting.

Completion evidence: all 86 cards enabled in STS1 are parity-complete and enabled through their intended reward/token/curse routes. All nine cards disabled in STS1 are excluded from the port and have no route into normal play.

Current execution record: `PHASE_N5_CARDS_AND_POWERS.md`. The authoritative inventory reports 86 of 86 in-scope cards and 23 of 25 player-card-relevant STS1 powers native, plus the native Melodia support model. The remaining two powers belong to potion behavior and move with their dependent potions in N6. The complete card/power matrix, both language loaders, N3 save/reload contracts, Kings lifecycle, Sakiko regression, and vanilla regression pass with zero managed issues. Phase N5 is complete.

### Phase N6: Port relics and potions

- [x] Port all 11 concrete STS1 relics in dependency order.
- [x] Port all six concrete STS1 potions with native targeting and consumption behavior.
- [x] Implement Monochrome Hairband through the verified post-victory hook/save order.
- [x] Verify deck-mutating relics through the shared deck command.
- [x] Verify save/reload, reward pools, duplication/removal, counters, icons, and owner/participant filtering in the declared single-player support scope.

Completion evidence: relic and potion parity is complete through a full character run.

Execution record: `PHASE_N6_RELICS_AND_POTIONS.md`. The authoritative inventory reports 11 of 11 relics, six of six potions, and 25 of 25 player-relevant powers native. The focused installed-game contract, both language loaders, N3 and N5 regressions, Sakiko save/quit/reload, and vanilla regression pass with zero managed issues. Phase N6 is complete; the later N7 run passed natural new-game-to-victory and declared multiplayer unsupported for v0.1.0.

### Phase N7: Harden and release

- [x] Run a clean new-game-to-victory test on the recorded game version.
- [x] Run the complete save/reload matrix.
- [x] Test with BaseLib absent and with a representative unrelated mod set.
- [x] Audit model IDs, startup diagnostics, full-run logs, missing assets, localization, and patch targets.
- [x] Decide and document multiplayer support. Multiplayer is unsupported in v0.1.0, so the conditional host/client matrix does not apply.
- [x] Build and publish from an isolated clean source snapshot using only documented local configuration; a clean Git checkout remains pending only on explicit checkpoint authorization.
- [x] Verify the release archive contains one self-contained Togawa folder and no undeclared assembly dependency.
- [x] Record the exact supported game version and known limitations.

Completion evidence: clean build, clean package, clean no-BaseLib load, stable save/reload, full-run proof, and reviewed release archive.

Execution record: `PHASE_N7_HARDEN_AND_RELEASE.md`. The natural run reached victory on `v0.111.0` (`41cef1ea`), all six lifecycle saves and the final profile reloaded, no disabled card was generated, both localization loaders and all focused regressions passed, and the reviewed v0.1.0 archive is self-contained. Phase N7 is complete for the declared singleplayer support boundary.

## Non-negotiable gates

The native-first foundation is not complete unless all of these are true:

- No `BaseLib` reference appears in the canonical project, NuGet assets, assembly references, manifest, staged package, or runtime requirement.
- Exactly one initializer runs.
- DLL and PCK are produced from the same source revision.
- Existing `TOGAWASAKIKO-...` model IDs remain stable or have a tested migration.
- All live models have valid native localization and resources.
- No unfinished content can enter normal generation.
- No custom act, event, enemy, encounter, intent, or exclusive ending asset is present in the release package.
- Build does not deploy implicitly.
- Save/reload proves state instead of assuming serialization works.
- Every Harmony patch is narrow, version-audited, and covered by a runtime scenario.
- Vanilla character selection and a short vanilla run remain functional.

## Immediate next work

Port execution is complete for the declared v0.1.0 singleplayer scope. Keep the validated package installed, preserve the disabled-card and custom-world exclusions, and wait for the user's explicit checkpoint phrase before committing or pushing the uncommitted port.

Future multiplayer work is separately specified in `MULTIPLAYER_PORT_REQUIREMENTS_AND_TODO.md`; it does not change the v0.1.0 support claim.

## Current references

- Community setup: https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup
- Community modding basics: https://github.com/Alchyr/ModTemplate-StS2/wiki/Modding-Basics
- Current templates: https://github.com/Alchyr/ModTemplate-StS2
- BaseLib reference: https://github.com/Alchyr/BaseLib-StS2
- WatcherMod reference: https://github.com/lamali292/WatcherMod
