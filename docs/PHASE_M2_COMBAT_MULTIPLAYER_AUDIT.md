# Multiplayer combat contracts and model audit

Date: 2026-09-08. Static review against the recovered installed-game source at `Recovered/v0.111.0-41cef1ea/src`. Runtime results belong to the multiplayer execution record; this document does not certify the full release acceptance matrix.

## Execution boundary

`CardModel.OnPlay` and native combat hooks execute deterministically on every peer. Their commands must update each peer's copy of state exactly once. An additional host-only guard inside these methods would desynchronize clients. Enqueueing another removal/addition from every callback would duplicate the operation, and awaiting a nested queued action can block the current action. Persistent additions/removals therefore remain inline inside their enclosing synchronized native card, choice/hook, reward, or room flow.

`CardSelectCmd.FromHand`, `FromCombatPile`, `FromSimpleGrid`, and `FromChooseACardScreen` already reserve native choices, show the chooser's local UI, wait for remote responses, and resume through the supplied `PlayerChoiceContext`. Every existing card and Fearless forwards that context. No card choice implementation creates a separate context or selects a local player's pile. Remote waits/disconnect behavior still requires the full acceptance matrix.

Named gameplay RNG streams remain `CombatCardSelection`, `CombatCardGeneration`, `CombatPotionGeneration`, `CombatTargets`, and the native random-pile/attack commands. All candidate sequences originate from owner piles, explicit ordered arrays, the native card pool, or the native opponents collection. No gameplay path in these cards/powers uses `System.Random`, wall-clock time, hash codes, or local visual state to select outcomes. Memento Mori's speed preference changes only visual waits.

Native `CardCmd.PreviewCardPileAdd` and `CardPileCmd.RemoveFromDeck` filter previews using `LocalContext.IsMine`. `PlayerCmd.EndTurn`, used by Symbol III: Water, filters local UI and sets readiness for the supplied owner. These native ownership policies are retained.

## Implemented corrections

- `PowerCopyCommand` now permits its advertised None applier policy and preservation of a null source applier. A non-null applier from another combat remains rejected. The previous nullable inequality accidentally rejected every null applier.
- `PersistentDeckRemovalGameAction.Request` uses the shared authority policy. Only the owning local peer submits ordinary player input; replay never generates new requests. The native transport associates an ordinary action with its sender, so a host cannot send a remote player's deck as if it owned it.
- The removal packet retains `NetDeckCard` and additionally serializes the registered `NetCombatCard` of an exact linked copy. Resolution uses that copy's `DeckVersion` and validates both owners. The native combat-card database keeps identities for removed cards during combat, so earlier deck mutations and duplicate requests cannot retarget another card at a shifted deck index.
- Multiplayer standalone removal requests without any registered linked combat copy fail explicitly. This restriction does not affect normal card/hook purge commands or persistent additions, which already execute inside the synchronized action. The legacy deck-index fallback remains available for single-player standalone requests.
- The power ledger now records at entry to native `Hook.AfterPowerAmountChanged`, before reactive powers execute. The previous last-in-list subscriber could observe Melodia after it had already consumed itself, overwrite the captured amount, reverse gain/loss chronology, or double-record a removal. The narrow prefix records synchronously and preserves the original native Task and gameplay hook order.

## Shared contract review

| Contract | Review result and remaining proof |
| --- | --- |
| Persistent add/copy | As Your Heart Desires, Ave Mujica, Budget Bento, Clock Out, Ether, all five Phantoms, all four Symbols, and Hairband callbacks use owner-bound run creation/loading. Native pile hooks/notifications execute on every peer. A new addition packet is unnecessary for these enclosing synchronized flows. |
| Persistent removal | Sakiko purge and upgraded Memento Mori use exact `DeckVersion` links, owner pile snapshots, and native removal. Hand/Draw/Discard/Exhaust/Play snapshots never search another player. Repeated exact removal returns AlreadyRemoved before growth/rewards. |
| Growth and persistent counters | Masquerade growth deduplicates by concrete persistent reference within the owner's active piles. Seize the Fate updates one persistent card and only its linked copies. Spring Sunlight reads and refreshes only the owner's deck and combat cards. |
| History | Desu Wa uses the owner's last finished play. Imprisoned XII counts only that owner's Desire plays. Symbol I: Fire selects only that owner's prior non-Symbol attack. |
| Power copy | Native cloning resets old-owner event handlers and internal data. Normal/instanced/per-applier stacking uses native `PowerCmd.FindExistingInstanceForStacking`. Explicit temporary, targeted, stance, and unapproved private-state powers remain rejected. |
| Ledger | One weak-table hook per concrete combat observes every creature, keys player identity by Net ID, records actual post-modifier deltas, and clears on combat end or round rewind. Extra turns do not advance RoundNumber. Prefix now captures before reactive nested hooks. Reconnect reconstruction still needs live acceptance. |
| Hype | Native ShouldClearBlock and its selected-preventer callback affect only the exact owner. The explicit-loss prefix remains on public `CreatureCmd.LoseBlock`, requires live combat/positive block/positive requested loss/alive target, and consumes one stack. Damage absorption and teardown remain outside that interception. |
| Rewards | Moonlight Sonata explicitly keys its removal reward to Owner. Kings, purge reward composition, and the new team test cards are tracked in the separate reward adaptation work. |
| Presentation | Existing previews use native local ownership. Card voice ownership is handled by the presentation adaptation. Dazzling impact is one replicated combat presentation event per executing peer and does not consume gameplay RNG. |

## Existing card matrix

Every existing native card source was read. Entries below identify the shared implementation contract reviewed, not an assertion that every per-card multiplayer runtime scenario passed. Carefree and Weakness are compatibility models excluded from ordinary generation; the two new multiplayer test cards are outside this baseline matrix.

| Model | Reviewed behavior boundary |
| --- | --- |
| AccompliceCard | native play; owner/target commands |
| AleaIactaEstCard | native play; owner/target commands |
| AmorisCard | native keyword/owner lifecycle |
| AnglesCard | native play; owner/target commands |
| ASplitMomentCard | native play; owner/target commands; owner/source/participant hook checks |
| AsYourHeartDesiresCard | native play; owner/target commands; native synchronized owner choice; inline persistent mutation/link contract |
| AveMujicaCard | native play; owner/target commands; native synchronized owner choice; inline persistent mutation/link contract; named native RNG |
| BandInvitationCard | native play; owner/target commands |
| BlackAndWhiteKeysCard | native play; owner/target commands; native synchronized owner choice |
| BlackBirthdayCard | native play; owner/target commands |
| BlackKeysCard | native keyword/owner lifecycle |
| BudgetBentoCard | native play; owner/target commands; inline persistent mutation/link contract |
| CarefreeCard | native play; owner/target commands; native synchronized owner choice; excluded compatibility model |
| CharismaticFormCard | native play; owner/target commands |
| ChoirSChoirCard | native play; owner/target commands; owner/source/participant hook checks |
| ClockOutCard | native play; owner/target commands; inline persistent mutation/link contract |
| CountingStarsCard | native play; owner/target commands |
| CrucifixXCard | native play; owner/target commands |
| CrueltyCard | native play; owner/target commands |
| CrychicCard | native play; owner/target commands; named native RNG |
| CuriosityCard | native play; owner/target commands |
| DarkHeavenCard | native play; owner/target commands |
| DatenCard | native play; owner/target commands; native synchronized owner choice; exact purge contract; named native RNG |
| DefendTogawaSakiko | native play; owner/target commands |
| DesireCard | native play; owner/target commands |
| DesuWaCard | native play; owner/target commands; owner-filtered combat history; named native RNG |
| DolorisCard | native keyword/owner lifecycle |
| EdgeOfBreakdownCard | native play; owner/target commands; native synchronized owner choice; exact purge contract |
| EnduranceCard | native play; owner/target commands |
| EtherCard | native play; owner/target commands; inline persistent mutation/link contract |
| FallenFlowersCard | native play; owner/target commands |
| FearlessCard | native play; owner/target commands |
| GeorgetteMeGeorgetteYouCard | native play; owner/target commands |
| GreetingsCard | native play; owner/target commands |
| HachibouseiDanceCard | native play; owner/target commands |
| HeartsBarrierCard | native play; owner/target commands |
| IdealCard | native play; owner/target commands |
| ImprisonedXIICard | native play; owner/target commands; owner-filtered combat history |
| InnerCryCard | native play; owner/target commands |
| KaoCard | native play; owner/target commands; owner/source/participant hook checks |
| KillKiSSCard | native play; owner/target commands |
| KindnessCard | native play; owner/target commands |
| KingsCard | native play; owner/target commands; owner/source/participant hook checks; per-instance native saved property |
| MasksCard | native play; owner/target commands; native synchronized owner choice |
| MasqueradeRhapsodyRequestCard | native play; owner/target commands; per-instance native saved property |
| MelodyCard | native play; owner/target commands |
| MementoMoriCard | native play; owner/target commands; inline persistent mutation/link contract |
| MortisCard | native keyword/owner lifecycle |
| OblivionisCard | native keyword/owner lifecycle; owner/source/participant hook checks |
| OurSongCard | native play; owner/target commands |
| PerdereOmniaCard | native play; owner/target commands; owner/source/participant hook checks |
| PerfectionCard | native play; owner/target commands; native synchronized owner choice; named native RNG |
| PhantomOfMutsumiCard | native play; owner/target commands; inline persistent mutation/link contract |
| PhantomOfSakikoCard | native play; owner/target commands; inline persistent mutation/link contract |
| PhantomOfSoyoCard | native play; owner/target commands; inline persistent mutation/link contract |
| PhantomOfTakiCard | native play; owner/target commands; inline persistent mutation/link contract |
| PhantomOfTomoriCard | native play; owner/target commands; inline persistent mutation/link contract; named native RNG |
| PrideCard | native play; owner/target commands; named native RNG |
| PrimoDieInScaenaCard | native play; owner/target commands |
| ProtectionCard | native play; owner/target commands |
| QuaerereLuminaCard | native play; owner/target commands; native synchronized owner choice |
| RadianceCard | native play; owner/target commands |
| RhinocerosBeetleCard | native play; owner/target commands |
| SeizeTheFateCard | native play; owner/target commands; inline persistent mutation/link contract; per-instance native saved property |
| SharedDestinyCard | native play; owner/target commands |
| SilentFarewellCard | native play; owner/target commands |
| SoraNoMusicaCard | native play; owner/target commands; native synchronized owner choice |
| SpringSunlightCard | native play; owner/target commands; owner/source/participant hook checks |
| StayEleganceCard | native play; owner/target commands; named native RNG |
| StrikeTogawaSakiko | native play; owner/target commands |
| SymbolIFireCard | native play; owner/target commands; inline persistent mutation/link contract; owner-filtered combat history |
| SymbolIIAirCard | native play; owner/target commands; inline persistent mutation/link contract |
| SymbolIIIWaterCard | native play; owner/target commands; inline persistent mutation/link contract; named native RNG |
| SymbolIVEarthCard | native play; owner/target commands; inline persistent mutation/link contract |
| TheGirlWithFlaxenHairCard | native play; owner/target commands |
| TheMoonlightSonataCard | native play; owner/target commands |
| TimorisCard | native keyword/owner lifecycle |
| TirednessCard | native play; owner/target commands |
| TwoMoonsCard | native play; owner/target commands |
| UtopiaCard | native play; owner/target commands |
| VeritasCard | native play; owner/target commands; owner/source/participant hook checks |
| VoiceCard | native play; owner/target commands |
| WeaknessCard | native keyword/owner lifecycle; owner/source/participant hook checks; excluded compatibility model |
| WhiteKeysCard | native keyword/owner lifecycle |
| WishFulfilledCard | native play; owner/target commands; native synchronized owner choice; exact purge contract |
| WishToBecomeHumanCard | native play; owner/target commands; owner/source/participant hook checks |
| WishYouGoodLuckCard | native play; owner/target commands |
| WorldviewCard | native play; owner/target commands |
## Power matrix

All existing native power implementations were inspected, including support and compatibility classes. Native per-instance state, owner/source filters, and participant sets keep duplicate Sakiko players independent.

| Model | Reviewed behavior boundary |
| --- | --- |
| CharismaticFormPower | Opposing-side buff gains only; native copy policy; owner receives copies. |
| CrueltyPower | Only owner's Desire play draws for owner. |
| CrychicPower | Owner's turn; one named generation draw; owner receives Phantom. |
| CuriosityPower | Instance-local map keyed by owner's power card; removes captured entry on play completion. |
| DazzlingDownPower | Owner must participate in turn end; affects only owner's Dazzling. |
| DazzlingPower | Only owner's buff gains; native opponent ordering and CombatTargets; local presentation does not choose targets. |
| DolorisPower | One native enemy-side duration tick; no per-player tick loop. |
| EndurancePower | Notified with the mutated deck owner; block targets that owner. |
| FearlessPower | Owner must participate; native owner choice context; exact purge. |
| FreshlySqueezedCucumberPower | Only target owner's next turn grants/removes delayed block. |
| GirlOfSpringPower | Removed on owner's next turn; Dazzling consults owner's instance only. |
| GodsCreationPower | Damage reduction checks exact target owner. |
| HypePower | Exact owner/preventer checks; shared explicit-loss contract. |
| KingsPower | Per-owner marker; pending reward persistence reviewed separately. |
| MelodiaPower | Only this instance's positive gain; threshold consumption now recorded before nested hooks. |
| MonsterDivinityPower | Energy to player owner only; damage requires exact dealer; participant-scoped expiration. |
| MortisPower | HP loss requires exact owner; owner receives Injury via named native random placement; native enemy-side duration tick. |
| OblivionisPower | One native enemy-side duration tick; no per-player tick loop. |
| OurSongPower | Instance-local owner attack capture; dealer/card source checks; participant-scoped expiration. |
| PerdereOmniaPower | Only owner's unplayable removable drawn card is purged; owner draws replacement. |
| PridePower | Instance-local captured card clones; owner participant receives generated copies; unsupported generic copy remains rejected. |
| PrimoDieInScaenaPower | Energy and shuffle removal require exact owner. |
| SeizeTheFatePower | Owner participant receives Hype. |
| SharedDestinyPower | Instance-local set; only new owner buffs produce owner draws. |
| TimorisPower | Owner must participate in player-side start; applies Vulnerable to exact owner. |
| WishYouGoodLuckPower | Exact damage target owner; only owner receives Thorns. |
| WorldviewPower | Only owner's unplayable draw; native owner's attack pool and generation stream. |

## Harness probes added

The explicitly enabled `CombatMultiplayerContractGameAction` executes the shared tests using the native action's `GameActionPlayerChoiceContext`. The packet is discovered through the same native action serializer as other custom actions. Its probes cover every participating player's duplicate-card identity, linked-copy removal, deck-index shift, wrong-owner rejection, packet round trip, repeat-removal idempotence, Hype explicit block retention and teammate isolation, null-applier power copy, and nested Melodia ledger chronology/provenance. The fixture removes its temporary cards and powers; added Block and Divinity energy are intentional deterministic fixture state.

`CombatMultiplayerEdgeDiagnostics` adds a bounded matrix inside that same replicated action: each native party slot actually plays a different number of generated Desires and Imprisoned XII, verifies owner energy and teammate isolation, and then resolves Symbol I: Fire's previous-attack helper against the real interleaved native history containing identical attack models owned by every player. Dazzling checks first-gain and remote-buff RNG non-consumption, and exactly one native CombatTargets draw plus two damage for its owner's buff. Power-copy checks separate Bomb instances across all players, dynamic-variable retention, removal-event isolation, and separate Strangle instances per applier with same-applier stacking. Generated cards, temporary powers, block and energy are restored; native histories and RNG advances remain in the recorded state. These tests intentionally do not simulate a round change or extra turn by manually calling global hooks.

`MultiplayerReplayDiagnostics` reconstructs the same native debug combat using the recorded initial run and native replay service, validates the frozen package sidecar plus native game/commit/model identity, replays all native events, compares every generated checksum ID and value using the native anonymized-state algorithm, waits for all actions, and compares the final complete native combat state. Native replay anonymizes player IDs with random unsigned values, so all test mutation ordering follows saved party slots, never numeric NetId sorting. Preliminary two-slot playback passed six native checksums per slot before the final edge helper; the final package must be captured and replayed again after integration. This is recorded-combat proof only, not post-combat reward or reconnect playback.

The edge helper then passed on both real mixed-party ENet peers in `artifacts/multiplayer/native-multiplayer-contract-20260908-011310/mixed-host`, with the harness reporting native state equality and purge-reward success. Its player-zero replay also passed all six native checksums and final native checksum `3073651128`. These are intermediate integration results; final-package matrix and replay evidence is owned by the multiplayer harness report.

## Outstanding acceptance boundaries

Final task evidence, 2026-09-08: the subsequent full multiplayer matrix passed six scenarios, 14 live peers, and 14 native replay perspectives with 114 matching replay checksums. All six potion probes were included in every applicable party action. Single-player N3/N5/N6/Kings, both N4 locales, both starting-room locales, vanilla control, and full N7 victory/save-reload regression passed. Exact package identities and the distinction between implemented development scope and broad release gates are recorded in [MULTIPLAYER_IMPLEMENTATION.md](MULTIPLAYER_IMPLEMENTATION.md).

The first single-player N5 rerun reported an explicit Perfection assertion failure after the random selector chose Perdere Omnia. Native `CardEnergyCost` intentionally ignores a zero-cost modifier for a negative canonical cost and preserves its unplayable -1 sentinel. The original test assumed random option zero always had a payable cost. The narrow diagnostic correction deterministically chooses a positive-cost offered card for both real Perfection plays, retains the free-this-turn/two-cost/Exhaust checks, and separately verifies the native negative-cost boundary on Perdere Omnia. This changes test selection only, without filtering or changing Perfection's gameplay pool. The failed log is retained under `artifacts/multiplayer/regression/native-n5-batch-20260908-004945`.

Static review and the bounded contract probes do not establish maximum-party full-run victory, reconnect/replay playback, remote interactive-choice cancellation/disconnect, latency, every relic/potion combination, every per-card runtime scenario, or cross-mod interactions. The full single-player regression suite and all mandatory multiplayer acceptance rows remain the release gate. Generated art status is unchanged by this combat audit.
