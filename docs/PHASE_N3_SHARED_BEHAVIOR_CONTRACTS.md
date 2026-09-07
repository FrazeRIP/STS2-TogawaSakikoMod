# Phase N3 Shared Behavior Contracts

Status: complete

Executed: 2026-09-06

Starting point: branch `Test1`, commit `3a35047`

Game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This is the execution record for Phase N3 of `NATIVE_FIRST_PORT_WORKFLOW.md`. It preserves the native-only Phase N1 package and the Phase N2 playable character slice while adding the shared behavior contracts required before broader content can be ported.

## Delivered contracts

### Persistent deck mutation

`PersistentDeckMutation` owns the shared add and remove operations.

- Canonical additions use `RunState.CreateCard` and `CardPileCmd.Add` with `PileType.Deck` and return the native `CardPileAddResult` unchanged.
- Explicit run-card copies use `RunState.CloneCard` and reject a source from another player or run.
- Removal accepts the exact mutable persistent card instance. It selects linked combat cards only when `ReferenceEquals(candidate.DeckVersion, persistentCard)` is true.
- Selection snapshots Hand, Draw, Discard, Exhaust, and Play before any removal begins.
- Linked cards are removed through `CardPileCmd.RemoveFromCombat`; the persistent card is removed through `CardPileCmd.RemoveFromDeck`.
- The result distinguishes success, non-removable prevention, missing deck membership, prior removal, and command failure. It includes the persistent card, every successfully removed linked card, and failure context.
- A narrow `PersistentCardRemoved` result event runs only after successful persistent removal.
- `PersistentDeckRemovalGameAction` carries the exact persistent-deck index as `NetDeckCard`, executes through `ActionQueueSynchronizer`, and serializes both the identity and visual-skip policy for multiplayer/replay ordering.
- No shared command directly adds to or removes from a card collection.

The actual visual-queue diagnostic uses `skipCombatVisuals: true` for the synchronized removal. On game build `v0.111.0`, the non-skipped native path starts both a cancellation tween and an exhaust VFX for the same queued `NCard`, which can return that node to its pool twice. Skipping that removal VFX leaves the queued native `PlayCardAction` to perform its normal single cancellation/fade after the card state is removed. The final diagnostic proves the synchronized removal action finishes first, the queued play then finishes without playing, and neither a node nor action remains.

### Signed power-change ledger

`PowerChangeLedgerService` registers one combat-scoped hook through `ModHelper.SubscribeForCombatStateHooks`. A weak-table entry gives each concrete `CombatState` its own mutable hook and ledger.

- `BeforePowerAmountChanged` snapshots prior amount and whether the instance already existed.
- `AfterPowerAmountChanged` records `power.Amount - priorAmount`, so modifiers are reflected in the actual signed delta instead of the requested amount.
- Immutable records contain round, target combat/model/player identity, side, model ID, runtime type, declared and amount-effective power type, signed delta, prior/current amount, change kind, applier identity, and card-source identity.
- Positive values are gains; negative values are reductions or removals. Queries filter gain, loss, removal, buff, debuff, target, current round, or previous round without rewriting source events.
- Direct `PowerCmd.Remove` is captured through each creature's `PowerRemoved` event. Amount-driven removals are de-duplicated against that event.
- Existing and newly added players and enemies are subscribed for the life of the combat.
- Round queries use `CombatState.RoundNumber`. They do not rotate on a player's turn. A round-number rewind clears stale state for load/replay reconstruction.
- `AfterCombatEnd` clears the ledger and detaches all combat and creature subscriptions.

No mutable `PowerModel`, `Creature`, or `CardModel` is retained as authoritative event history.

### Narrow power copying

`PowerCopyCommand` accepts a source snapshot, target, explicit amount override when needed, card source, and one of four applier policies: preserve source, use target, use an explicit creature, or use no applier.

- The source is cloned through `ClonePreservingMutability`; a canonical result is converted with `ToMutable`.
- Owner/event subscriptions are supplied by the native clone lifecycle. Target, applier, turn-start amount, and duration-skip fields are cleared before applying.
- Intended amount and applier are passed explicitly to `PowerCmd.Apply`, preserving native stacking, per-applier identity, modifiers, hooks, history, duration setup, and UI behavior.
- The result distinguishes a new instance, stacking into an existing instance, native prevention, and unsupported input. It includes before/after amounts and context.
- Paired temporary powers implementing `ITemporaryPower`, live powers with a separate target reference, and powers with unapproved custom `InitInternalData` state are rejected.
- `StranglePower` and `OblivionPower` are explicitly approved as reset-safe custom-state types. Other custom-state powers remain rejected until covered by a specific policy and test.

### Hype

`HypePower` preserves the STS1 behavior text: whenever its owner is about to lose Block, one Hype is consumed instead.

- Normal turn-boundary clearing is prevented through `ShouldClearBlock` and consumes one stack only when the native `AfterPreventingBlockClear` callback identifies Hype as the selected preventer.
- The only behavior patch targets the public four-argument `CreatureCmd.LoseBlock` overload. It intercepts an explicit positive loss only during an active, non-ending combat when the living target has positive block and positive Hype.
- The patch returns the awaited `PowerCmd.Decrement` task, preserving action ordering.
- It does not patch `LoseBlockInternal`, damage absorption, teardown, or unrelated block paths.
- Native first-preventer ordering prevents Hype from double-consuming when Blur, Barricade, or another retention effect wins the hook decision.
- The power has stable ID `POWER.TOGAWASAKIKO-HYPE_POWER`, English and Simplified Chinese localization, and small/large native PCK assets.

### Monochrome Hairband and native save

Monochrome Hairband remains on `AfterCombatVictory` and now calls `PersistentDeckMutation.AddCanonicalAsync<DesireCard>`.

- A combat-state identity guard makes one victory callback idempotent.
- The living-owner check remains.
- Both the act entry and encounter entry explicitly exclude `TOGAWASAKIKO-THE_OBLIVION`.
- Persistence is awaited before the preview is requested.
- No save-write patch was added.

The recovered `CombatManager` source for `v0.111.0-41cef1ea` still awaits `Hook.AfterCombatVictory`, marks the room pre-finished, and then awaits `SaveManager.SaveRun`. The Hairband addition is therefore present in the following native save. The only SaveManager patch remains the argument-gated, read-only reload diagnostic inherited from Phase N2.

## Acceptance evidence

### Pure selection and policy checks

Startup runs 31 deterministic assertions after `ModelDb` initialization:

| Area | Covered decisions |
| --- | --- |
| Deck identity | Exact `DeckVersion` reference, identical-model exclusion, unrelated-copy exclusion |
| Ledger | Round isolation, two players and one enemy, buff/debuff filters, gains/losses/removals, repeated gains, immutable snapshots |
| Power copy | Normal stackable, instanced dynamic state, per-applier, duration, paired temporary rejection, custom-state rejection |
| Hype | Active/ending combat, dead target, zero block, zero/negative loss, and zero Hype |
| Hairband | Eligible victory and both act/encounter forms of The Oblivion exclusion |

### Actual-game N3 contract run

Artifact root: `artifacts/n3-contract/native-n3-final-contract-20260906-215319`

The fixed-seed Release run passed all nine required marker groups and then passed a fresh-process reload:

| Contract | Actual-game scenarios passed |
| --- | --- |
| Deck mutation | One of two identical models; exact linked copy only; Hand, Draw, Discard, Exhaust, and Play; Eternal prevention; hook/history/event exactly once; no stale state; synchronized visual-queue cancellation; outside-combat removal; persistent add/remove save probe |
| Ledger | Requested 1 modified to actual 3; new, stack, reduction, duration tick, amount-driven full removal, direct removal; every live player/enemy; buff/debuff/gain/loss filters; repeated same-power gains; current/previous rounds; extra-turn-safe round selection; round rewind reset; post-combat reset |
| Power copy | Strength new/stacked copy; Bomb instanced clone with dynamic value 77 and no old owner/removed handler; Strangle per-applier copy; Weak duration and native first-tick skip; rejection of Flex paired temporary state and Automation custom state |
| Hype | Partial/full explicit loss; no Hype; zero/negative loss; zero block; damage absorption; player and enemy owners; Blur first-preventer interoperability; later Hype turn clear; active teardown probe with Barricade |
| Hairband/save | Exactly one Desire on eligible victory; repeated callback adds no duplicate; post-combat native save contains it; fresh process reloads it; outside add remains; removed Two Moons remains absent |

The saved/reloaded deck contained exactly one `DesireCard`, one explicitly added `SilentFarewellCard`, and zero removed `TwoMoonsCard` instances. Both gameplay and reload managed-issue counts were zero. A stricter final scan found no `[ERROR]`, managed exception, localization-formatting error, or mod-load failure in either log.

The teardown probe records Hype amount and block while the power is being removed by combat shutdown. It confirmed teardown did not consume Hype through the explicit-loss patch. Replay recording also serialized the synchronized removal and diagnostic barrier actions without an action-conversion exception.

### Loader and regression runs

- English loader: `artifacts/n3-loader/n3-final-loader-eng-20260906-214642/slay-the-spire-2.log`
- Simplified Chinese loader: `artifacts/n3-loader/n3-final-loader-zhs-20260906-214705/slay-the-spire-2.log`
- Sakiko/reload/vanilla regression: `artifacts/n3-regression/native-n3-final-regression-20260906-214732`

Both loader runs reported one initializer, one bootstrap, one vertical-slice initialization, the correct language, and zero pre-startup issues.

The regression passed Sakiko character selection, combat portrait creation, reward generation, Dazzling behavior, one Hairband victory addition, reward completion, save contents, quit/fresh-process reload, custom-model deserialization, Ironclad selection, and a short vanilla combat. Togawa, reload, and vanilla issue counts were all zero.

### Conditional scenarios

Multiplayer content remains disabled and Phase N2 did not claim multiplayer support. The conditional host/client and host-authority acceptance items were therefore not triggered. The synchronized action has a native packet representation, exact deck identity, and replay-writer coverage, but an actual multiplayer host/client session remains a separate gate before multiplayer support can be declared.

The custom final act/encounter remains intentionally disabled. Its exclusion was exercised through the pure act/encounter predicate; eligible post-victory hook ordering and idempotence were exercised in the actual game.

## Build, package, and installed artifact

- Release build: passed with zero warnings and zero errors.
- `dotnet format --verify-no-changes`: passed.
- Package validation: passed with DLL, manifest, PCK, and PDB only.
- Manifest dependencies: zero.
- BaseLib references in canonical code, project, NuGet assets, manifest, and assembly references: zero.
- PCK size: 2,633,076 bytes.
- Direct card/power collection mutation scan in shared commands: zero matches.
- Final log audit across contract, English loader, Simplified Chinese loader, Sakiko/reload, and vanilla logs: zero error/exception/localization/mod-load matches.

The validated package remains installed at `Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko`.

| File | SHA-256 |
| --- | --- |
| `TogawaSakiko.dll` | `3BBBA77B0D0EA87028019506F92710BD92B9852578F4CAB681EB326EF35AD97B` |
| `TogawaSakiko.json` | `E66967066D1F3F8DCB325B7C3EDDBFCAB0DE569F6AA45721373ABD54595E071F` |
| `TogawaSakiko.pck` | `38A7D0631505F2F0E39C93C81387878A784A3E23AF3A4472FEA2A3F22158B478` |

Each installed hash exactly matched the corresponding Release staging artifact.

## Repeatable commands

Run from the repository root with `TogawaSakiko/local.props` configured:

```powershell
dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Release --no-incremental --nologo
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:PackNativeMod -p:Configuration=Release -nologo
pwsh -NoProfile -File .\tools\Test-NativePackage.ps1 -PackagePath .\TogawaSakiko\artifacts\stage\TogawaSakiko
dotnet format .\TogawaSakiko\TogawaSakiko.csproj --verify-no-changes --no-restore --verbosity minimal
```

Deployment remains explicit:

```powershell
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:DeployNativeMod -p:Configuration=Release -p:AllowLocalDeploy=true -nologo
```

Run the final isolated game checks against the deployed package:

```powershell
pwsh -NoProfile -File .\tools\Invoke-NativeN3ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n3-contract
pwsh -NoProfile -File .\tools\Invoke-NativeLoaderSmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n3-loader -Language eng
pwsh -NoProfile -File .\tools\Invoke-NativeLoaderSmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n3-loader -Language zhs
pwsh -NoProfile -File .\tools\Invoke-NativeGameplaySmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n3-regression
```

All game scripts use isolated app-data profiles and fixed seeds. Phase N3 behavior diagnostics run only with `--togawa-native-n3-contract-smoke` or its reload counterpart. Ordinary gameplay retains no automatic test behavior.

## Limitations carried forward

- Actual multiplayer host/client behavior is not yet verified or enabled.
- The power-copy command is intentionally conservative. Unsupported temporary, targeted, or custom-internal-state powers must receive explicit policies and tests before use.
- The final custom act and The Oblivion encounter remain disabled; only their exclusion predicate is active and tested.
- The synchronized queued-card diagnostic skips the affected native removal VFX on `v0.111.0` and relies on the queued play action's native cancellation visual. State, hook, history, node, action, save, and replay-writer ordering are covered.
- Phase N2 presentation and content limitations remain: static combat portrait, incomplete character-specific UI/audio, intentionally narrow live pools, and no unfinished bulk content.

## Next gate

Phase N4 completes presentation and the content inventory without enabling unfinished behavior.
