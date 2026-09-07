# Phase N5: Cards and Powers

Status: complete. All 86 cards enabled in STS1 and all 23 powers required by those cards are native and accepted. Nine STS1-disabled cards are excluded by user-approved scope; the two remaining power models are potion-linked and move with their dependent potions in Phase N6.

Baseline: Slay the Spire 2 `v0.111.0` / `41cef1ea`.

## Current parity

- Cards: 86 of 86 in-scope cards have native implementations and installed-game acceptance. The nine cards disabled in STS1 are excluded from completion and normal generation. The earlier Carefree and Weakness compatibility models remain registered to preserve their stable IDs but are still filtered out of every normal pool; the other seven disabled cards have no canonical model registration.
- Powers: 23 of 25 player-card-relevant STS1 powers have native implementations and acceptance, plus the native Mantra support model. `DazzlingDownPower` and `FreshlySqueezedCucumberPower` are used by potions and are deferred with those potions to Phase N6.
- Audio: all 52 scoped source files remain packaged for source inventory and replacement parity; all 38 active in-scope native routes are wired. Disabled-card voice lines are not completion requirements.
- Art: all 95 source-inventory card slots have exact 250x190 and 500x380 files. `Weakness` uses the generated replacement-ready placeholder because the STS1 project has no portrait, but disabled-card art is not part of behavior completion.

The machine-readable source of truth is `FULL_PORT_PARITY_INVENTORY.json`; `FULL_PORT_PARITY_INVENTORY.md` is its generated review view.

## Accepted batch N5.1

| Model | Stable ID | Native behavior | Pool state |
| --- | --- | --- | --- |
| Greetings | `TOGAWASAKIKO-GREETINGS_CARD` | 0-cost uncommon Skill; Innate; Exhaust; gain 2 energy, or 3 when upgraded; Sakiko voice | Enabled reward card |
| Tiredness | `TOGAWASAKIKO-TIREDNESS_CARD` | 0-cost token Skill; draw 1, or 2 when upgraded; Exhaust; Sakiko voice | Token only; excluded from rewards |
| Melody | `TOGAWASAKIKO-MELODY_CARD` | 0-cost token Attack; deal 6 damage, or 9 when upgraded, through the native attack command | Token only; excluded from rewards |
| Ideal | `TOGAWASAKIKO-IDEAL_CARD` | 1-cost token Power; apply native Free Attack 2, or 3 when upgraded | Token only; excluded from rewards |

`A Split Moment` now uses the same Sakiko voice path. The shared voice command preserves the STS1 five-second per-player cooldown, routes through the SFX bus, skips non-interactive diagnostics, and frees its player node after playback. It does not introduce an external audio dependency.

The STS2 rich-text energy formatter requires a 24x24 pool-named glyph at `res://images/packed/sprite_fonts/togawa_sakiko_energy_icon.png`. The build derives this compatibility path from the existing Sakiko `text_energy.png`, exports it explicitly, and validates its loadability and dimensions. It is a path/layout compatibility copy, not replacement art.

## Accepted cumulative expansion

The verified native set now also includes Protection, Radiance, Kindness, Amoris, Doloris, Mortis, Oblivionis, Timoris, Black Keys, White Keys, Black and White Keys, Voice, Inner Cry, Memento Mori, Budget Bento, Phantom of Sakiko, Phantom of Taki, Phantom of Tomori, Symbol II: Air, Dark Heaven, Georgette Me Georgette You, Hearts Barrier, Daten, Kill KiSS, Symbol IV: Earth, Quaerere Lumina, Kings, Accomplice, Desu Wa, Edge of Breakdown, Masquerade Rhapsody Request, Clock Out, Fallen Flowers, Hachibousei Dance, Phantom of Mutsumi, Phantom of Soyo, Rhinoceros Beetle, and Counting Stars. Carefree and Weakness have additional compatibility coverage but are not counted because STS1 disables them.

Their required native powers include Doloris, Mortis, Oblivionis, Timoris, Monster Divinity, Kings, Curiosity, Endurance, Fearless, Girl of Spring, God's Creation, Our Song, Perdere Omnia, Primo Die in Scaena, Seize the Fate, Shared Destiny, Charismatic Form, Cruelty, Crychic, Pride, Wish You Good Luck, and Worldview, with Mantra implemented as a native support model. These batches cover exact persistent-deck additions and purges, stance transitions, player/enemy Divinity, retained curses, hand-scoped curse effects, actual Strength transfer, deck-size block, selection flows, Scry-window discard accounting, post-victory reward modification, permanent per-card purge growth, prior-card-type retrieval, combat-long Retain, native Gold gain, Plating, all-enemy damage, calculated Block that deliberately bypasses Dexterity, buff-type counting after next-turn Energy is applied, rare card mutation/copy behavior, reward-state persistence, and extra-turn lifecycle contracts. The final accepted installed-game artifact is `artifacts/n7-release/native-n5-batch-20260907-155052` with 725 pure assertions and zero managed issues.

The 17-card/six-power rare batch is accepted. Focused runtime markers pass for rare core behavior, rare powers, persistent mutation/copy behavior, Ave Mujica and Black Birthday, and Symbol III: Water's extra-turn lifecycle.

## Phase N6 handoff

1. Port the ten non-starter relics in dependency order.
2. Port all six potions, implementing `DazzlingDownPower` and `FreshlySqueezedCucumberPower` immediately before their dependent potion tests.
3. Reuse the accepted persistent-deck, power-copy, signed-ledger, Hype, and post-victory/save contracts.
4. Keep all nine STS1-disabled cards and every custom-world family outside completion and normal generation.
5. Execute the complete relic/potion reward, ownership, save/reload, language, Sakiko, and vanilla regression matrix before completing N6.

This order is refined from each card's STS1 Java imports and behavior body. Imports alone are not treated as semantic proof; every model is compared against its Java implementation before coding.

## Automated and installed-game evidence

Current accepted pure-state coverage executes 725 assertions over IDs, type, rarity, target, cost, keywords, base/upgraded values, selection policy, pile semantics, reward eligibility, Kings carrier serialization, saved-property round trips, voice routes, and loadable assets.

The final English and Simplified Chinese loader runs each validate 353 entries and 706 formatted variants with no missing keys, unconverted markers, or localization-formatting errors.

The installed-game diagnostic creates mutable cards in a live Sakiko `CombatState`, uses `CardCmd.AutoPlay`, and verifies the following post-command totals and pile destinations:

- Greetings gains 5 energy across base and upgraded copies, and both copies enter Exhaust.
- Tiredness draws the three prepared cards across base and upgraded copies, and both copies enter Exhaust.
- Melody removes 15 Block across base and upgraded copies, and both copies enter Discard.
- Ideal applies five native Free Attack stacks across base and upgraded copies.
- Quaerere Lumina exposes only the top seven or nine Draw-pile cards, discards the selected exact instances through native pile commands, and gains seven Block for seven actual discards across base and upgraded probes.
- Kings deals 14 or 20 damage, applies one nonstacking debuff, reduces only the following encounter card reward by exactly one option, remains active across regeneration, and clears after a card reward is taken.
- Accomplice creates two or three combat-only Desires and makes every Desire currently in hand free for that turn without changing the persistent deck.
- Carefree draws two or three cards and grants exactly one selected non-Retain hand card native Retain for the rest of combat; it remains excluded from reward generation because STS1 disables it.
- Desu Wa retrieves one or two random cards matching the prior played card's type, consuming eligible Draw-pile cards before falling back to Discard.
- Edge of Breakdown purges the exact selected Discard card through the shared synchronized command and applies two or one Frail. Its English STS1 description says Vulnerable, but the Java behavior and Simplified Chinese source prove Frail.
- Masquerade Rhapsody Request gains one or two permanent damage for each successful Sakiko purge, synchronizes the exact persistent/combat copies by `DeckVersion`, and round-trips its saved growth.
- Weakness starts combat in Discard and remains excluded from normal pools because STS1 disables it.
- Clock Out gains 15 or 20 Gold, adds one exact persistent Tiredness and linked Discard copy, and Exhausts.
- Fallen Flowers applies seven or nine Dazzling and retains native Ethereal behavior.
- Hachibousei Dance deals eight damage, applies eight native Plating, and upgrades only from four to three energy.
- Phantom of Mutsumi deals six damage, gains six Block, and adds a base or upgraded Protection to both persistent deck and combat Discard before Exhausting.
- Phantom of Soyo deals seven damage to every opponent and adds a base or upgraded Kindness to both persistent deck and combat Discard before Exhausting.
- Rhinoceros Beetle gains four or seven base Block plus floor(Dazzling / 2), explicitly using unpowered Block so Dexterity does not modify it.
- Counting Stars first applies one native next-turn Energy, then counts current buff types and applies one or two Dazzling per type.

The dedicated Kings lifecycle runner verifies the natural `AfterCombatEnd` -> player power teardown -> `AfterCombatVictory` -> native save -> reward-generation ordering. The diagnostic removes Monochrome Hairband, uses a combat-generated Kings power with no persistent Kings card, and proves that one hidden native run modifier persists the player-scoped pending state. It captures that exact save, observes the natural two-option reward and natural clear, then starts a second game process, reloads the captured save, generates another two-option encounter reward, selects it through the native reward/deck-add path, and observes `Hook.AfterRewardTaken` clear the restored state.

Accepted artifacts:

- Final 86-card-scope command and lifecycle matrix: `artifacts/n7-release/native-n5-batch-20260907-155052`
- Final Kings no-card/no-relic victory/save/reward/reload contract: `artifacts/n7-release/native-v010-kings-final-clean-20260907-162304`
- Final English loader: `artifacts/n7-release/native-loader-20260907-161706`
- Final Simplified Chinese loader: `artifacts/n7-release/native-loader-20260907-161716`
- Final Phase N3 command/save regression: `artifacts/n7-release/native-n3-contract-20260907-155027`
- Final Sakiko save/reload and vanilla regression: `artifacts/n7-release/native-gameplay-20260907-155247`
- Final staged package: `TogawaSakiko/artifacts/stage/TogawaSakiko`
- Current cumulative command test: `artifacts/n5-batch/native-n5-uncommon-direct-normalized-20260907-050913`
- Current Kings victory/save/reward/reload contract: `artifacts/n5-kings/n5-uncommon-direct-kings-regression-20260907-051257`
- Current English presentation/localization loader: `artifacts/loader/n5-uncommon-direct-eng-20260907-051052`
- Current Simplified Chinese presentation/localization loader: `artifacts/loader/n5-uncommon-direct-zhs-20260907-051115`
- Current English presentation-catalog loader: `artifacts/n4-presentation/n5-uncommon-direct-catalog-eng-20260907-051359`
- Current Simplified Chinese presentation-catalog loader: `artifacts/n4-presentation/n5-uncommon-direct-catalog-zhs-20260907-051401`
- Current Phase N3 command/save regression: `artifacts/n3-contract/n5-uncommon-direct-n3-regression-20260907-051147`
- Current Sakiko save/reload and vanilla regression: `artifacts/gameplay/n5-uncommon-direct-regression-20260907-051212`
- Current scoped gallery: `artifacts/n4-gallery/gallery-20260907-051633`
- N5.1 command test: `artifacts/n5-batch/n5-native-command-batch1-clean-20260906-235901`
- Final rebuilt checkpoint rerun: `artifacts/n5-batch/n5-checkpoint-final-20260907-000527`
- English presentation/localization loader: `artifacts/n4-presentation/n5-batch1-catalog-eng-20260906-235946`
- Simplified Chinese presentation/localization loader: `artifacts/n4-presentation/n5-batch1-catalog-zhs-20260906-235954`
- Phase N3 command/save regression: `artifacts/n3-contract/n5-batch1-n3-regression-20260907-000003`
- Sakiko save/reload and vanilla regression: `artifacts/n5-regression/n5-batch1-full-regression-20260907-000029`
- Refreshed development gallery: `artifacts/n5-gallery/batch1-verified`

All accepted logs report zero managed exceptions, localization-formatting errors, or mod-load failures. The scope-adjustment calibration artifact `artifacts/n5-batch/native-n5-batch-20260907-102920` is retained as negative evidence: it caught a generator regression that dropped Carefree's already-shipped compatibility prompt after disabled cards became excluded. The generator now preserves extra localization keys for every registered native compatibility model without counting disabled cards as in-scope work.

## Commands

```powershell
dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Release --no-restore -v:minimal
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:DeployNativeMod -p:Configuration=Release -p:AllowLocalDeploy=true -nologo
pwsh -NoProfile -File .\tools\Test-NativePackage.ps1 -PackagePath .\TogawaSakiko\artifacts\stage\TogawaSakiko
pwsh -NoProfile -File .\tools\Invoke-NativeN5BatchTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n5-batch
pwsh -NoProfile -File .\tools\Invoke-NativeKingsContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n5-kings
pwsh -NoProfile -File .\tools\Invoke-NativeN4CatalogTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n4-presentation -Language eng
pwsh -NoProfile -File .\tools\Invoke-NativeN4CatalogTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n4-presentation -Language zhs
pwsh -NoProfile -File .\tools\Invoke-NativeN3ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n3-contract
pwsh -NoProfile -File .\tools\Invoke-NativeGameplaySmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n5-regression
```

## Current limitations

- The nine cards disabled in STS1 are intentionally skipped. Carefree and Weakness remain registered only for stable-ID/save compatibility and are excluded from normal pools; the other seven have no canonical model registration.
- The two potion-linked powers were completed with their dependent potions in Phase N6; no active power gap remains.
- Token cards in this batch are registered for exact generation and serialization, but are not added to reward pools.
- Kings persists pending rewards on one hidden Sakiko-run `ModifierModel`, keyed by player Net ID through a native saved property. Existing card/relic saved booleans remain compatibility inputs and migrate into this carrier on load; the removed-relic/combat-generated-card edge case is supported and verified.
- Multiplayer behavior remains unverified and unsupported; multiplayer-specific generation is not enabled.
- No unfinished relic or potion behavior is enabled by this batch. Custom act, event, enemy, encounter, intent, enemy-compatibility power, ending, and exclusive music content is outside scope and must remain absent from the package.
