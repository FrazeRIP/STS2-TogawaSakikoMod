# Multiplayer implementation branch

Branch: `codex/multiplayer-adaptations`. Development package: `0.2.0-multiplayer.dev`.

This work is authorized by the September 8 multiplayer request. Multiplayer adaptations and both requested test cards are implemented on this development branch, with measured coverage below. It supersedes the earlier deferral for this branch; the v0.1.0 single-player acceptance record remains historical. Generated artwork remains placeholder artwork.

## Behavior and compatibility

- Native player actions, hooks, event options, and reward choices execute their deterministic state changes on every peer. Only the owning local player submits a new player action or opens the choosing UI. Replay cannot submit another player action.
- Persistent combat removal packets include the native linked combat-card identity, preserving the exact permanent card when an earlier removal shifts deck indices. A remote owner or a multiplayer request without a registered linked combat copy is rejected. Existing card/hook mutations remain inside their enclosing native synchronized action.
- The power-change ledger records at the entry to the native amount-change hook, before nested power effects can overwrite the captured amount. The native gameplay hook order is unchanged. Power copies with an explicitly absent applier are allowed.
- Colorful Notebook assigns its owner to a native hook choice context. Warmth-Infused Porcelain Cup checks reward ownership before consuming reward RNG. Existing owner-scoped relic and potion behavior is retained.
- Multiplayer keeps the canonical native Neow room. Each Sakiko independently chooses Another Mask, The Third Movement, or Blazing Hairband through the native event synchronizer. Vanilla players keep their native options. Another Mask retains its saved one-time deck conversion guard.
- Card and hurt voices play for the local Sakiko owner. Remote Sakiko card voices are silent. Run cleanup, run initialization, and a changed local Net ID reset voice cooldown state. Shared visible combat effects retain their existing local rendering.
- The native handshake still checks game version, gameplay mod versions, and model IDs. A narrow addition to its gameplay-mod list carries a digest of the loaded package's DLL, manifest, and PCK. Peers must use identical packages, including native load lobbies. Identity is frozen during mod initialization so updating files while a process remains open cannot make its already-loaded code advertise a newer package. PDBs are excluded from identity.
- Game target remains STS2 `v0.111.0` / `41cef1ea`, with MegaDot `4.5.1.m.14` and .NET 9. Source inspection uses `Recovered/v0.111.0-41cef1ea`; the older `STS2Decompile` tree is not the API authority.

## Test cards

| Card | Stable ID | Base | Upgrade |
| --- | --- | --- | --- |
| Test: Party Purge Reward / 测试：全队移除奖励 | `TOGAWASAKIKO-MULTIPLAYER_PURGE_TEST_CARD` | Every combat participant receives one native card-removal reward after combat | Two rewards per participant |
| Test: Party Hype / 测试：全队狂热 | `TOGAWASAKIKO-MULTIPLAYER_HYPE_TEST_CARD` | Every living player, including the caster, gains one Hype | Two Hype |

Both are zero-cost Skills with Exhaust and Token rarity. They are explicitly registered for the native console/library, serialization, and network identity, and excluded from the normal unlocked card pool. The original 86 enabled cards and nine STS1-disabled exclusions are unchanged. There are now 90 registered card models, including the two compatibility cards and two test cards.

The purge card adds the game's `CardRemovalReward` to each player's room rewards. Native reward selection removes a card from that player's permanent deck; another player never chooses the card. Rewards use the native room save representation and native dead-player/final-victory reward rules. Repeated plays intentionally add more rewards.

The test cards reuse Memento Mori and Heart's Barrier portraits. Existing four multiplayer hand images already provide the required 422×1200 transparent resources. New generated art was unnecessary.

With the native console open during combat, spawn either into the issuing player's hand using the game's networked `card` command:

```text
card TOGAWASAKIKO-MULTIPLAYER_PURGE_TEST_CARD
card TOGAWASAKIKO-MULTIPLAYER_HYPE_TEST_CARD
```

Both also override native combat/modifier generation eligibility. This is necessary because native `CardFactory.FilterForCombat` accepts Token rarity and some generation effects inspect the full catalog instead of the unlocked pool.

## Verification tools

`tools/Invoke-NativeMultiplayerContractTest.ps1` runs separate game processes with isolated profile roots over native ENet loopback. It uses real game actions, player-choice synchronization, reward synchronization, and checksum tracking. File barriers coordinate diagnostic checkpoints; they do not carry gameplay actions or choices. The harness verifies vanilla control, Sakiko host/vanilla client, vanilla host/Sakiko client, duplicate Sakiko, and a mixed four-player party.

The opt-in diagnostics check new-card base/upgrade localization and pool exclusion, native negative handshake packets, shared persistent deck/power contracts, and starting-event owner/save behavior. Native packet-only negative tests are distinguished from the actual multiplayer transport tests. A captured replay is not a successful playback test, and a reconstructed save is not a fresh client reconnect.

Existing regression commands remain `Invoke-NativeN3ContractTest.ps1`, `Invoke-NativeKingsContractTest.ps1`, `Invoke-NativeN5BatchTest.ps1`, `Invoke-NativeN6ContractTest.ps1`, `Invoke-NativeN4CatalogTest.ps1`, `Invoke-NativeStartingRoomTest.ps1`, `Invoke-NativeGameplaySmokeTest.ps1`, and `Invoke-NativeN7FullRunTest.ps1`. All use isolated app-data roots. A normal build still does not deploy.

Build/package validation checks the four-file self-contained package with no BaseLib dependency. The additional compile-time `Steamworks.NET` reference is supplied by the game, is not copied into the package, and supports the game's native connection-error API used by the diagnostic transport harness.

The manifest uses the native parser's valid semantic version `0.2.0-multiplayer.dev`. The human-readable bootstrap version retains its `v` prefix. The pinned native parser accepts that prefix but rejects a second hyphen inside the prerelease component. The negative handshake probe reads the actual loaded manifest version, independently of the bootstrap display text.

All 88 existing card models and 27 power implementations were inspected. Shared multiplayer probes exercise exact permanent-card identity and duplicate removal, owner-scoped interleaved combat history, Hype, nested ledger changes, Dazzling RNG, independent instanced/per-applier powers, and every one of the six potions for every party slot. Potion probes use native consumption and effect wrappers; they verify immediate effects and teammate isolation, without claiming delayed-turn expiration coverage.

## Evidence and limits

The initial native vanilla control reached combat, owner-submitted host/client plays, and victory with matching state snapshots and checksum values. The first N5 regression attempt exposed a nondeterministic test fixture: Perfection selected an unplayable negative-cost card while the old test expected a zero-cost playable card. The diagnostic now deliberately exercises a positive-cost selection and separately preserves the native negative-cost boundary; gameplay was not changed to satisfy that assertion.

The first five-scenario ENet matrix passed in `artifacts/multiplayer/native-multiplayer-contract-20260908-005947/summary.json`: vanilla generated seven matching checksums; each two-player mixed arrangement generated eight with two synchronized purge choices; duplicate Sakiko generated twelve with four choices; the four-player party generated twelve with eight choices. This is intermediate package evidence; subsequent diagnostics and startup identity hardening require final package validation.

### Single-player regression evidence

Completed results are under `artifacts/multiplayer/verified-regression`:

| Check | Result |
| --- | --- |
| N3 shared command/hooks | 31 assertions, nine actual-game markers, exact permanent-card cleanup, strict save reload; zero managed issues |
| N5 cards and powers | 725 assertions; common/uncommon/rare, mutation and generated-card flows, owner history, two stacked extra turns, and feedback checks passed; zero managed issues |
| N6 relics and potions | 73 assertions, 15 actual-game markers, 11 original relics and all six potions, native save/reload passed; zero managed issues |
| Kings | Eight actual-game markers, independent pending-state carrier, natural two-option rewards and clearing, pending save/reload passed; zero managed issues |
| N4 EN and zh-Hans | 365 localized entries / 730 formatted variants per language; 345 textures, 52 audio resources, 11 standalone resources, 17 localization files; no unresolved formatting or startup managed issues |
| Starting room EN and zh-Hans | All three fixed choices passed in both languages, with exact decks/relic effects and zero managed issues |
| Gameplay smoke | Sakiko combat/rewards and save reload passed; native Ironclad control passed with the mod loaded |
| N7 full native AutoSlay traversal | Victory; six captured and reloaded checkpoints (new run, combat reward, shop, rest, boss reward, act transition); completed-run cleanup and final profile reload; zero excluded-card generation and zero managed issues |

The full N7 result is `native-n7-full-run-20260908-012452/summary.json`. `contract-batch.json` and `presentation-batch.json` preserve the other complete results. These runs used DLL SHA256 `3A2EB0306BA7DEB2997CD41D1DA3076456D9297DB96D9832EB567C1D9B296065` and the final PCK. Subsequent compiled changes only corrected version metadata and the opt-in negative-handshake test's manifest lookup; production gameplay did not change. N3 and both N4 locales were rerun on the final package, recorded as `final-package-n3.json` and `final-package-catalog.json`; the native semantic-version warning is absent.

Earlier interrupted acceptance attempts are explicitly marked and do not count as final passes. The retained N4 failure was its old 361-entry assertion, corrected to 365 for the four new localization keys. The retained handshake-fixture failure resulted from comparing the bootstrap display version to the manifest version; it now uses and validates the actual loaded manifest.

### Final multiplayer package evidence

The final native ENet matrix passed all six scenario executions, 14 live peer runs, and 14 native replay perspectives with 114 matching replay checksums. Every run used separate process/profile roots and native synchronization with `TestMode=false`. English and zh-Hans alternated by player slot. Every scenario passed its native starting-event phase, per-player choices and inventory, host-only native save/reload, client save absence, state/RNG comparison, and recorded-combat playback. All owned test processes were closed after completion.

| Scenario | Players | Matching live checksum checkpoints | Synchronized choices | Starting choice offset | Replay perspectives |
| --- | --- | --- | --- | --- | --- |
| Vanilla control | 2 | 8 | 1 | 0 | 2 passed |
| Sakiko host, vanilla client | 2 | 9 | 2 | 0 | 2 passed |
| Vanilla host, Sakiko client | 2 | 9 | 3 | 0 | 2 passed |
| Duplicate Sakiko | 2 | 13 | 4 | 0 | 2 passed |
| Mixed maximum party | 4 | 13 | 8 | 0 | 4 passed |
| Duplicate Sakiko, alternate fixed choices | 2 | 13 | 4 | 1 | 2 passed |

Choice totals include native starting-card selections where offered, as well as purge rewards. Offsets 0 and 1 cover Another Mask, The Third Movement, and Blazing Hairband. The four-player phase verified all 24 native potion uses, six per owner. The replay covers the recorded combat through the shared custom action; the later victory and native purge reward selections are verified by the live peer runs rather than the replay.

Aggregate: `artifacts/multiplayer/final/summary.json`. Its source groups are `matrix-a-20260908-013456`, `matrix-b-20260908-013456`, `matrix-c-20260908-013456`, and `matrix-d-offset1-20260908-013457`. Every group and replay is bound to the same package identity `TogawaSakiko-package-sha256-7803973CB7A78E16400EB5510FD4A10C78A65769CF11EFFA43E0198411ADB807`.

| Final file | SHA256 |
| --- | --- |
| TogawaSakiko.dll | `B6E4C0C188F60F03CFE95CC543DFCC494B238DA9BFAC4DF289883297B64A1C5D` |
| TogawaSakiko.json | `3923C46EEFEA5138D3209ED8CE67EF5E6251131C90264E4AE146982E7B48CE4D` |
| TogawaSakiko.pck | `67E410D158727ED044B8FF34AA5E4DB39053ADB92D3E558BFFE6FD6BD5981F84` |

The four-file package is staged at `TogawaSakiko/artifacts/stage/TogawaSakiko` and installed in the local game's `mods/TogawaSakiko` directory; hashes match. Release build completed with zero warnings/errors, package validation passed, and all 15 PowerShell scripts plus 17 localization catalogs and the manifest parse successfully. The legacy `TogawaSakikoCode` tree is untouched. Changes remain uncommitted on the requested branch; no push was requested.

The pinned game's `NJoinFriendScreen` rejects `RunSessionState.Running` with `NetError.RunInProgress` and disconnects. Live rejoin and host migration are not supplied by this branch. Supported native save/load paths are tested separately; serialized reconstruction must not be described as a live client reconnect. Native replay files do not embed the gameplay-mod list, so the diagnostic recording/playback workflow binds a separate package-identity sidecar to each replay.

This is a multiplayer development branch. Native ENet loopback, packet contracts, recorded-combat playback, and native save reconstruction do not certify Steam/WAN transport, latency or disconnect recovery, a fresh network load-lobby session, a complete natural multiplayer campaign, every interactive choice cancellation, cross-mod behavior, or manually inspected remote-player rendering. The broad historical release checklist remains a separate release gate. No final-art completion is claimed.

Detailed audits: [cards and powers](PHASE_M2_COMBAT_MULTIPLAYER_AUDIT.md), [relics and presentation](MULTIPLAYER_RELIC_PRESENTATION_AUDIT.md), [original acceptance checklist](MULTIPLAYER_PORT_REQUIREMENTS_AND_TODO.md).
