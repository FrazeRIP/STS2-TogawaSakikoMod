# Phase N6: Relics and Potions

> Current-state review, 2026-09-07: All 11 original relics and six potions remain implemented. Another Mask is an approved native addition (12 relics total), offered with The Third Movement and Blazing Hairband in Ocean of Memories. Hairband previews and relic visible bounds were adjusted. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Status: complete. All 11 concrete STS1 relics, all six concrete STS1 potions, and both potion-linked powers are native, packaged, and accepted on Slay the Spire 2 `v0.111.0` / `41cef1ea`.

Scope: the nine cards disabled in STS1 are not completion work and have no normal generation route. Carefree and Weakness remain registered only as stable-ID/save compatibility models. Custom acts, events, enemies, encounters, intents, endings, cutscenes, exclusive music, and custom-enemy compatibility powers remain removed from the canonical package.

## Implementation decisions

- Relics and potions inherit only the mod-owned `SakikoRelicModel` and `SakikoPotionModel` bases. No BaseLib type or API is used.
- Every state change uses native commands, model hooks, reward APIs, or saved properties. The implementation does not directly mutate combat card piles, persistent deck collections, creature power lists, or potion collections.
- Togawa-only presentation is handled by the narrow `N6PresentationPathPatch`; it supplies existing STS1 relic and potion resources without changing vanilla model behavior.
- The Sakiko relic pool contains the intended non-starter pool relics. Ancient, Shop, and Event relics retain their native rarity/routing semantics instead of being inserted into ordinary reward tiers.
- Chocolate Milk Jelly, Freshly Squeezed Cucumber, and Orange Milk Jelly remain shared potions as in STS1. Earl Grey Tea, Hallucination Potion, and Matcha Parfait are Sakiko-pool potions.
- The two STS1 potion powers are native `PowerModel` implementations: `DazzlingDownPower` removes its matching Dazzling at side-turn end, and `FreshlySqueezedCucumberPower` grants its stored Block at the owner's player-turn start.

## Relic contracts

| Relic | Rarity | Native contract |
| --- | --- | --- |
| Monochrome Hairband | Starter | Retains the accepted Phase N3 `AfterCombatVictory` behavior, shared persistent-deck add command, exact one-per-combat idempotence, final/custom encounter exclusion, and following native save order. |
| Blazing Hairband | Ancient | Native boss swap removes the unmelted starter through `RelicCmd.Remove`; each eligible victory adds exactly one random combat-valid card through `PersistentDeckMutation`, excludes disabled compatibility cards, and is idempotent per combat identity. |
| Colorful Notebook | Common | `AfterRoomEntered` applies exactly one Dazzling to its owner for combat rooms only. |
| Cute Animal Band-Aid | Common | `AfterRewardTaken` heals its living owner for one only after successful reward selection in a pre-finished combat room; failed selection and pre-victory calls do nothing. |
| Fountain Drink | Common | The native potion-reward hook forces a potion reward after Monster, Elite, or Boss combat only when the owner has no open potion slot. |
| Golden Pocket Watch | Uncommon | Counts only owner card plays, grants two Strength on every twelfth play, preserves remainder, and serializes the counter. |
| Masquerade Mask | Rare | Counts only successful `SakikoPurgeCommand` removals; every third purge adds one native `CardRemovalReward`, preserves remainder, and serializes the counter and rewards. |
| The Compass | Shop | At the end of an owner-participating side turn, applies two Dazzling to the owner and one to every hittable enemy. |
| The Doll | Uncommon | Resets at combat start, applies two Hype exactly once at the owner's second player-turn start, and safely normalizes its transient counter after victory/reload. |
| The Third Movement | Event | Stores three uses; only powered attack damage from the owner's Moonlight Sonata is tripled, one use is consumed after each such card play, and unrelated damage is unchanged. |
| Warmth-Infused Porcelain Cup | Ancient | On the intended 50 percent reward roll, replaces the first existing option with Heart's Barrier without changing option count or duplicating an existing Heart's Barrier; composes with Kings' two-card reward. |

## Potion contracts

| Potion | Rarity and route | Native effect |
| --- | --- | --- |
| Chocolate Milk Jelly | Uncommon, shared | Applies one native Free Attack. |
| Earl Grey Tea | Common, Sakiko | Applies two Dazzling. |
| Freshly Squeezed Cucumber | Uncommon, shared | Applies a 20-Block turn-start power. |
| Hallucination Potion | Common, Sakiko | Applies five Dazzling and matching delayed Dazzling Down. |
| Matcha Parfait | Uncommon, Sakiko | Generates one combat-only Melody in Hand with deterministic combat RNG damage from 6 through 30 inclusive. |
| Orange Milk Jelly | Uncommon, shared | Applies one native One-Two Punch, doubling the next eligible attack. |

All six potions use native combat-only/self-targeting declarations and were procured and consumed through the game's potion command path in the focused installed-game test.

## Automated coverage

`N6PureContractTests` executes 73 assertions over:

- stable model IDs, model counts, relic rarities, potion rarities, potion usage and targeting;
- reward-pool membership and shared-versus-character potion routing;
- Blazing Hairband card eligibility and one-combat identity selection;
- Fountain Drink room/slot predicates;
- Golden Pocket Watch and Masquerade Mask counter rollover;
- The Third Movement source/dealer/value-property eligibility;
- Warmth-Infused Porcelain Cup replacement selection;
- potion potency, Matcha damage bounds, saved-property round trips, and loadable presentation resources.

The generated parity inventory reports:

- 86 of 86 enabled STS1 cards native, with nine disabled cards excluded;
- 25 of 25 player-relevant STS1 powers native;
- 11 of 11 concrete STS1 relics native;
- six of six concrete STS1 potions native;
- zero missing or wrong-sized in-scope presentation pairs.

## Actual-game acceptance evidence

Final accepted N6 contract artifact: `artifacts/n7-release/native-n6-contract-20260907-155227`.

The isolated installed-game run passed 15 required runtime markers and proved:

- Blazing Hairband's native starter removal, random persistent add, disabled-card exclusion, and repeated-callback idempotence;
- Colorful Notebook's combat-room entry ordering;
- procurement and native consumption of all six potions, including a generated 14-damage Melody and a doubled six-damage Melody attack for 12 damage;
- Fountain Drink's full-slot and open-slot branches;
- Compass owner-side participant filtering;
- Pocket Watch's twelfth-card Strength trigger;
- Doll's one-time second-turn Hype trigger;
- Third Movement's exact `45/45/45/15` Moonlight Sonata damage sequence;
- Cup and Kings hook composition while preserving a two-card reward;
- Mask counting successful purges but not prevented removals;
- Band-Aid healing exactly one only after successful post-combat reward selection.

The same run wrote a native mid-combat save and a second game process restored Pocket Watch `7`, Masquerade Mask `2`, Doll `1`, Third Movement `1`, one Earl Grey Tea, and five serialized card-removal rewards. Both processes reported zero managed issues.

Final cross-phase artifacts:

- N5 all-enabled-card regression: `artifacts/n7-release/native-n5-batch-20260907-155052`
- N3 command/hook/save regression: `artifacts/n7-release/native-n3-contract-20260907-155027`
- English catalog loader: `artifacts/n7-release/native-loader-20260907-161706`
- Simplified Chinese catalog loader: `artifacts/n7-release/native-loader-20260907-161716`
- Sakiko combat/reward/save/quit/reload and vanilla Ironclad regression: `artifacts/n7-release/native-gameplay-20260907-155247`
- Validated staged package: `TogawaSakiko/artifacts/stage/TogawaSakiko`
- Installed package: `Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko`

The final build completed with zero warnings and zero errors. Package validation found zero dependencies, no BaseLib assembly reference, no excluded runtime type, and no excluded custom-world content token. English and Simplified Chinese loader tests found no startup or formatting issue. The focused N3, N5, N6, Sakiko reload, and vanilla runs found no managed exception, localization-formatting error, unknown model ID, or Togawa load failure.

## Commands

```powershell
pwsh -NoProfile -File .\tools\Build-Sts1ParityInventory.ps1
.\tools\Build-NativeLocalizationCatalog.ps1
.\tools\Build-NativePresentationCatalog.ps1
dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Release --no-restore -v:minimal
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:DeployNativeMod -p:Configuration=Release -p:AllowLocalDeploy=true -nologo
pwsh -NoProfile -File .\tools\Test-NativePackage.ps1 -PackagePath .\TogawaSakiko\artifacts\stage\TogawaSakiko
.\tools\Invoke-NativeN6ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n6-contract -GameplayTimeoutSeconds 240 -ReloadTimeoutSeconds 60
.\tools\Invoke-NativeN5BatchTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n5-batch -RunName native-n5-batch-final -TimeoutSeconds 240
.\tools\Invoke-NativeN3ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n3-contract -RunName native-n3-contract-final -GameplayTimeoutSeconds 240 -ReloadTimeoutSeconds 60
.\tools\Invoke-NativeN4CatalogTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n4-presentation -Language eng -RunName native-n4-catalog-final -TimeoutSeconds 60
.\tools\Invoke-NativeN4CatalogTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n4-presentation -Language zhs -RunName native-n4-catalog-final -TimeoutSeconds 60
.\tools\Invoke-NativeGameplaySmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n6-regression -RunName native-n6-final-regression -GameplayTimeoutSeconds 240 -ReloadTimeoutSeconds 60 -VanillaTimeoutSeconds 180
```

## Limitations and N7 outcome

- Multiplayer support remains unverified and unsupported. The owner/participant predicates are explicit, but host/client authority, duplicate callbacks, reconnect, and mixed-character multiplayer require the N7 support decision and declared test matrix.
- Phase N6 uses focused deterministic installed-game diagnostics plus Sakiko and vanilla regression. Phase N7 subsequently passed the natural new-game-to-final-victory run and complete save/reload matrix.
- The known headless Godot renderer messages are native engine diagnostics, not managed exceptions; no acceptance script suppresses managed failures.
- Disabled card models are not port scope. Carefree and Weakness remain inert compatibility IDs only, and the other seven disabled cards have no canonical registration.
- Custom-world content and its exclusive assets are removed rather than deferred.

Phase N7 completed the full-run and save-location matrix, audited all audio routes, retained source-faithful inactive registrations, declared multiplayer unsupported, tested companion-mod isolation, and produced the reviewed self-contained release archive and missing/difference report.
