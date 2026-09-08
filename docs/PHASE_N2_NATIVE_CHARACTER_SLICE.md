# Phase N2 Native Character Slice

> Current-state review, 2026-09-07: Historical N2 slice, not current content counts. Full native gameplay scope and September 7 feedback are implemented. Presentation checks demonstrate functioning integration only. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Status: complete

Executed: 2026-09-06

Game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This is the execution record for Phase N2 of `NATIVE_FIRST_PORT_WORKFLOW.md`. It preserves the Phase N1 no-BaseLib package boundary and proves the first playable native Togawa Sakiko slice.

## Delivered checkpoint

- The canonical assembly contains 13 explicitly mapped native models: one character, three pools, seven cards, one power, and one relic.
- All mod-owned IDs preserve the legacy `TOGAWASAKIKO-` entry prefix through a narrow `ModelDb.GetEntry(Type)` patch restricted to this assembly's `AbstractModel` subclasses.
- `NativeModelCatalog` registers only the models required by this slice through `ModHelper.AddModelToPool`.
- The character is appended to the native character list by a Togawa-only registry patch.
- English and Simplified Chinese localization are stored as native JSON in the PCK and validated after `ModelDb` initializes.
- The character-select background, character icon, card art, Dazzling icons, and Monochrome Hairband icons resolve from the mod PCK.
- The in-combat character uses an intentional static first-pass `NCreatureVisuals` tree. It does not claim final animation parity.
- The manifest has no mod dependencies, and the built DLL has no BaseLib assembly reference.

## Frozen model IDs

| Category | Entry |
| --- | --- |
| Character | `TOGAWASAKIKO-TOGAWA_SAKIKO` |
| Card pool | `TOGAWASAKIKO-TOGAWA_SAKIKO_CARD_POOL` |
| Relic pool | `TOGAWASAKIKO-TOGAWA_SAKIKO_RELIC_POOL` |
| Potion pool | `TOGAWASAKIKO-TOGAWA_SAKIKO_POTION_POOL` |
| Card | `TOGAWASAKIKO-STRIKE_TOGAWA_SAKIKO` |
| Card | `TOGAWASAKIKO-DEFEND_TOGAWA_SAKIKO` |
| Card | `TOGAWASAKIKO-THE_MOONLIGHT_SONATA_CARD` |
| Card | `TOGAWASAKIKO-A_SPLIT_MOMENT_CARD` |
| Card | `TOGAWASAKIKO-DESIRE_CARD` |
| Card | `TOGAWASAKIKO-TWO_MOONS_CARD` |
| Card | `TOGAWASAKIKO-SILENT_FAREWELL_CARD` |
| Power | `TOGAWASAKIKO-DAZZLING_POWER` |
| Relic | `TOGAWASAKIKO-STARTER_RELIC_TOGAWA_SAKIKO` |

Startup validation requires every mapped type to resolve to its expected native model and rejects missing, duplicate, or drifted IDs before a run begins.

## Playable content

### Character and pools

Togawa Sakiko starts with 72 HP, 50 gold, normal starting energy, Monochrome Hairband, and the nine-card STS1 starter deck:

- four Strike
- four Defend
- one The Moonlight Sonata

Only three behavior-complete common cards are eligible for card rewards: A Split Moment, Two Moons, and Silent Farewell. The relic pool contains only the starter relic, and the potion pool is intentionally empty. No behaviorless legacy content can enter normal generation.

### Cards, power, and relic

| Content | Implemented behavior |
| --- | --- |
| Strike | Costs 1 and deals 6 damage; upgrade adds 3 damage. |
| Defend | Costs 1 and gains 5 block; upgrade adds 3 block. |
| The Moonlight Sonata | Costs 2, deals 15 damage, Exhausts, and creates a card-removal reward; upgrade adds 5 damage. |
| A Split Moment | Costs 0, gains 1 block, and applies 1 Dazzling; upgrade adds 1 to both values. |
| Desire | Token skill costing 1 that gains 8 block; upgrade adds 3 block. |
| Two Moons | Costs 1 and deals 7 damage; it hits twice while the player has Dazzling; upgrade adds 3 damage per hit. |
| Silent Farewell | Costs 0 and applies 1 Vulnerable to all enemies; upgrade adds 1 Vulnerable. |
| Dazzling | Buff counter. Whenever its owner gains a positive Buff amount, it deals damage equal to its amount to a random hittable opponent using unpowered damage. |
| Monochrome Hairband | After a living owner wins combat, creates Desire through `RunState.CreateCard`, adds it to the persistent deck through `CardPileCmd.Add`, and previews the addition. |

The direct native subclasses above are the required card, power, relic, and pool foundation for this slice. No reusable potion base was added because the slice needs only an empty native `PotionPoolModel`; a potion abstraction will be introduced only if later potion behavior demonstrates a shared need.

## Native presentation topology

The two PCK scenes used by the slice are:

- `res://TogawaSakiko/scenes/screens/char_select/togawa_sakiko_background.tscn`: a centered 1920 by 1200 `Control` with a full-rect `TextureRect`.
- `res://TogawaSakiko/scenes/ui/togawa_sakiko_icon.tscn`: a full-rect `TextureRect` using the character icon texture.

The static combat fallback is constructed as a native `NCreatureVisuals` root with the exact named children required by the current combat UI:

- `%FormVfx`: input-transparent `Control`
- `%Visuals`: `Sprite2D` at `(0, -160)` using the Togawa portrait
- `%Bounds`: 168 by 320 input-transparent `Control` at `(-84, -320)`
- `%CenterPos`: marker at `(0, -160)`
- `%IntentPos`: marker at `(20, -340)`
- `%TalkPos`: marker at `(0, -350)`

The factory patch intercepts only `TogawaSakiko`. Character selection and power-image path patches are likewise narrowed to the mod-owned character or power type.

## Save-order proof

The installed game runs `AfterCombatVictory`, pre-finishes the combat room, and then awaits its normal run save. Monochrome Hairband therefore modifies the persistent deck before the game saves it; no `SaveManager` write patch is used.

The automated gameplay run copied the valid post-victory `current_run.save`, started a fresh game process, invoked the real `SaveManager.LoadRunSave` and `RunState.FromSerializable` path, and verified all of the following in the reconstructed run:

- the character is Togawa Sakiko;
- Monochrome Hairband is owned;
- the deck contains the Desire created after combat.

The reload diagnostic patch is enabled only by `--togawa-native-reload-smoke` and does not participate in normal gameplay.

## Repeatable commands

Run these from the repository root after configuring `TogawaSakiko/local.props` from the committed example.

```powershell
dotnet build .\TogawaSakiko\TogawaSakiko.csproj --no-restore
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:PackNativeMod
pwsh -File .\tools\Test-NativePackage.ps1 -PackagePath .\TogawaSakiko\artifacts\stage\TogawaSakiko
```

Deployment remains explicit:

```powershell
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:DeployNativeMod -p:AllowLocalDeploy=true
```

With the staged mod deployed to the selected local game:

```powershell
pwsh -File .\tools\Invoke-NativeLoaderSmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\loader-smoke -Language eng
pwsh -File .\tools\Invoke-NativeLoaderSmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\loader-smoke -Language zhs
pwsh -File .\tools\Invoke-NativeGameplaySmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\gameplay-smoke
```

The gameplay script uses isolated app-data profiles and fixed seeds. Its diagnostic arguments temporarily limit character enumeration only after `ModelDb` initialization, inject A Split Moment only into the dedicated Togawa test run, and force non-release AutoSlay behavior only for the diagnostic process. None of these branches runs during ordinary play.

## Verification evidence

### Build and package

- `dotnet build --no-restore`: passed with zero warnings and zero errors.
- Package validation: passed with exactly DLL, manifest, PCK, and optional PDB; zero manifest dependencies; no BaseLib assembly reference.
- Exported PCK size: 2,622,744 bytes.
- All 12 native localization JSON files parsed successfully.

### Loader

- English: `artifacts/loader-smoke/phase-n2-final-eng-20260906-162853/slay-the-spire-2.log`
- Simplified Chinese: `artifacts/loader-smoke/phase-n2-final-zhs-20260906-162920/slay-the-spire-2.log`

Both runs reported exactly one initializer, one native bootstrap, one 13-model vertical-slice marker, `RunningModded=true`, and no pre-startup managed issue.

### Gameplay, save, reload, and vanilla regression

Artifact root: `artifacts/gameplay-smoke/native-gameplay-20260906-162754`

The automated assertions passed for:

- Togawa character selection;
- static combat portrait creation;
- Dazzling trigger and damage;
- Moonlight Sonata card-removal reward creation;
- Monochrome Hairband adding Desire after victory;
- completed combat reward screen;
- saved character, relic, and Desire IDs;
- fresh-process deserialization of those custom models;
- Ironclad selection and completion of a short vanilla combat.

The Togawa, reload, and vanilla managed-issue counts were all zero. The headless engine can emit `ERROR: Invalid Task ID` during run transitions and shutdown, including in the vanilla diagnostic. It did not accompany a managed exception, mod-load failure, or AutoSlay failure, so it is recorded as headless harness noise rather than a Togawa success signal or a Togawa regression.

## Known limitations carried forward

- The combat character is a static portrait. Idle, attack, cast, hit, and death animation parity remains Phase N4 work.
- Some presentation surfaces and sounds use narrow base-game fallbacks, including Defect selection/transition audio. Final character frames, energy UI, trails, rest-site, merchant, map, and multiplayer presentation remain incomplete.
- Only the three behavior-complete common reward cards are enabled. The remaining card catalog remains excluded from native compilation and live pools.
- Relic and potion reward pools are intentionally empty.
- Multiplayer behavior has not been verified.
- Full installed-PCK extraction and the complete STS1-to-STS2 parity inventory remain outstanding evidence tasks.

## Next gate

Phase N3 implements and proves the shared behavior contracts: persistent deck add and synchronized deck/combat removal, the signed current/previous-round power ledger, safe power cloning, Hype block-loss prevention, and post-victory card-gain regression coverage.
