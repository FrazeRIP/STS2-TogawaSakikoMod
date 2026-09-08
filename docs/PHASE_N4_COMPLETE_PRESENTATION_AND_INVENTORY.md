# Phase N4 Complete Presentation and Inventory

> Current-state review, 2026-09-07: Presentation wiring and inventory checks are complete; final assets are not. Feedback adds rest/shop/mask placeholders, adapted card UI, relic normalization and the approved starting event. Recorded package coverage is 345 textures, 52 audio assets, 11 standalone resources and 17 localization files. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Status: complete

Executed: 2026-09-06; scope refresh verified 2026-09-07

Starting point: branch `Test1`, commit `3a35047`

Game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This is the execution record for Phase N4 of `NATIVE_FIRST_PORT_WORKFLOW.md`. It preserves the Phase N1 native-only package, the Phase N2 playable slice, and the Phase N3 shared contracts while completing presentation and source-truth inventories without making unfinished content reachable in normal play. The 2026-09-07 scope refresh removed custom act, event, enemy, encounter, intent, ending, cutscene, and exclusive music content from canonical resources and all generated catalogs.

## Delivered presentation

### Character surfaces

Sakiko now owns native paths for character selection, combat visuals, the top panel, map marker, energy counter and energy icon, card frame, transition, rest site, merchant, card trail, locked selection state, and all four multiplayer hand gestures.

- Five self-contained Godot scenes cover combat visuals, energy, rest site, merchant, and card trail.
- Two mod-owned materials cover the character transition and card frame.
- The scene scripts are canonical C# under `NativeCode/Presentation/Godot`; exported scenes do not depend on inaccessible base-project `res://src` scripts or base scene inheritance.
- The STS1 character intentionally used `AbstractAnimation.Type.NONE`, so combat, rest-site, and merchant presentation remains static rather than inventing unsupported animation behavior.
- Eleven direct texture contracts use recovered native dimensions: top-panel icon/outline 88x88, select/locked icons 132x195, map marker 49x64, energy icon 71x72, transition 2560x1200, and four multiplayer hands 422x1200.
- The cropped rest-site portrait is 886x647 and is also validated by the package texture catalog.
- Narrow compatibility patches apply only to Sakiko's exact card-pool key and exact static merchant/rest-site nodes.

Attack, cast, and death sounds deliberately retain the Defect fallback until the sound-routing phase. The four multiplayer hands are replace-later compatibility placeholders derived from native Defect topology, recolored for Sakiko, and marked with the neutral placeholder badge. Multiplayer support is not declared.

### Content presentation inventory

`FULL_PORT_PARITY_INVENTORY.json` and its generated Markdown view are the source-of-truth inventory:

| Domain | Concrete STS1 models | Native behavior now | Behavior pending | Presentation result |
| --- | ---: | ---: | ---: | --- |
| Cards | 95 | 36 | 59 | 95 exact 250x190/500x380 pairs |
| Powers | 25 | 7 | 18 | 25 custom 32x32/84x84 pairs |
| Relics | 11 | 1 | 10 | 11 icon/outline/large sets |
| Potions | 6 | 0 | 6 | 6 complete native layer sets |

Nine source-disabled cards remain disabled. `WeaknessCard` was the only concrete source model with missing STS1 art; its deterministic replace-later pair uses the generated placeholder master at the exact native card dimensions. Eleven enemy-only or custom-enemy compatibility powers are excluded, including `PlayerFilightPower` and `StrengthUpPower`; `MonsterDivinityPower` remains in scope because player cards apply it to ordinary enemies.

The 2026-09-07 scope correction regenerated the static catalog at 336 textures, 25 in-scope power models, and 137 total model records. Fresh installed-game acceptance of that regenerated catalog is part of the active N5 regression pass; the original N4 runtime artifacts below remain historical evidence for the pre-correction catalog.

The original N4 PCK catalog validated 338 package textures, 52 playable audio resources, 10 standalone resources, and 15 live localization files. The scope-corrected generated catalog now contains 336 textures. The source synchronizer deletes excluded world-content resources and compatibility-power icons before copying and refuses to reintroduce them. Copying or packaging presentation does not register unfinished models in card, relic, or potion pools.

## Localization

`Build-NativeLocalizationCatalog.ps1` converts the complete in-scope model catalog from the STS1 English and Simplified Chinese sources while preserving the behavior-ready subset proven in earlier phases and Phase N5 batches.

Per language it emits:

- 193 card keys for 95 models;
- 82 power keys for 25 player-card-relevant models plus the native Mantra support model;
- 34 relic keys for 11 concrete models;
- 12 potion keys for 6 concrete models;
- 28 custom keyword keys.

That is 349 generated entries per language. Static generation reports zero missing keys, legacy STS1 markers, or language-key mismatches; fresh actual-game formatting evidence is pending in the active N5 regression pass.

The source-difference report inventories four in-scope deferred STS1 localization families: 17 records per language, of which 15 are behavior-linked and two are template scaffolds (`OrbID` and `Example`). Sakiko, behavior UI, and credit text will be emitted only with their owning implementations. Event and monster tables plus alternate-Neow, custom-intent, and custom-act keys are excluded rather than deferred.

## Reproducible assets and gallery

- Source synchronization: `tools/Sync-Sts1PresentationAssets.ps1`
- Parity inventory: `tools/Build-Sts1ParityInventory.ps1`
- Model localization: `tools/Build-NativeLocalizationCatalog.ps1`
- Derived character surfaces: `tools/Build-NativeCharacterPresentationAssets.py`
- PCK allowlist/catalog: `tools/Build-NativePresentationCatalog.ps1`
- Development gallery: `tools/Build-NativePresentationGallery.py`

Refreshed gallery: `artifacts/n4-gallery/gallery-20260907-081256`

The refreshed gallery contains 17 contact sheets for all 137 scoped models, all in-scope presentation source groups, and the 12 derived character surfaces. Its manifest records 347 rendered asset entries with dimensions and SHA-256 hashes; gallery inclusion never enables behavior.

The neutral placeholder was generated as an isolated navy theatrical half-mask with a silver crescent and pale-blue star, then converted to deterministic transparent PNG form. The source, alpha map, transparent master, and exact-size Weakness derivatives remain in `ArtSource/placeholders` and `images/placeholders` for later replacement without changing IDs or paths.

## Acceptance evidence

### Installed-game presentation and localization

- English: `artifacts/n7-release/native-loader-20260907-161706/slay-the-spire-2.log`
- Simplified Chinese: `artifacts/n7-release/native-loader-20260907-161716/slay-the-spire-2.log`

Each isolated installed-game run reported exactly one initializer and one Phase N4 pass marker. Both validated 338 textures, 52 audio resources, 10 standalone resources, 15 localization files, five custom scene contracts, and twelve direct character-texture contracts. Each also validated 345 localization entries and 690 formatted variants. Neither log contains a managed exception, missing-resource result, or localization-formatting error.

### Sakiko, save/reload, and vanilla regression

Artifact root: `artifacts/n5-regression/native-gameplay-20260907-024156`

The fixed-seed Release run passed Sakiko selection, custom static combat portrait creation, card reward generation, Dazzling behavior, exactly one post-victory Hairband addition, reward completion, native save contents, fresh-process custom-model reload, Ironclad selection, and a short vanilla combat. Togawa, reload, and vanilla managed-issue counts were all zero.

Headless dummy-renderer runs can emit `Invalid Task ID` and `Parameter "t" is null` engine lines during asynchronous transition or texture preload. The same signatures occur in isolated vanilla runs and predate Phase N4; they are not managed exceptions or localization errors. They are retained in raw logs rather than hidden.

### Shared-contract preservation

Final accepted artifact root: `artifacts/n7-release/native-n3-contract-20260907-155027`

All 31 pure assertions, nine actual-game marker groups, synchronized command/hook-order probes, native save, and strict reload passed after the final N4 package was installed. Gameplay and reload managed-issue counts were zero.

The earlier artifact `artifacts/n4-regression/n4-character-presentation-regression-20260906-232322` is not acceptance evidence. It exposed a missing energy-label font override; the self-contained scene was corrected and both later clean regressions passed.

## Build, package, and installed artifact

- Release build: zero warnings and zero errors.
- `dotnet format --verify-no-changes`: passed.
- Package: DLL, manifest, PCK, and PDB only.
- Manifest dependencies: zero.
- BaseLib reference in canonical code, project, manifest, or assembly metadata: none.
- PCK size: 28,079,900 bytes.
- Staged and installed SHA-256 values: exact matches.
- Excluded act/event/enemy PCK tokens: zero.

| File | SHA-256 |
| --- | --- |
| `TogawaSakiko.dll` | `E3F02DD5C037BB7F575C53EB4A275CB1DB8B972B967CFC3C83F19D823EEFD936` |
| `TogawaSakiko.json` | `D809D26326FEAC70A5EDC300C41E8BB2616E22B48C16EABFB37872B3C48FDB76` |
| `TogawaSakiko.pck` | `763E9E57FE318C224AA1DF2B0C40A1073A685421659FC44B55FFD805CAD21B12` |
| `TogawaSakiko.pdb` | `09F5798C938EF2DC7CA7E695A493A822B85B60F8DADE5F7FC3624E854BAB0EE4` |

The validated package remains installed at `Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko`.

## Repeatable commands

Run from the repository root:

```powershell
pwsh -NoProfile -File .\tools\Sync-Sts1PresentationAssets.ps1
pwsh -NoProfile -File .\tools\Build-Sts1ParityInventory.ps1
pwsh -NoProfile -File .\tools\Build-NativeLocalizationCatalog.ps1
python .\tools\Build-NativeCharacterPresentationAssets.py
pwsh -NoProfile -File .\tools\Build-NativePresentationCatalog.ps1
python .\tools\Build-NativePresentationGallery.py
dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Release --no-restore -v:minimal
dotnet format .\TogawaSakiko\TogawaSakiko.csproj --verify-no-changes --no-restore --verbosity minimal
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:DeployNativeMod -p:Configuration=Release -p:AllowLocalDeploy=true -nologo
pwsh -NoProfile -File .\tools\Test-NativePackage.ps1 -PackagePath .\TogawaSakiko\artifacts\stage\TogawaSakiko
pwsh -NoProfile -File .\tools\Invoke-NativeN4CatalogTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -Language eng
pwsh -NoProfile -File .\tools\Invoke-NativeN4CatalogTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -Language zhs
pwsh -NoProfile -File .\tools\Invoke-NativeGameplaySmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2'
pwsh -NoProfile -File .\tools\Invoke-NativeN3ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2'
```

## Limitations carried into Phase N5

- At the original N4-to-N5 handoff, 57 cards and 18 scope-corrected in-scope STS1 powers lacked canonical native behavior. Nine source-disabled cards must remain unreachable unless a later explicit decision enables them. Current implementation totals are maintained in the N5 execution record.
- Ten relics and all six potions remain behavior-pending.
- The 52 in-scope audio resources are packaged and loadable but not all are routed to card, character, or VFX behavior yet.
- Custom act, event, enemy, encounter, intent, enemy-compatibility power, ending, cutscene, and exclusive music content is excluded and must remain absent from the package.
- Multiplayer presentation topology exists, but host/client behavior and deterministic content have not been verified.
- Replace-later art is explicit for Weakness and the four multiplayer hands; no generated placeholder is presented as original STS1 art.

Phase N5 starts with behavior dependency analysis and small native-command-only card/power batches. Presentation readiness does not relax the rule that a model enters a normal pool only after base/upgraded behavior and focused actual-game verification pass.
