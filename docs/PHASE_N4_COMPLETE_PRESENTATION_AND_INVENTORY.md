# Phase N4 Complete Presentation and Inventory

Status: complete

Executed: 2026-09-06

Starting point: branch `Test1`, commit `3a35047`

Game baseline: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Engine baseline: MegaDot `4.5.1.m.14.mono.custom_build`

This is the execution record for Phase N4 of `NATIVE_FIRST_PORT_WORKFLOW.md`. It preserves the Phase N1 native-only package, the Phase N2 playable slice, and the Phase N3 shared contracts while completing presentation and source-truth inventories without making unfinished content reachable in normal play.

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
| Cards | 95 | 7 | 88 | 95 exact 250x190/500x380 pairs |
| Powers | 36 | 2 | 34 | 34 custom 32x32/84x84 pairs and two explicit base-game reuses |
| Relics | 11 | 1 | 10 | 11 icon/outline/large sets |
| Potions | 6 | 0 | 6 | 6 complete native layer sets |

Nine source-disabled cards remain disabled. `WeaknessCard` was the only concrete source model with missing STS1 art; its deterministic replace-later pair uses the generated placeholder master at the exact native card dimensions. `PlayerFilightPower` and `MonsterVigorPower` intentionally reuse STS2 Flight and Vigor presentation because their STS1 classes inherit those base powers.

The PCK catalog validates 373 package textures, 58 playable audio resources, 10 standalone resources, and 15 live localization files. Copying or packaging presentation does not register unfinished models in card, relic, potion, encounter, event, or enemy pools.

## Localization

`Build-NativeLocalizationCatalog.ps1` converts the complete model catalog from the STS1 English and Simplified Chinese sources while preserving the seven live cards, two live powers, and starter relic entries already proven in earlier phases.

Per language it emits:

- 190 card keys for 95 models;
- 102 power keys for 34 custom-presented models;
- 34 relic keys for 11 concrete models;
- 12 potion keys for 6 concrete models;
- 28 custom keyword keys.

That is 366 validated entries per language. The actual-game diagnostic resolves and formats 732 base/upgraded or singular/plural variants with zero missing keys, legacy STS1 markers, language-key mismatches, or localization formatting exceptions.

The source-difference report also inventories six deferred STS1 localization families: 33 records per language, of which 30 are behavior-linked and three are template scaffolds (`EventID`, `OrbID`, and `Example`). Character/alternate-Neow, event, monster/act, behavior UI, and credit text will be emitted only with their owning implementations, preventing misleading dead localization from being treated as completed behavior.

## Reproducible assets and gallery

- Source synchronization: `tools/Sync-Sts1PresentationAssets.ps1`
- Parity inventory: `tools/Build-Sts1ParityInventory.ps1`
- Model localization: `tools/Build-NativeLocalizationCatalog.ps1`
- Derived character surfaces: `tools/Build-NativeCharacterPresentationAssets.py`
- PCK allowlist/catalog: `tools/Build-NativePresentationCatalog.ps1`
- Development gallery: `tools/Build-NativePresentationGallery.py`

Final gallery: `artifacts/n4-gallery/gallery-20260906-232854`

The gallery contains 18 contact sheets for all 148 planned models, all presentation source groups, and the 12 derived character surfaces. Its manifest records 386 rendered asset entries with dimensions and SHA-256 hashes. The total intentionally includes two recovered base-game presentation references and repeated character-focused audit views; gallery inclusion never enables behavior.

The neutral placeholder was generated as an isolated navy theatrical half-mask with a silver crescent and pale-blue star, then converted to deterministic transparent PNG form. The source, alpha map, transparent master, and exact-size Weakness derivatives remain in `ArtSource/placeholders` and `images/placeholders` for later replacement without changing IDs or paths.

## Acceptance evidence

### Installed-game presentation and localization

- English: `artifacts/n4-presentation/n4-final-catalog-eng-20260906-2331-eng-20260906-233140/slay-the-spire-2.log`
- Simplified Chinese: `artifacts/n4-presentation/n4-final-catalog-zhs-20260906-2331-zhs-20260906-233142/slay-the-spire-2.log`

Each isolated installed-game run reported exactly one initializer and one Phase N4 pass marker. Both validated 373 textures, 58 audio resources, 10 standalone resources, 15 localization files, five custom scene contracts, and eleven direct character-texture contracts. Neither log contains a managed exception, missing-resource result, or localization-formatting error.

### Sakiko, save/reload, and vanilla regression

Artifact root: `artifacts/n4-regression/n4-final-regression-20260906-2333-20260906-233158`

The fixed-seed Release run passed Sakiko selection, custom static combat portrait creation, card reward generation, Dazzling behavior, exactly one post-victory Hairband addition, reward completion, native save contents, fresh-process custom-model reload, Ironclad selection, and a short vanilla combat. Togawa, reload, and vanilla managed-issue counts were all zero.

Headless dummy-renderer runs can emit `Invalid Task ID` and `Parameter "t" is null` engine lines during asynchronous transition or texture preload. The same signatures occur in isolated vanilla runs and predate Phase N4; they are not managed exceptions or localization errors. They are retained in raw logs rather than hidden.

### Shared-contract preservation

Artifact root: `artifacts/n3-contract/n3-post-n4-contract-20260906-2335-20260906-233247`

All 31 pure assertions, nine actual-game marker groups, synchronized command/hook-order probes, native save, and strict reload passed after the final N4 package was installed. Gameplay and reload managed-issue counts were zero.

The earlier artifact `artifacts/n4-regression/n4-character-presentation-regression-20260906-232322` is not acceptance evidence. It exposed a missing energy-label font override; the self-contained scene was corrected and both later clean regressions passed.

## Build, package, and installed artifact

- Release build: zero warnings and zero errors.
- `dotnet format --verify-no-changes`: passed.
- Package: DLL, manifest, PCK, and PDB only.
- Manifest dependencies: zero.
- BaseLib reference in canonical code, project, manifest, or assembly metadata: none.
- PCK size: 40,592,544 bytes.
- Staged and installed SHA-256 values: exact matches.

| File | SHA-256 |
| --- | --- |
| `TogawaSakiko.dll` | `7F79BF6E931F2D100E0D0DBC77D86FE16394F23B36EF596F37698F1655AAA7B2` |
| `TogawaSakiko.json` | `E66967066D1F3F8DCB325B7C3EDDBFCAB0DE569F6AA45721373ABD54595E071F` |
| `TogawaSakiko.pck` | `0E92014F036DB289FD70284A48AF90544E3A000AA4FC91EF80C5DC3682564FE3` |
| `TogawaSakiko.pdb` | `B8CF1AE168F0CFD0A813955E7FA30B52529F7718520510CD8AF444195DE0FB3E` |

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

- 88 cards and 34 powers still lack canonical native behavior. Nine source-disabled cards must remain unreachable unless a later explicit decision enables them.
- Ten relics and all six potions remain behavior-pending.
- The 58 audio resources are packaged and loadable but not yet routed to card, character, VFX, enemy, act, or ending behavior.
- The custom act, events, enemies, intents, rewards, cutscene, and ending remain disabled.
- Multiplayer presentation topology exists, but host/client behavior and deterministic content have not been verified.
- Replace-later art is explicit for Weakness and the four multiplayer hands; no generated placeholder is presented as original STS1 art.

Phase N5 starts with behavior dependency analysis and small native-command-only card/power batches. Presentation readiness does not relax the rule that a model enters a normal pool only after base/upgraded behavior and focused actual-game verification pass.
