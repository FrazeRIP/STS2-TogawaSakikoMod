# Multiplayer relic, potion, starting-choice, and presentation audit

Implementation review: 2026-09-08. Source target: recovered Slay the Spire 2 v0.111.0 (`41cef1ea`). This record covers implementation and model contracts; it does not claim live host/client, reconnect, replay, or complete multiplayer acceptance.

Final task evidence, 2026-09-08: the subsequent six-scenario native ENet matrix passed starting-event owner choices, host-only save and reconstruction, all six potion consumption/effect probes per owner, and 14 native replay perspectives. See [final package evidence and remaining limits](MULTIPLAYER_IMPLEMENTATION.md). Historical prospective statements below describe the audit stage; remote manual rendering and live reconnect are still not certified.

## Starting choices

Standard multiplayer keeps the room's canonical native `NEOW` identity. The native `EventSynchronizer.BeginEvent` creates one mutable event for each player on every peer; `OptionIndexChosenMessage` resolves the sender's mutable event. The new `SakikoMultiplayerNeowPatch` supplies Another Mask, The Third Movement, and Blazing Hairband only to Sakiko-owned Neow instances in standard multiplayer starts without visible modifiers. Vanilla owners use the original `GenerateInitialOptions`, its independent owner/slot event RNG, dialogue, and reward behavior. Solo Sakiko continues to use the existing Ocean of Memories event ID for save compatibility.

Each Sakiko choice has an instance-scoped weak selection guard. Native `AncientEventModel.Done` records that owner's three ancient choices and chosen result. Native `EventRoom` marks the room pre-finished only after every player's event finishes. Native `EventSynchronizer.AwaitPendingOptionTasks` remains responsible for awaiting option effects before room exit. Another Mask's existing saved `AppliedStartingChange` guard, owner-only starter removal, upgrade-preserving native transformation, and transient in-progress guard are retained. No new synchronization packet or shared choice state is needed.

The Sakiko-owned dialogue view skips the generic first-ever introduction and uses the already populated custom Sakiko dialogue keys. The canonical Neow dialogue set and vanilla-owned views remain unchanged.

## Relics and potions

| Content | Owner and synchronization review |
| --- | --- |
| Monochrome Hairband, Blazing Hairband | Native `Hook.AfterCombatVictory` iterates all run listeners on each peer. Existing relic-instance combat guards are set before awaiting; dead owners are excluded. Native deck additions execute in that enclosing deterministic lifecycle, before native save. Blazing Hairband consumes `CombatCardGeneration` from the same iteration order. Preview uses `LocalContext.IsMine`. |
| Another Mask | Conversion acts only on `Owner.Deck`, removes only the owner's starter, and is guarded by a saved property. Native event choice routing provides per-player synchronization. |
| Colorful Notebook | Replaces the throwing context with an owner-bound native `HookPlayerChoiceContext`. Its application task is assigned immediately; a downstream choice is deferred into the native synchronized owner queue while room entry can finish. This matters because Dazzling can invoke damage/power hooks. |
| Warmth-Infused Porcelain Cup | Fixed a multiplayer ownership defect: an unrelated player's reward hook previously evaluated `NextFloat` before the owner predicate. Ownership now returns before touching that player's reward RNG. Same-owner probability and existing Cup/Kings composition remain unchanged. |
| Golden Pocket Watch, The Third Movement | Card-play and dealer predicates match the relic owner. Persistent counters remain saved and instance-owned. |
| The Doll | Player-turn predicate matches its owner; transient combat progress is reset at combat start. Saved counter remains instance-owned. |
| The Compass | Participant predicate requires its owner's creature; enemies are visited in native combat-state order. Uses incoming native choice context. |
| Cute Animal Band-Aid | Reward recipient must match the owner, be alive, and be in pre-finished combat. |
| Fountain Drink | Reward predicate matches the owner and combat room; full-slot behavior preserved. |
| Masquerade Mask | Receives owner-specific purge notifications, maintains its saved instance counter, and adds native removal rewards for that owner. Reward persistence changes are covered separately by the multiplayer reward implementation. |
| All six potions | Every `OnUse` receives the native potion-use action context. Every potion remains self-targeted, checks the target, and applies powers/generated cards with its own owner. No peer-local gameplay mutation was found. |
| Matcha Parfait | Inclusive damage range remains 6–30. Draws from synchronized `CombatCardGeneration` inside the native potion action and creates the Melody for its own owner. |

The audit intentionally does not introduce host-only guards into these replicated gameplay callbacks: each peer must reconstruct the same state from the same synchronized action or native lifecycle.

## Presentation and audio policy

Card voices and hurt voices play only for the local Sakiko owner. A remote Sakiko's card and hurt voices are silent. Character-select playback remains attached to the local selection UI. Dazzling damage impact remains visible/audible once in each peer's combat scene, including remote actions; it is local presentation attached to the target creature and does not send presentation packets. Hurt-variant selection continues to use cosmetic `Random.Shared`, never gameplay RNG.

Audio cooldown and prior-variant state now clears on native run initialization, cleanup, and local-Net-ID assignment. The helper also detects replacement of the local `Player` object, covering reconstructed local owners even when the same Net ID is reused. This state is never serialized or checksummed.

Character combat textures, rest/shop visuals, map/top-panel/icon/card-back/energy assets, and all four multiplayer hand paths already route through the character model or creature instance. Another Mask sprite lookup uses that creature's player relics; Master of Melodia retains precedence. Hairband card previews remain local-owner-only. Native relic flash events remain attached to the correct relic instance.

No new image was necessary for the requested functionality. The existing point, rock, paper, and scissors PNG headers were rechecked and all are exactly 422×1200. Their paths remain stable and their generated-placeholder status is unchanged; this audit does not certify final artwork or a rendered remote-player screen.

## Added verification surface

`MultiplayerRelicPresentationContractTests.Run()` creates an isolated mixed party with two Sakiko owners and one Ironclad, checks native versus fixed choice boundaries and exact ordering, checks independent relic-preview owners and localization, checks canonical dialogue preservation, checks the complete unrelated player's reward RNG state remains unchanged, checks Another Mask's saved guard metadata, and checks local/remote/no-owner audio predicates. The return value is its assertion count. This is callable by the multiplayer harness after native model registration and localization initialization.

Live multiplayer choice timing, Another Mask save/reconnect, room-finish ordering, six potion use/consumption flows, remote presentation, and all-player reward composition still require the task's runtime acceptance matrix. Static source inspection alone is not evidence of matching network checksums.

## Opt-in native starting-event harness phase

`MultiplayerStartingEventDiagnostics.RunAsync(state, barrier)` is an additional phase for the genuine ENet harness, called after map/assets setup and before combat. Enable with `--togawa-mp-starting-event` and enable the harness's native save flag for this phase. Existing process-isolated app-data roots are required. `--togawa-mp-starting-choice-offset=0`, `1`, or `2` rotates the fixed choices across Sakiko owners so duplicate/mixed-party scenarios can cover all three outcomes.

The helper enters the actual starting map coordinate; only the owning process calls `EventSynchronizer.ChooseLocalOption`. It waits for that owner's finished native event and pending tasks, verifies every unrelated player's deck/relic inventory is untouched, checks exact Sakiko decks/relics, and compares each peer's deck/relic/history/RNG snapshot. It then waits for all-player ancient pre-finish and native save completion, verifies the multiplayer save exists only in the host profile, loads/canonicalizes the host save through `SaveManager`, and uses native JSON serialization plus `RunState.FromSerializable` to check every player's reconstructed inventory and independent ancient history. Another Mask's saved conversion guard is exercised again after reconstruction.

The phase also checks owner assignment, stable identity, and save reconstruction for every one of the 12 relics and six potions under every player's context. Fresh detached counter-bearing relic fixtures receive distinct slot-derived values; serialized counter state must survive reconstruction. These fixtures are never added to the live inventories or invoked as gameplay hooks.

Artifacts are `peer-*-starting-state-*.json`, `peer-*-starting-run.save`, and `peer-*-starting-result.json`. The result explicitly distinguishes native host-save reload and detached state reconstruction from a fresh reconnect session. Presence of the helper is not a passing runtime result; only completed artifacts and clean matching peer logs count as evidence.

Peer comparisons normalize the detached serialized player's five `Discovered*` collections because native discovery updates belong to local presentation/progress. The first live mixed-host attempt demonstrated this distinction: after Another Mask, the sole peer snapshot difference was its entry in the local owner's `discovered_relics`. Decks, relic state, player/run RNG, history, and all other serialized gameplay fields remain compared. Native save files and their reconstruction preserve all discovery collections.
