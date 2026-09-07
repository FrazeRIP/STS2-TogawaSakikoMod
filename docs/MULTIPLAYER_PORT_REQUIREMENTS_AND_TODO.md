# Multiplayer Port Requirements and Todo

Status: future work; multiplayer remains unsupported in Togawa Sakiko v0.1.0.

Baseline reviewed: Slay the Spire 2 `v0.111.0` (`41cef1ea`) and the native single-player port on branch `Test1`.

This document defines the work required before the mod may claim multiplayer support. It is not authorization to enable multiplayer behavior in the current release.

## Scope boundary

The future multiplayer port covers the same gameplay content as the accepted single-player release:

- Togawa Sakiko character and presentation;
- all 86 cards enabled in STS1;
- all 25 player-relevant powers;
- all 11 relics;
- all six potions;
- shared deck mutation, power ledger, power copy, Hype, Kings, Dazzling, save/reload, audio, and reward behavior.

The following remain outside scope:

- the nine cards disabled in STS1;
- custom acts, events, encounters, enemies, bosses, minions, intents, endings, cutscenes, and exclusive custom-world music;
- fabricated behavior or triggers for source audio that STS1 registers but never uses;
- unrelated BaseLib, compatibility-library, or custom networking dependencies.

No multiplayer change may regress the accepted single-player behavior, stable model IDs, English or Simplified Chinese localization, or no-BaseLib package boundary.

## Definition of multiplayer support

Multiplayer support can be declared only when all of the following are true:

- Every peer loads the same gameplay-affecting mod and model-ID database, or the native handshake rejects the session with a clear mismatch instead of allowing divergent state.
- State-changing work executes through the correct native synchronized action, choice, reward, and save paths. Local presentation remains local and never changes authoritative state.
- Host and clients reach identical card piles, decks, powers, relic counters, potion state, rewards, RNG state, action IDs, choice IDs, and checksums after every covered scenario.
- A callback observed by multiple peers cannot add, remove, reward, decrement, or trigger an effect more than once.
- Join, load, reconnect, replay, disconnect, and combat reconstruction do not retain stale combat-scoped state or lose player-scoped persistent state.
- Two-player and maximum-supported-party tests pass with mixed characters and duplicate Sakiko players.
- Both host and client logs contain no managed exception, failed packet conversion, missing model, state divergence, checksum mismatch, localization-formatting error, or Togawa load failure.
- The complete current single-player regression suite still passes after multiplayer changes.

## Existing foundations

These pieces are useful starting points but are not proof of multiplayer support:

- `PersistentDeckRemovalGameAction` owns a packet-serializable `NetPersistentDeckRemovalAction`, carries exact `NetDeckCard` identity, uses `ActionQueueSynchronizer`, and has replay-writer coverage.
- `PowerChangeLedgerService` creates one ledger per concrete `CombatState`, observes all players and enemies, identifies player owners by Net ID, rotates by `CombatState.RoundNumber`, and clears on rewind/combat end.
- `KingsRewardCarrierModifier` stores a sorted player-Net-ID pending set through a native saved property.
- Card selection code generally receives the native `PlayerChoiceContext`, and runtime random gameplay choices use the run's named RNG streams rather than `System.Random`.
- Relic and power hooks generally filter by owner or participating player.
- Hurt voices already use `LocalContext.IsMe` to avoid playing another player's local damage response.
- Four multiplayer hand textures exist at the native 422x1200 size, so character scenes have complete topology while art remains replaceable.
- The base game handshake includes the gameplay-affecting mod list and model-ID database hash. The port must verify that this is sufficient for exact Togawa package/version compatibility on the supported build.

## Known blockers and audit targets

### Authority and synchronized actions

- [ ] Define one helper for `NetGameType.Host`, `Client`, `Singleplayer`, and `Replay` decisions. Do not scatter ad hoc host checks across models.
- [ ] Document which native hooks execute on every peer, only the owning peer, or only the host on the target game build.
- [ ] Make every state-changing callback idempotent across peers and action replays.
- [ ] Prove all custom `INetAction` types are discovered and assigned the same serialization identity on every peer.
- [ ] Record action ID, owner Net ID, source model ID, and peer role in development diagnostics without changing gameplay order.

### Persistent deck mutation

- [ ] Audit every call to `PersistentDeckMutation` and classify it as already inside a synchronized native game action, requiring a mod-owned synchronized action, or valid outside-combat host work.
- [ ] Route active-combat persistent removals through exact `NetDeckCard` identity and `ActionQueueSynchronizer`; do not call the unsynchronized core directly from a peer-local path.
- [ ] Decide and implement the synchronized packet/result contract for persistent additions that are not already safely replicated by their enclosing native action.
- [ ] Verify add/remove preview VFX is local to the owning player while deck and pile state changes on every peer exactly once.
- [ ] Re-run exact linked-copy removal across Hand, Draw, Discard, Exhaust, Play, and the visual play queue with host and client observing the same outcome.
- [ ] Cover all current mutation consumers, including `SakikoPurgeCommand`, `MementoMoriCard`, As Your Heart Desires, Ave Mujica, Budget Bento, Clock Out, Ether, the five Phantom cards, the four Symbol cards, Monochrome Hairband, and Blazing Hairband.
- [ ] Preserve non-removable prevention and ensure Masquerade growth/reward counters advance once, only after successful removal.

### Choices and deterministic RNG

- [ ] Replace Colorful Notebook's `ThrowingPlayerChoiceContext` with a native multiplayer-safe hook context or a proven no-choice application path.
- [ ] Audit every `CardSelectCmd` call for local UI ownership, reserved choice ID, remote wait, cancellation, and disconnected-player behavior.
- [ ] Verify all random consumers use the intended synchronized run RNG stream and consume the same number of values on every peer.
- [ ] Cover Blazing Hairband, Matcha Parfait, Crychic, Dazzling target selection, Ave Mujica, Daten, Desu Wa, Perfection, Pride, Stay Elegance, Worldview, and any generated-card/potion helper found by the final audit.
- [ ] Ensure cosmetic-only randomness, such as the local hurt-voice variant, cannot alter gameplay RNG or checksums.
- [ ] Add diagnostics that compare named RNG stream snapshots at each acceptance checkpoint.

### Power ledger, copying, and Hype

- [ ] Run ledger scenarios with at least two players and multiple enemies on host and client; compare immutable event sequences, actual deltas, round numbers, appliers, and card sources.
- [ ] Verify extra turns do not rotate the ledger early and that reconnect/replay reconstruction clears stale round history exactly once.
- [ ] Verify copied normal, instanced, and per-applier powers resolve the same target/applier instances on all peers without old-owner event leakage.
- [ ] Keep temporary, paired, targeted, and custom-internal-state powers rejected unless an explicit multiplayer-safe policy and tests are added.
- [ ] Verify Hype prevents normal block clear and explicit positive `CreatureCmd.LoseBlock` once on all peers, with no duplicate decrement.
- [ ] Re-run Hype interaction with vanilla retention effects, damage absorption, dead creatures, combat teardown, and enemy owners under synchronized combat.
- [ ] Confirm the narrow public `CreatureCmd.LoseBlock` patch remains version-correct; do not patch `LoseBlockInternal` globally.

### Cards and combat history

- [ ] Audit all 86 enabled cards for owner, local-player, target, participant, pile, history, turn, and replay assumptions.
- [ ] Verify cards that inspect prior plays or pile state use owner-scoped history and deterministic ordering in mixed parties.
- [ ] Verify cards that generate, clone, retain, exhaust, purge, autoplay, change cost, or apply effects to all creatures produce identical serialized state.
- [ ] Test two Sakiko players using the same card/power in the same round so static caches or ownerless lookup cannot cross-contaminate them.
- [ ] Verify token and curse lifecycle hooks for remote owners, including draw, end-in-hand, exhaust, purge, removal, cloning, save, and reconnect.
- [ ] Confirm disabled compatibility cards never enter any player's normal pool, reward, random generation, or multiplayer save.

### Relics, potions, rewards, and saves

- [ ] Make Monochrome Hairband and Blazing Hairband victory additions execute once for each eligible living owner, regardless of which peer receives the hook first.
- [ ] Preserve the normal `AfterCombatVictory` then native-save ordering without patching `SaveManager` unless the target source proves that order changed.
- [ ] Verify Kings pending state independently for two Sakiko players, including no starter relic, no persistent Kings card, reward reduction, reward skip, reward selection, save, reload, reconnect, and clear.
- [ ] Verify Cup and Kings reward composition for each player without modifying another player's reward options.
- [ ] Verify all relic counters and transient guards are owner-scoped, saved when required, rebuilt after load, and never incremented by another player's actions.
- [ ] Verify potion ownership, local targeting UI, synchronized consumption, generated cards, delayed powers, and full-slot reward behavior.
- [ ] Confirm host-only save ownership and client reconstruction for new run, mid-combat, combat reward, shop, rest site, boss reward, act transition, and victory.
- [ ] On host disconnect, match the base game's supported behavior and prove the mod adds no exception, corrupt save, or orphaned task. Do not claim host migration unless the base game supports and the test proves it.

### Presentation and audio

- [ ] Gate card voice playback to the intended local player. Card `OnPlay` currently calls the voice helper on every executing peer.
- [ ] Define whether remote Sakiko card plays should be silent or use a separate positional/remote-volume policy; implement one documented behavior.
- [ ] Verify Dazzling VFX/audio, character-select voice, hurt voice, previews, relic flashes, and card-removal VFX do not duplicate across peers.
- [ ] Clear or safely reinitialize static per-player audio cooldown state between runs, reconnects, and local-player changes.
- [ ] Replace the point, rock, paper, and scissors hand placeholders if final multiplayer presentation quality is required. Keep each texture exactly 422x1200 with the existing path and transparent topology.
- [ ] Verify remote character combat, rest-site, merchant, map-marker, top-panel, card-back, energy, reward-hand, and death presentation.

### Compatibility and packaging

- [ ] Re-audit the recovered source and installed assemblies for the exact game build selected for the multiplayer release; do not inherit the v0.111.0 support claim automatically.
- [ ] Verify the native handshake rejects a missing Togawa mod, different model-ID hash, different gameplay-mod set, and incompatible Togawa package/version.
- [ ] If the native handshake does not compare the exact Togawa version, add the narrowest compatible handshake validation supported by the current game API.
- [ ] Keep the package self-contained with no BaseLib dependency/API and no custom-world or disabled-card content expansion.
- [ ] Preserve all stable IDs and add migrations before any unavoidable serialized-schema change.

## Implementation phases

### M0: Freeze the baseline and create the harness

- [ ] Select and record the exact supported game version, commit, engine, transport, and maximum party size.
- [ ] Snapshot the current passing single-player artifacts and package hashes.
- [ ] Build a repeatable two-process host/client harness with isolated app-data roots and captured logs for both peers.
- [ ] Add a bounded wait/cleanup policy so failed tests cannot leave game processes or lobbies running.
- [ ] Add peer-role, local Net ID, action ID, choice ID, checksum, and stage markers.

Completion gate: host and client can enter one vanilla multiplayer combat through the harness with clean logs before Togawa behavior is changed.

### M1: Authority and transport foundation

- [ ] Implement the centralized peer-role/authority policy.
- [ ] Verify custom action registration, packet serialization, owner mapping, replay conversion, and mismatch handling.
- [ ] Replace known unsafe choice contexts and classify every callback by execution authority.
- [ ] Add pure tests for authority predicates and packet round trips.

Completion gate: a minimal Sakiko starter combat reaches matching host/client checksums and replays without divergence.

### M2: Shared behavior contracts

- [ ] Multiplayer-enable persistent add/removal and all purge consumers.
- [ ] Verify the ledger, power copy, Hype, Monochrome Hairband, and Kings across peers.
- [ ] Add exact state snapshots for both peers after every command/hook boundary.

Completion gate: every Phase N3 scenario passes in host/client form, including save/reconnect/replay variants.

### M3: Card and power matrix

- [ ] Audit and test starter/basic, common, uncommon, rare, token/special, and curse groups in dependency order.
- [ ] Run duplicate-Sakiko and mixed-character ownership tests for every custom power family.
- [ ] Verify all random and interactive cards under latency and remote-choice waits.

Completion gate: all 86 enabled cards and 25 powers have a recorded multiplayer scenario or a justified shared-contract proof, with matching peer state.

### M4: Relic, potion, reward, and persistence matrix

- [ ] Test all 11 relics and six potions for owner filtering, counters, choices, rewards, save/load, and reconnect.
- [ ] Test per-player Kings/Cup/reward behavior and both Hairbands.
- [ ] Complete the room/save matrix on host and fresh/reconnected clients.

Completion gate: no player receives another player's reward, mutation, relic count, potion effect, or saved pending state.

### M5: Presentation and audio

- [ ] Apply local/remote playback policy to all voice and VFX routes.
- [ ] Verify every Sakiko UI surface for host, client, duplicate Sakiko, and mixed-character parties.
- [ ] Replace multiplayer hand placeholders if required for the release quality bar.

Completion gate: no missing asset, wrong local ownership, duplicate playback, stale cooldown, or remote UI exception remains.

### M6: Hardening and release

- [ ] Run the full acceptance matrix below on a clean self-contained package.
- [ ] Re-run every current single-player N3/N5/N6/gameplay/full-run gate.
- [ ] Audit host and every client log together and compare final state/checksum summaries.
- [ ] Validate package, archive, installation instructions, version, handshake behavior, and upgrade/reload compatibility.
- [ ] Update `STS1_TO_STS2_DIFFERENCES.md`, `NATIVE_FIRST_PORT_WORKFLOW.md`, and the release record only after all mandatory scenarios pass.

Completion gate: the support statement names the exact tested game build, party/transport matrix, limitations, and artifact paths.

## Mandatory acceptance matrix

| Scenario | Required proof |
| --- | --- |
| Host Sakiko, client vanilla | Character selection, combat, reward, save/reload, and victory with matching state |
| Host vanilla, client Sakiko | Same coverage with Sakiko owned by the remote client |
| Host Sakiko, client Sakiko | Independent decks, powers, ledgers, Kings state, relic counters, audio, and rewards |
| Maximum supported mixed party | No ownership crossover, deadlock, packet mismatch, or checksum divergence |
| Persistent add/remove | Exact deck identity and linked Hand/Draw/Discard/Exhaust/Play/visual-queue state on every peer |
| Interactive choices | Local chooser UI, remote wait, cancel, disconnect, and resumed choice |
| Random effects | Matching generated IDs, values, targets, pile order, and RNG stream snapshots |
| Hype/power copy/ledger | Matching hook order, actual deltas, owners/appliers, round rotation, and teardown |
| Kings/Hairbands/rewards | Per-player once-only behavior before and after native save, reload, skip, and selection |
| Potions/relics | Owner-only counters/effects and synchronized consumption/reward state |
| Reconnect during combat | Rebuilt piles, powers, turn/action/choice state, ledger reset policy, and clean continuation |
| Reconnect at reward/room transition | Correct player reward, Kings pending state, counters, and next save |
| Replay | All custom actions convert and finish with matching final state and no unknown packet/model |
| Host disconnect | Base-game-consistent termination/recovery with no Togawa corruption or orphaned work |
| Mod mismatch | Clean rejection for absent/incompatible package or model database |
| Single-player regression | Existing clean build, loaders, N3/N5/N6, Sakiko, vanilla, save/reload, and full-run gates still pass |

Run each gameplay scenario with English and Simplified Chinese loading represented across the matrix. At least one complete run must reach final victory in multiplayer.

## Required diagnostics and artifacts

- [ ] Add pure tests for authority rules, Net ID ownership, packet round trips, deterministic selection, and save-schema parsing.
- [ ] Add an installed-game multiplayer contract harness, proposed as `tools/Invoke-NativeMultiplayerContractTest.ps1`.
- [ ] Capture separate host/client app-data roots, logs, replay files, saves, and structured state summaries.
- [ ] Record package hashes, game/commit/engine versions, transport, party composition, player Net IDs, seeds, and mod lists.
- [ ] Compare checkpoints for deck/piles, card identity, powers, relics, potions, rewards, RNG streams, action/choice counters, and checksums.
- [ ] Treat state-divergence, checksum, timeout, disconnected wait, unknown packet/action/model, managed exception, and localization-format errors as hard failures.
- [ ] Preserve negative artifacts for any discovered engine limitation and document the narrow workaround and affected build.
- [ ] Create `docs/PHASE_M1_MULTIPLAYER_FOUNDATION.md` when implementation begins, then one execution record per completed multiplayer phase.

## Release checklist

- [ ] Zero-warning, zero-error clean build.
- [ ] `dotnet format --verify-no-changes` passes.
- [ ] Package validator reports zero BaseLib references/dependencies and zero excluded custom-world tokens.
- [ ] English and Simplified Chinese host/client loaders pass.
- [ ] Every mandatory matrix row passes with structured evidence.
- [ ] Host/client/replay logs have zero managed, mod-load, localization, packet, checksum, or state-divergence issues.
- [ ] No disabled card appears in any player deck, pool, reward, generated card, save, or replay.
- [ ] Short and full single-player regressions remain green.
- [ ] Clean multiplayer package is installed and its hashes match the validated staged package.
- [ ] Support statement and known limitations are updated without claiming untested transports, player counts, reconnect modes, or game versions.

Until every release item is checked, multiplayer must remain documented as unsupported.
