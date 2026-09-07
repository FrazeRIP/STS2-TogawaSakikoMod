# STS1 to native STS2 differences

This report records the deliberate scope boundary and every known source gap for the native Togawa Sakiko v0.1.0 port. The machine-readable and per-model audit remains `FULL_PORT_PARITY_INVENTORY.json`; its readable companion is `FULL_PORT_PARITY_INVENTORY.md`.

## Ported gameplay scope

| Domain | In-scope STS1 total | Native STS2 total | Remaining active gap |
| --- | ---: | ---: | ---: |
| Enabled cards | 86 | 86 | 0 |
| Player-relevant powers | 25 | 25 | 0 |
| Relics | 11 | 11 | 0 |
| Potions | 6 | 6 | 0 |
| Active audio routes | 38 | 38 | 0 |

The character, starting deck/relic, rewards, card/power behavior, relics, potions, native save/reload behavior, shared mechanics, English and Simplified Chinese localization, original in-scope art, sound effects, and active voice routes are present.

## Disabled cards skipped

These nine cards are disabled in STS1 and are excluded from completion and all normal generation:

- `AreTheseLyrics` — `TOGAWASAKIKO-ARE_THESE_LYRICS_CARD`
- `AuthorityRestoration` — `TOGAWASAKIKO-AUTHORITY_RESTORATION_CARD`
- `Carefree` — `TOGAWASAKIKO-CAREFREE_CARD`
- `IWantToBeYourGod` — `TOGAWASAKIKO-I_WANT_TO_BE_YOUR_GOD_CARD`
- `NeverGiveYouUp` — `TOGAWASAKIKO-NEVER_GIVE_YOU_UP_CARD`
- `NumbersAndFaces` — `TOGAWASAKIKO-NUMBERS_AND_FACES_CARD`
- `Passion` — `TOGAWASAKIKO-PASSION_CARD`
- `RaiseTheBet` — `TOGAWASAKIKO-RAISE_THE_BET_CARD`
- `Weakness` — `TOGAWASAKIKO-WEAKNESS_CARD`

`Carefree` and `Weakness` retain hidden native compatibility models so existing references to those previously established stable IDs can deserialize. The card-pool filter rejects both, and the full-run harness scans all captured saves for every disabled ID. The other seven disabled cards have no canonical native model.

## Custom-world content removed

The STS1 custom act and all content owned only by that world are outside this port and absent from the canonical assembly and PCK:

- custom acts, events, encounters, enemies, bosses, minions, and intents;
- enemy-only compatibility powers and helpers;
- custom ending, cutscene, boss-map, and enemy presentation assets;
- cutscene audio and exclusive custom-world music.

Ordinary STS2 acts, rooms, encounters, rewards, bosses, and the native finale are used. A narrow Sakiko-only finale compatibility patch bypasses the base Architect dialogue lookup because the current game has no custom-character dialogue row, then calls the native run-victory path.

## Source files missing in STS1

- `sakiko/WishFulfilled.wav` is both registered and requested by the STS1 code, but the file does not exist in the STS1 repository. The card behavior is complete; no invented placeholder audio was fabricated.
- `Weakness` has STS1 localization and a disabled source model but no small or large STS1 portrait. Its compatibility-only placeholder pair uses the exact 250x190 and 500x380 card slots and stable destination filenames. Because the card is skipped, this art is not presented as active content.

There are no missing art assets for any enabled card, in-scope power, relic, potion, character surface, or active VFX.

## Intentional source-faithful inactive routes

- `Hurt3.wav` is registered in STS1 but unreachable because the source random upper bound is exclusive. Native damage voice routing intentionally selects only Hurt1 and Hurt2.
- `GeorgetteMeGeorgetteYou` remains silent because its STS1 sound call is commented out.
- `MusicPulseAttackEffect` image/audio is registered by STS1 but the effect is never instantiated. It remains packaged evidence and has no active route.
- `General1`, `General2`, `Others1`, and other inventory entries labelled “registered but no active call” remain inactive. They were not assigned invented triggers.

## Native platform adaptations

- BaseMod/BaseLib actions, registration, localization, and save extensions were replaced by STS2 models, native commands, native hooks, and narrowly targeted Harmony patches. The release has no BaseLib dependency or assembly reference.
- Persistent deck changes use native command execution. Exact `DeckVersion` identity synchronizes removal from the persistent deck and Hand, Draw, Discard, Exhaust, Play, and visual play-queue surfaces without directly mutating card collections.
- The power-change ledger is combat-scoped and round-rotated by `CombatState.RoundNumber`; immutable events store actual post-modifier signed deltas, change kind, target, applier, power type, and card source.
- Power copying uses `ClonePreservingMutability` followed by `PowerCmd.Apply`, resets ownership, preserves the declared applier policy, and explicitly rejects unsupported temporary, paired, or internal-state powers.
- Hype uses `ShouldClearBlock` and `AfterPreventingBlockClear`. A Sakiko-only compatibility patch covers explicit positive `CreatureCmd.LoseBlock` calls; `LoseBlockInternal` is not globally patched.
- Monochrome Hairband remains on `AfterCombatVictory` and the following native save. The shared persistent-deck add command makes the reward idempotent and excludes final/custom encounters.
- Kings uses one hidden native run modifier with a saved, player-scoped pending set because STS2 has no BaseMod global-save field equivalent. It is injected only for runs containing Sakiko, omitted from the top bar, imports the older card/relic pending flags on load, and preserves the reward even if the starter relic was removed and Kings existed only as a generated combat card.
- The STS1 character deliberately used no animation controller. STS2 combat, rest-site, and merchant models therefore use an approved static first-pass presentation rather than fabricated animations.
- Dazzling uses the original 400x400 source texture, 0.6-second lifetime, target-centered spawn, yellow native overlay, and source sound through a native Godot VFX node.
- `MonsterDivinityPower` mechanics are complete because enabled player cards can apply it to ordinary enemies. Its continuous STS1 ambient particle/aura cosmetics, which depended on STS1 base-game visual classes, are omitted.
- STS2 progression expects a built-in epoch key. Since all Sakiko pools ship unlocked, narrow Sakiko-only prefixes skip the incompatible epoch bookkeeping and do not alter vanilla-character progression.

## Presentation and multiplayer limitations

- Four multiplayer hand textures are replace-later compatibility placeholders matching native 422x1200 slots. They preserve scene topology only.
- Multiplayer behavior, authority, reconnect, replay, and mixed-party determinism are not supported or claimed for v0.1.0.
- Singleplayer on `v0.111.0` (`41cef1ea`) is the tested support target.

### Work required before claiming multiplayer support

The current implementation already has an exact `NetDeckCard` removal action with native synchronization/replay serialization, ledger and Kings state keyed by player Net ID, and explicit owner/participant filters. Those foundations do not constitute multiplayer verification. A later multiplayer release must:

- make every state-changing victory, reward, relic, potion, card, and power callback explicitly host-authoritative and prove callbacks cannot apply twice on host and clients;
- audit random generation and every `PlayerChoiceContext` path so all choices and RNG outcomes are synchronized rather than locally reproduced;
- verify custom action packet conversion, exact `DeckVersion` identity, action ordering, visual play-queue cancellation, and history/hooks on both peers;
- pass real host/client scenarios with two Sakikos and mixed Sakiko/vanilla parties, including extra turns, defeated/disconnected players, multiple enemies, rewards, shops, rest sites, boss rewards, and victory;
- pass save/load, reconnect, replay, disconnect, and resumed-reward tests for all per-player saved and combat-scoped state, including Kings and the power ledger;
- prove local presentation and voice playback occur for the intended client without duplicated remote audio or VFX;
- require matching mod/package versions on every peer and publish a recorded host/client compatibility matrix;
- replace the four multiplayer hand placeholders if polished multiplayer presentation is part of that release.

This is primarily an authority, synchronization, reconnect, and validation phase; it does not require re-porting the 86 enabled cards unless network tests expose a behavior divergence.

The complete future implementation checklist and acceptance matrix are in `MULTIPLAYER_PORT_REQUIREMENTS_AND_TODO.md`.

## Pending or replace-later assets

No enabled single-player card, power, relic, potion, character surface, or active VFX is waiting on replacement art.

| Asset | Current state | Native size | Required for v0.1.0 |
| --- | --- | ---: | --- |
| `images/ui/hands/multiplayer_hand_togawa_sakiko_point.png` | Generated compatibility placeholder | 422x1200 | No; multiplayer is unsupported |
| `images/ui/hands/multiplayer_hand_togawa_sakiko_rock.png` | Generated compatibility placeholder | 422x1200 | No; multiplayer is unsupported |
| `images/ui/hands/multiplayer_hand_togawa_sakiko_paper.png` | Generated compatibility placeholder | 422x1200 | No; multiplayer is unsupported |
| `images/ui/hands/multiplayer_hand_togawa_sakiko_scissors.png` | Generated compatibility placeholder | 422x1200 | No; multiplayer is unsupported |
| `images/card_portraits/weaknesscard.png` | Generated compatibility placeholder | 250x190 | No; Weakness is skipped |
| `images/card_portraits/big/weaknesscard.png` | Generated compatibility placeholder | 500x380 | No; Weakness is skipped |
| `audio/sakiko/WishFulfilled.wav` | Source file does not exist; no placeholder fabricated | Source unavailable | Optional audio replacement only |

Optional future presentation work without a one-file replacement target is an ambient Monster Divinity aura and an animated Sakiko rig. Neither exists as a usable STS1 animation asset, and neither blocks the supported single-player gameplay port.
