# STS1 to STS2 Port Refresh Plan

> Execution update, 2026-09-06: the temporary BaseLib-backed checkpoint in this document is superseded by `NATIVE_FIRST_PORT_WORKFLOW.md`. Keep this document for its verified baseline, reverse-engineering notes, utility contracts, and later content phases, but do not execute its BaseLib-backed Phase 1 or BaseLib-removal Phase 2.

> Current checkpoint, 2026-09-07: native-first Phases N1 through N4 are complete. Phase N5 is active, with its first dependency-ordered card batch accepted and recorded in `PHASE_N5_CARDS_AND_POWERS.md`; the current inventory reports 11 of 95 cards native.

> Historical checkpoint, 2026-09-06: native-first Phases N1 through N4 were complete, with Phase N5 card/power behavior work next.

Status: canonical execution checklist for resuming the port
Baseline date: 2026-09-06
Target game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`
Target engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This document supersedes the implementation order and current-state claims in `PROJECT_STUDY_AND_CODEGEN_GUIDE.md`, `STS1_TO_STS2_STRUCTURE_PLAN.md`, `CARDS_PLAN.md`, and `docs/STS2_BASELIB_WIKI.md`. Those files remain useful historical references, but they describe an older game/template state and a BaseLib-dependent architecture.

## Goal and definition of self-contained

Resume the Java STS1 mod port without discarding the partial C# port already in this repository.

The released mod must be self-contained at runtime:

- A player installs one `TogawaSakiko` folder containing the mod manifest, DLL, and PCK.
- The manifest has no third-party mod dependency, including BaseLib.
- The mod may use `sts2.dll` and `0Harmony.dll` because they are supplied by the game and are part of the mod platform.
- Build-only SDKs, analyzers, the publicizer, and packers may remain NuGet dependencies when useful, but their versions must be pinned and they must not be required in the installed mod folder.
- If small portions of MIT-licensed reference code are adapted, keep only the required portions, rewrite them around this mod's needs, and add the required attribution to a third-party notice file.

## Review of the proposed sequence

The proposed sequence is directionally correct. The following changes make it safer and easier to verify:

1. Separate recovery from dependency removal. First make the current BaseLib-backed port compile and load on the current game. Then replace BaseLib behind verified compatibility contracts. Otherwise game API drift and framework-removal failures are mixed together.
2. Prove one vertical slice before bulk visual work. The first slice is character selection, a playable character, the starter deck, one card, one power, and the starter relic. Once that works without BaseLib, expand presentation to all content.
3. Preserve IDs before changing base classes. Existing local modded progress already contains `TOGAWASAKIKO-...` model IDs. Native `ModelDb` derives IDs from type names and will drop BaseLib's prefix unless the mod owns an equivalent narrow ID policy.
4. Do not register behaviorless stubs into live reward pools. A presentation stub can exist in a development catalog, but a card, relic, potion, encounter, event, or enemy enters normal generation only after its minimum behavior is correct.
5. Treat utilities as behavior contracts, not as a general replacement for BaseLib. Build only the registration, UI, command, hook, and save facilities required by this mod.
6. Add explicit gates for localization, save/reload, clean no-BaseLib installation, logs, deterministic multiplayer behavior, and refreshes after game updates.
7. Delay the custom act, events, and enemies until the character's core combat loop is stable. They touch more hard-coded registries and scene/UI paths than cards, powers, relics, and potions.

## Verified baseline

### Installed game and tools

| Item | Verified value |
| --- | --- |
| Game | `v0.111.0`, commit `41cef1ea`, build date 2026-08-13 |
| `sts2.dll` | 9,757,184 bytes; SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` |
| Game PCK | 1,990,705,700 bytes; SHA-256 `C60F672EE7804E6AEFA1E19A582FA1C80B126A7B0EEF4D084D3ABF110DF2EAB7` |
| MegaDot | `4.5.1.m.14.mono.custom_build` |
| Local MegaDot executable | `X:\Projects\SlayTheSpire2\Godot\megadot-4.5.1-m.14-windows-x86_64-llvm-editor-csharp\MegaDot_v4.5.1-stable_mono_win64.exe` |
| .NET target | .NET 9 |
| Decompiler | ILSpy command-line tool; exact installed `sts2.dll` was decompiled successfully |
| Game asset extractor | GDRE Tools is not currently installed; game PCK recovery remains an unchecked research item |

`GodotSharp` is a runtime/API subdirectory. `GodotPath` must point to the MegaDot executable in its parent directory, not to the `GodotSharp` directory.

### Reference snapshots reviewed

| Reference | Snapshot | How it should be used |
| --- | --- | --- |
| Alchyr character template | `55ca2c606e6c78dd39689a5cf979b243a49652e7` | Current build, manifest, initializer, path discovery, and publish conventions; do not overwrite this repository with it |
| Alchyr template wiki | `4a931fb64c341a5a33d3c4f2a0f1d9bab36a3f8a` | Current setup, decompile, PCK, logging, and debugging workflow |
| WatcherMod | `652ea85a18fda9d1f35e909d14b95f3845bc211a` | Current content behavior and compatibility-patch examples; not a template and not proof of a no-BaseLib architecture |
| BaseLib | `22757933ba10adc4322a628519a233a567507d87` / package `3.4.5` | Temporary migration scaffold and MIT-licensed implementation reference only |
| STS1 source | `7e0314b784082823699b94b256a4b048ef1617a0` | Behavioral and localization source of truth |
| Current STS2 port | `fc06ec948766062d433ae8430257847e6c71bd1e` | Preserve and repair; do not regenerate from an empty template |

### Current repository inventory

This subsection is the repository snapshot captured at the start of the refresh. It is retained as migration evidence; the current canonical native implementation is described by the N1 and N2 execution records.

- 125 C# source files are present: 124 under `TogawaSakikoCode` plus the older project-root initializer. The main groups are 96 under Cards, 17 under Powers, four under Character, and two under Relics.
- Cards currently represent the full 95-card STS1 visual catalog plus one card base class. There are 95 small and 95 large card portraits, with matching filenames.
- The STS1 project has 277 Java files, including 61 actions, 97 card files, 37 powers, 12 relic files, seven potion files, 12 monsters, 18 patches, seven effects, two events, and one dungeon. The concrete model totals are 95 cards, 36 powers, 11 relics, and six potions after excluding infrastructure and abstract bases.
- The STS2 port has only a starter relic and a potion base class. Events, monsters, the custom act, rewards, rooms, effects, and most relic/potion content are absent or empty folders.
- 120 of 125 C# files import BaseLib. Removing BaseLib is therefore a migration, not a project-file-only change.
- 44 source files contain TODO, placeholder, skeletal, or approximation markers. Approximate behaviors are not parity-complete.
- 111 files provide English localization inline through BaseLib. The committed native JSON files contain only a few placeholder entries, and there is no `zhs` directory. The STS1 repository contains both English and Simplified Chinese catalogs.
- No Godot scenes, resources, shaders, or GDScript files are tracked. The character still inherits `PlaceholderCharacterModel`, so the actual in-combat character presentation is not ported.
- The character-selection/icon files are still template-named placeholders even though source art from STS1 has been copied elsewhere.
- Existing model IDs must remain stable. Renaming classes or removing BaseLib's ID prefix without a compatibility patch would invalidate cards, character statistics, discoveries, and saved runs.

### Current build result

This subsection records the legacy BaseLib-era build result at refresh start. It is not the current canonical build result.

An isolated build was run against the installed game and current MegaDot, with deployment redirected to a temporary mods directory. Restore succeeds, then compilation stops with five API-drift errors:

- `DolorisCard.OnTurnEndInHand` now needs the protected access level used by `CardModel`.
- `DazzlingDownPower.AfterTurnEnd` no longer overrides a current hook.
- `FearlessPower.BeforeTurnEnd` no longer overrides a current hook.
- `SeizeTheFatePower.AfterTurnEnd` no longer overrides a current hook.
- `SharedDestinyPower.AfterPowerAmountChanged` is missing the current `PlayerChoiceContext` parameter.

This is only the first compile barrier. A successful compile may reveal analyzer, initializer, model registration, localization, resource, or runtime errors.

### Other known project drift

- `GodotPath` points to a missing older installation instead of the verified MegaDot executable.
- `Sts2PathDiscovery.props` can overwrite a registry-discovered or supplied Windows path with the default Steam path. Use the current template's conditional fallback logic.
- Two classes have `[ModInitializer]`; the game invokes every attributed initializer, so Harmony is currently patched twice.
- The initializer calls parameterless `PatchAll()` and does not register C# Godot scripts. Use the executing assembly explicitly for both operations.
- The manifest has no `min_game_version` and uses the legacy string dependency form. Current manifests use dependency objects with `id` and `min_version`.
- The project uses wildcard package versions. Pin versions so a restore cannot silently change the framework during a port phase.
- `project.godot` references a missing `icon.svg` and lacks current template display settings.
- The current build target treats a missing Godot executable as a build failure even when only C# compilation is requested. Godot must be required for publish, not necessarily for code-only build.

## How the current game/mod workflow works

### Runtime order

```text
Discover manifests and Workshop/local mod folders
  -> validate game versions and dependency graph
  -> load each mod DLL into the game's AssemblyLoadContext
  -> mount that mod's PCK into Godot's res:// filesystem
  -> invoke every [ModInitializer] in the DLL
  -> initialize localization from base and mounted mod paths
  -> reflect over game and mod AbstractModel subclasses
  -> construct canonical models and assign ModelIds
  -> preload model assets and start normal game flow
```

Consequences:

- The DLL contains C# logic. The PCK contains Godot resources such as imported textures, scenes, materials, shaders, localization JSON, and audio.
- The PCK is mounted before the initializer runs. Initializer code can therefore address resources such as `res://TogawaSakiko/images/...`.
- A C# class attached to a mod-owned `.tscn` must be registered with `Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly)` before the scene is instantiated.
- Every concrete mod `AbstractModel` is discovered by reflection and constructed with a public parameterless constructor. A behaviorless concrete model can still affect initialization even when it is not in a reward pool.
- Native `ModHelper.AddModelToPool` and `ModHelper.SubscribeForCombatStateHooks`/`SubscribeForRunStateHooks` cover a useful part of BaseLib's old role.
- Native card, relic, and potion pools already concatenate models registered through `ModHelper`.
- `ModelDb.AllCharacters` is still a hard-coded five-character list in `v0.111.0`; a custom character needs a small owned Harmony extension. Other hard-coded lists must be audited only when their content type is implemented.
- Native IDs are category plus slugified type name. The current BaseLib layer prefixes custom entries with the mod ID. The owned layer must reproduce the existing `TOGAWASAKIKO-` IDs before BaseLib is removed.
- STS2 combat commands and hooks are multiplayer-aware. Direct mutation of private lists bypasses history, UI, hook, save, replay, and synchronization behavior.

### Build and PCK workflow

- `dotnet build` is the fast path for C#-only changes. It should copy the DLL, manifest, and PDB to a selected test output only after a successful build.
- `dotnet publish` is required after any localization, image, scene, material, shader, or audio change.
- Before exporting scene-based content, run MegaDot headlessly with `--import` so source assets and scene dependencies have current import data.
- Export the asset pack with MegaDot `--headless --export-pack BasicExport <output.pck>` from the directory containing `project.godot`.
- Validate the produced folder contains matching `TogawaSakiko.dll`, `TogawaSakiko.pck`, and `TogawaSakiko.json` before copying it to the game.
- A PCK made by a newer Godot version may not load in this game. Use the verified MegaDot build unless the game's engine version changes.
- Normal `build` does not refresh non-code assets. Testing asset work after only a build produces stale results.

### Reverse-engineering workflow after each game update

1. Read `release_info.json` and record the game version, commit, date, and `sts2.dll` SHA-256.
2. Decompile the exact installed `sts2.dll` into an ignored, version-named working directory with ILSpy.
3. Keep `sts2.xml` beside the DLL as the first source for public API signatures; use decompiled code to trace control flow and hard-coded lists.
4. Recover the exact installed `SlayTheSpire2.pck` with GDRE Tools when scene, asset, text, or node topology is relevant. Keep recovered game files outside Git.
5. Diff the previous and new public APIs used by this mod, then compile before changing behavior.
6. Re-audit each owned Harmony patch against its target method and fail clearly when a target is absent or structurally incompatible.
7. Run the loader, model-ID, utility, save/reload, and presentation smoke gates before updating `min_game_version`.

Do not copy decompiler artifacts such as generated collection types, publicizer annotations, or reconstructed state-machine code directly into the mod.

## Target self-contained architecture

Keep the owned framework narrow and specific to Togawa Sakiko:

- `Core/Bootstrap`: one initializer, assembly script registration, ordered patch catalog, content registration, and startup diagnostics.
- `Core/Compatibility`: game-version checks and narrow Harmony patches for hard-coded registries or missing native hooks.
- `Core/Ids`: explicit stable ID policy and compatibility assertions for every saved model type.
- `Core/Content`: assembly scan limited to this mod, with registration into native card/relic/potion pools.
- `Core/Assets`: resource path conventions, existence validation, custom card frame/energy resources, and character scene paths.
- `Core/Localization`: native JSON validation and any small compatibility bridge still required during migration.
- `Commands`: mod-specific operations built on native command APIs.
- `Tracking`: combat-scoped services such as the power-delta ledger.
- `Diagnostics`: a development-only startup audit and repeatable smoke commands.

Native APIs are the default. Add a Harmony patch only when the installed game has no public hook or registry for the required behavior.

The minimum known owned patch surface is:

- Preserve the current `TOGAWASAKIKO-` `ModelDb.GetEntry` values for this mod's model types.
- Append Togawa Sakiko to `ModelDb.AllCharacters` without changing vanilla ordering.
- Supply mod-prefixed custom character visual, icon, select-screen, map, rest-site, merchant, energy, and multiplayer-hand paths where native private/convention paths cannot be used safely.
- Support the mod's card-frame material and custom energy icons.
- Intercept explicit block-loss commands for Hype; normal turn-boundary clearing can use native block-clear hooks.
- Add later content types only where current hard-coded registries require it.

Use a centralized patch catalog with target validation. One incompatible patch should identify itself and stop the mod's initialization instead of leaving a partially patched run.

## Required utility contracts

### Deck mutation

Use native ownership and command APIs. Never mutate `Deck.Cards` or combat pile collections directly.

Add-to-deck contract:

- Create a run-scoped card with `RunState.CreateCard(canonicalCard, owner)` or clone an explicitly supplied run card.
- Add it with `CardPileCmd.Add(..., PileType.Deck)` so gain history, floor metadata, add-prevention hooks, and pile hooks run.
- Return the actual `CardPileAddResult`, because a hook can prevent or replace the card.
- Default to persistent-deck-only. If an effect must also create a combat copy, expose that as a separate explicit operation and document its destination pile and `DeckVersion` link.

Synchronized removal contract:

- Accept the exact persistent deck-card instance, not just a model ID.
- During combat, snapshot every card in Hand, Draw, Discard, Exhaust, and Play whose `DeckVersion` is the same persistent instance.
- Remove those combat copies through `CardPileCmd.RemoveFromCombat`; its current implementation also handles cards in the visual play queue.
- Remove the persistent card through `CardPileCmd.RemoveFromDeck` so history and hooks execute once.
- Return a result describing the persistent card, all removed combat copies, and whether removal was prevented or failed.
- Publish a narrow post-removal event/result for card-specific effects such as Endurance or Masquerade. Do not hard-code all STS1 side effects into the general deck command.
- Execute as a synchronized game action when invoked during combat so multiplayer and replay ordering are deterministic.

Acceptance checklist:

- [x] Remove one of two identical card models and remove only its linked combat copy.
- [x] Remove linked copies correctly from each of the five combat piles.
- [x] Cancel/remove a linked card already in the play queue without leaving a node or action behind.
- [x] Remove outside combat.
- [x] Respect cards that cannot be removed.
- [x] Fire history and hooks exactly once and leave no card registered in the wrong state.
- [x] Save and reload after persistent add/remove.
- [ ] Verify host/client and replay state when multiplayer support is enabled. Multiplayer remains disabled; single-player replay recording serialized the synchronized action successfully.

### Current/previous-round power ledger

The requested "buff gains" and the STS1 implementation are not identical: STS1 has a player power-loss recorder, while enemy/final-boss logic separately records gains. Implement one delta ledger that can answer both questions.

- Register one combat-scoped hook model through native `ModHelper.SubscribeForCombatStateHooks`.
- Record the post-modifier amount supplied to `AfterPowerAmountChanged`, not the requested amount.
- Store immutable event data: combat round, target identity and side, model ID/type, power type at the time of the event, actual signed delta, applier, and card source.
- Positive deltas are gains; negative deltas are reductions. Provide filtered queries for buffs, debuffs, gains, and losses.
- Rotate/currently select buckets by `ICombatState.RoundNumber`, not by an individual player's turn. Extra turns and multiplayer participants make player-turn rotation incorrect.
- Subscribe to `Creature.PowerRemoved` if complete removals must count as a loss, because direct `PowerCmd.Remove` does not emit `AfterPowerAmountChanged` with the removed amount.
- Clear the ledger at combat end and never retain mutable `PowerModel` instances as the authoritative history.

Acceptance checklist:

- [x] Record new, stacked, reduced, duration-ticked, and fully removed powers.
- [x] Query current and previous round separately for every player and enemy.
- [x] Filter buffs versus debuffs without changing the underlying event record.
- [x] Handle multiple gains to the same power in one round.
- [x] Handle extra turns and multiple players without rotating early.
- [x] Reset between combats and after load/replay transitions.

### Copy a power/buff

The current game already provides the core operation. An existing combat power is mutable; `ClonePreservingMutability()` makes a mutable copy, clears its owner/event subscribers, clones dynamic variables, and resets private internal data. The base-game `Misery` card uses this pattern before `PowerCmd.Apply`.

Owned wrapper contract:

- Accept a live power snapshot and a target.
- Clone it with `ClonePreservingMutability`; if a canonical model is ever accepted, convert it with `ToMutable` instead.
- Preserve the intended amount and applier policy explicitly.
- Apply through `PowerCmd.Apply`, allowing normal stacking, per-applier instances, modifiers, history, hooks, and UI updates.
- Never insert a cloned power directly into a creature's power list.
- Define explicit handling for temporary powers that have a paired internal power. Do not claim every game power is safely copyable until covered by a test.

Acceptance checklist:

- [x] Copy a normal stackable buff.
- [x] Copy an instanced and an instanced-per-applier power.
- [x] Copy dynamic-variable state without retaining the old owner or event handlers.
- [x] Verify temporary/duration powers and paired internal powers.
- [x] Reject or explicitly handle powers whose custom internal state cannot be reconstructed.

### Hype block-loss prevention

Preserve the original STS1 semantics: when a creature has block and Hype, each positive block-loss event consumes one Hype and prevents that entire event. Damage consuming block is not automatically included unless the STS1 behavior proves it used the same loss path.

STS2 currently has separate paths:

- Normal turn-boundary clearing calls native `ShouldClearBlock` and `AfterPreventingBlockClear` hooks.
- Explicit effects call `CreatureCmd.LoseBlock`, which directly invokes `Creature.LoseBlockInternal` and has no amount-modification hook.
- Combat teardown also clears block directly and must not consume Hype.

Implementation contract:

- Use `HypePower.ShouldClearBlock` to prevent normal clearing and consume one stack in `AfterPreventingBlockClear`.
- Add a narrow compatibility patch around `CreatureCmd.LoseBlock` for positive explicit losses while the target has block and Hype.
- Complete the returned asynchronous operation only after one Hype has been reduced, so game-action ordering remains deterministic.
- Guard against double consumption when another preventer such as Blur, Barricade, or Sturdy Clamp participates.
- Do not patch `LoseBlockInternal` globally; that would also affect teardown and bypass command context.

Acceptance checklist:

- [x] Prevent turn-boundary clear and consume exactly one Hype.
- [x] Prevent a partial and a full explicit block-loss command and consume exactly one Hype.
- [x] Do nothing for zero/negative loss, zero block, no Hype, normal damage absorption, or combat teardown.
- [x] Interoperate deterministically with vanilla block-retention effects.
- [x] Work for player and enemy owners, matching the STS1 patch's creature-wide scope.

### Post-combat card gain and save

Do not patch `SaveManager` for Monochrome Hairband on the current game.

In `v0.111.0`, combat victory runs `AfterCombatVictory`, then marks the combat room pre-finished, then immediately awaits `SaveManager.SaveRun`. A card inserted into the persistent deck during `AfterCombatVictory` is therefore included in the game's normal post-combat save.

Implementation contract:

- Implement Monochrome Hairband's gain in `AfterCombatVictory`.
- Create the exact card in the run state and add it with the deck-add command.
- Await the add before returning from the hook.
- Preserve the STS1 exclusion for The Oblivion/final custom encounter through an explicit room/encounter predicate.
- Keep presentation/preview separate from persistence and make the hook idempotent for one victory.
- Add an explicit save only if a later game version moves the normal save before the hook; the compatibility audit must prove that ordering first.

Acceptance checklist:

- [x] Win an eligible combat and receive exactly one card.
- [x] Confirm the post-combat save already contains that card, then quit and reload.
- [x] Skip the excluded final/custom encounter.
- [x] Do not duplicate on reward-screen navigation, reload, or multiplayer callbacks.
- [ ] Verify host authority if multiplayer support is enabled. Multiplayer remains disabled, so this conditional gate was not triggered.

## Execution checklist

### Phase 0: Freeze the evidence baseline

- [x] Inventory the current STS2 repository and the STS1 source repository.
- [x] Verify the installed game version, assembly, PCK, and exact MegaDot version.
- [x] Decompile the installed `sts2.dll` and trace loader, model, deck, power, block, and post-combat save flows.
- [x] Review the current template/wiki, WatcherMod, and BaseLib snapshots.
- [x] Run a build against the current game with deployment redirected outside the game folder.
- [x] Record the first five compile failures and known project-file drift.
- [ ] Recover the installed game PCK with GDRE Tools into a versioned directory outside Git.
- [ ] Record the relevant base-game character scenes, card UI scenes/materials, character-select node hierarchy, rest/merchant scenes, and resource dimensions.
- [ ] Create a generated STS1-to-STS2 content parity inventory covering IDs, source class, STS2 class, EN/zhs localization, small/large art, behavior status, and pool-enabled status.
- [ ] Classify every STS1 patch/action as native API, native hook, owned command, narrow Harmony patch, presentation-only, or deferred act content.

Completion evidence: the parity inventory has no unexplained source item and all decompile/extraction inputs identify the exact game build.

### Phase 1: Restore a current loadable migration baseline

This phase may temporarily keep BaseLib. It exists to isolate game-update changes before replacing the framework.

- [ ] Add an ignored local MSBuild properties file and a committed example; store the local game, data, mods-output, and verified MegaDot executable paths there.
- [ ] Replace the stale path-discovery logic with the current conditional fallback behavior.
- [ ] Make Godot mandatory for publish, not for code-only build.
- [ ] Pin BaseLib, analyzer, publicizer, and optional PCK packer versions for the migration checkpoint.
- [ ] Consolidate to one initializer.
- [ ] Use `Assembly.GetExecutingAssembly()`, register Godot C# scripts, and call `Harmony.PatchAll(assembly)` once.
- [ ] Update the temporary manifest to current dependency-object syntax and set `min_game_version` to the actually verified baseline.
- [ ] Update `project.godot`, its icon path, and export settings from a reviewed template diff.
- [ ] Repair the five current hook-signature compilation failures against `v0.111.0` semantics.
- [ ] Rebuild until compiler and analyzer errors are zero; do not treat restored compilation as runtime success.
- [ ] Import and publish the PCK into an isolated output directory.
- [ ] Inspect the output triplet and PCK resource paths.
- [ ] Load the temporary BaseLib-backed checkpoint in the actual game and verify one initializer invocation, mod-list presence, localization merge, model initialization, and no Togawa exceptions in `godot.log`.
- [ ] Open character select and begin a throwaway test run without modifying the user's active run.

Completion evidence: a tagged or otherwise recorded migration checkpoint compiles, publishes, and reaches a Togawa test combat on `v0.111.0`.

### Phase 2: Replace BaseLib with the owned foundation

- [ ] Freeze all existing model IDs and add startup assertions for representative character, card, power, relic, potion, and pool IDs.
- [ ] Implement the narrow ID-prefix compatibility policy before changing any base class.
- [ ] Implement assembly-local content scanning and native pool registration.
- [ ] Replace `PlaceholderCharacterModel` with an owned/native character model and the minimum custom-character registry/UI patches.
- [ ] Implement only the card-frame, energy-icon, portrait, power-icon, relic-icon, potion-icon, and resource-path support this mod uses.
- [ ] Replace BaseLib card/power/relic/potion base classes with mod-owned equivalents while retaining call-site-compatible helpers where that reduces migration risk.
- [ ] Replace BaseLib `CommonActions` calls with native commands or small owned wrappers.
- [ ] Move authoritative localization to native PCK JSON for English and Simplified Chinese. Keep key generation deterministic and validate every model key.
- [ ] Replace any remaining BaseLib utility one feature at a time; maintain a dependency report until the count reaches zero.
- [ ] Add third-party notices for any adapted MIT-licensed implementation.
- [ ] Remove the BaseLib package reference and manifest dependency only after the shadow implementation passes the same smoke checks.
- [ ] Publish and launch with BaseLib absent from the test mod set.
- [ ] Verify the installed folder contains only Togawa's DLL, PCK, manifest, and optional PDB for development.

Completion evidence: the vertical slice loads, starts a combat, and saves/reloads with unchanged model IDs in a clean no-BaseLib test environment.

### Phase 3: Implement and verify shared utilities

- [ ] Implement run-deck add and synchronized deck/combat removal.
- [ ] Implement the signed current/previous-round power ledger for players and enemies.
- [ ] Implement the safe power-copy wrapper and its supported/unsupported policy.
- [ ] Implement Hype across normal clear and explicit block-loss paths.
- [ ] Implement the post-victory persistent-card-gain helper used by Monochrome Hairband.
- [ ] Add deterministic development scenarios for every acceptance item in the utility contracts above.
- [ ] Verify utility failures log enough context to identify card/power/owner/round without leaking or corrupting state.

Completion evidence: every utility acceptance checklist passes in a real game integration test; pure state-selection logic also has automated tests where practical.

### Phase 4: Presentation parity and the first vertical slice

- [ ] Build a real Togawa character-select entry: button, unlocked/locked portrait, background/transition behavior, title, and description.
- [ ] Build a mod-owned in-combat character scene with idle, attack, cast, hit, and death behavior or an explicitly approved static first-pass fallback.
- [ ] Build top-panel icon, map marker, energy counter, card trail, rest-site view, merchant view, and required multiplayer hand/icon assets.
- [ ] Complete custom card frame/back and energy-symbol presentation.
- [ ] Finish one starter card, one dependent power, and the starter relic end to end.
- [ ] Verify the starter deck can begin and finish a combat before expanding the catalog.
- [ ] Validate all 95 small/large card-art pairs against stable IDs, dimensions, alpha, crop, and runtime portrait selection.
- [ ] Add presentation records for all card types, costs, rarities, targets, upgrades, keywords, and tags without enabling incorrect behaviors in reward pools.
- [ ] Port relic, potion, and power icons into the parity inventory even when behavior remains disabled.
- [ ] Port exact approved English and Simplified Chinese names/descriptions from STS1, adapting only syntax required by STS2 variables and keywords.
- [ ] Keep custom act, event, and enemy placeholders as non-model planning records until their phase; do not register inert concrete content.

Completion evidence: a gallery/debug pass renders every card and planned icon without missing-resource or missing-localization logs, while normal runs offer only behavior-ready content.

### Phase 5: Native/simple card behaviors

- [ ] Reclassify all cards by implementation dependency: native commands only, shared utility, custom power, narrow patch, or later act content.
- [ ] Implement starter cards first, then common, uncommon, rare, token/special, and curse groups in dependency order.
- [ ] Use native damage, block, draw, discard, exhaust, power, and card-pile commands wherever they preserve STS1 semantics.
- [ ] Implement normal and upgraded behavior together.
- [ ] Verify cost, type, rarity, target, tags, keywords, dynamic values, generated-card ownership, and result pile.
- [ ] Remove approximation status only after behavior matches STS1; an approximation is not completion.
- [ ] Enable a card in the live pool only after its behavior, localization, art, and upgrade checks pass.

Completion evidence: every native/simple card is marked parity-complete in the generated inventory and survives focused combat scenarios.

### Phase 6: Custom cards and powers

- [ ] Implement powers in the dependency order required by cards rather than as an isolated bulk pass.
- [ ] Port signed power-ledger consumers, restored/lost-power behavior, Dazzling/Hype interactions, cost persistence, special retain/exhaust behavior, and card-removal side effects.
- [ ] Port custom actions as small commands over native state and hooks.
- [ ] Add narrow compatibility patches only after proving no current native hook covers the behavior.
- [ ] Audit every hook for owner, side, participant list, source, multiplayer, extra-turn, and combat-ending behavior.
- [ ] Verify all token and curse lifecycle hooks, including draw, end-in-hand, exhaust, removal, and cloning.
- [ ] Replace every skeletal or approximate card/power with exact behavior or leave it disabled with an explicit reason.

Completion evidence: all 95 cards and their required powers are either parity-complete and enabled or explicitly deferred with no chance of appearing in normal play.

### Phase 7: Relics and potions

- [ ] Port the starter relic first and validate new-run/save/reload behavior.
- [ ] Port all 11 concrete STS1 relics by rarity and dependency.
- [ ] Implement Monochrome Hairband through `AfterCombatVictory` and the normal following game save.
- [ ] Verify Blazing Hairband and any deck mutation relic through the shared deck command.
- [ ] Port all six concrete STS1 potions with native targeting, consumption, reward-pool, and save behavior.
- [ ] Validate relic/potion localization, icons, outlines, counters, flashes, and multiplayer ownership.
- [ ] Verify relic removal, duplication, boss swap, and reward serialization where applicable.

Completion evidence: relic and potion parity inventory is complete and a full character run does not produce hook, reward, or save errors.

### Phase 8: Custom act, events, enemies, and presentation systems

- [ ] Re-audit `v0.111.0` act, room, event, encounter, monster, intent, map, victory, and unlock registries from the decompiled DLL and recovered PCK.
- [ ] Port The Oblivion act/room flow as a vertical slice before adding all encounters.
- [ ] Port the two STS1 events and their save/reload branches.
- [ ] Port normal enemies, elites/bosses, intents, encounter pools, VFX, music, and scenes in dependency order.
- [ ] Port custom victory/cutscene/ending behavior only after normal act transition and save recovery are reliable.
- [ ] Validate room history, map generation, act transition, reward generation, encounter restart, and final-victory saves.
- [ ] Keep all unfinished encounters/events out of generation pools.

Completion evidence: the custom act can be entered, saved, reloaded, completed, and exited with every encounter/event branch represented in the parity inventory.

### Phase 9: Hardening and release

- [ ] Run a new-game-to-victory singleplayer test on the target game version.
- [ ] Save/reload at character select/new run, normal combat reward, boss reward, event, shop, rest site, act transition, custom act, and final victory.
- [ ] Test with only this mod installed and BaseLib absent.
- [ ] Test alongside a small representative mod set and distinguish Togawa errors from unrelated mod errors.
- [ ] Decide and document multiplayer support. If supported, test host/client, multiple Togawa players, mixed characters, extra turns, reconnect/replay, generated cards, deck removal, and power tracking.
- [ ] Assert stable model IDs and provide migrations before any unavoidable rename.
- [ ] Audit startup and a full run log for errors, missing localization, missing assets, failed patches, and unknown model IDs.
- [ ] Verify no live pool contains a stub or approximation.
- [ ] Verify package versions, `min_game_version`, manifest dependency list, PCK engine version, and output filenames.
- [ ] Build and publish from a clean checkout using only documented local configuration.
- [ ] Produce a release archive containing the one self-contained mod folder and installation instructions.

Completion evidence: clean build/publish, clean no-BaseLib load, stable save/reload, full-run proof, and a reviewed release archive.

## Verification gates used throughout

| Gate | Required evidence |
| --- | --- |
| Compile | Restore and build succeed with zero compiler/analyzer errors against the recorded `sts2.dll` |
| Publish | MegaDot import/export succeeds and creates a non-stale PCK |
| Package | DLL, PCK, and manifest names/IDs agree; no runtime third-party DLL is required |
| Startup | Exactly one initializer runs; every compatibility patch reports success; no Togawa exception follows |
| Model IDs | Expected `TOGAWASAKIKO-...` IDs exist and are unique before a run starts |
| Assets/localization | Every enabled model resolves both presentation sizes and required EN/zhs keys |
| Gameplay | Focused scenario proves base and upgraded behavior, pile transitions, hooks, and ownership |
| Save | Mutated state is present after quit/reload and is not duplicated |
| Isolation | The same smoke test passes with BaseLib absent |
| Regression | Vanilla character selection and a short vanilla run still work |
| Multiplayer | Either the declared support matrix passes or the release clearly states it is unverified |

A zero-test run, stale PCK, compile-only result, or main-menu-only result is not completion for gameplay content.

## Immediate next work item

Execute Phase N5 from `NATIVE_FIRST_PORT_WORKFLOW.md`:

1. derive the card/power dependency graph from the STS1 behavior source;
2. port native-command-only models first in starter, common, uncommon, rare, token/special, and curse order;
3. implement and verify base/upgraded behavior with exact ownership, piles, targeting, and save/replay semantics;
4. enable each model only after focused actual-game evidence passes.

Do not repair or compile the preserved BaseLib-era source tree, and do not enable bulk content before its dependencies and behavior are complete.

## Primary references

- Mod setup and publish workflow: https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup
- Decompilation guidance: https://github.com/Alchyr/ModTemplate-StS2/wiki/Decompiling
- PCK/game-asset recovery: https://github.com/Alchyr/ModTemplate-StS2/wiki/Extracting-Assets-and-Text
- Current template: https://github.com/Alchyr/ModTemplate-StS2
- Watcher behavior/compatibility reference: https://github.com/lamali292/WatcherMod
- BaseLib source and license: https://github.com/Alchyr/BaseLib-StS2
