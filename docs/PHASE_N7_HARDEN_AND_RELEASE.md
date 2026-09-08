# Phase N7: Harden and release

> Current-state review, 2026-09-07: Historical N7 archive and acceptance evidence follow. Later feedback has different package hashes and adds Another Mask, Ocean of Memories and gameplay/presentation/localization fixes. Source publication is now authorized. Earlier waiting/uncommitted statements are historical. Full-run N7 acceptance was not repeated for the feedback package. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Status: complete for the declared singleplayer v0.1.0 support boundary.

Target: Slay the Spire 2 `v0.111.0` (`41cef1ea`) on branch `Test1`.

## Release decision

The native port is released as `v0.1.0`. It contains 86 enabled cards, 25 player-relevant powers, 11 relics, six potions, the native character/presentation slice, active STS1 sound and voice routes, and all shared behavior contracts. The nine cards disabled in STS1 are skipped. Custom acts, events, enemies, encounters, intents, endings, cutscenes, and exclusive music remain removed.

Singleplayer is supported on the recorded build. Multiplayer is explicitly unsupported and unverified, so no host/client support claim or conditional multiplayer matrix applies to v0.1.0.

## Hardening decisions

- Final manifest and bootstrap version: `v0.1.0`; assembly informational version: `0.1.0-native-full-port`.
- The final package is native and self-contained: no BaseLib manifest dependency, assembly reference, API, or runtime folder is required.
- Progression compatibility is limited to Sakiko's exact pool/character path and only bypasses built-in epoch bookkeeping that cannot represent a mod epoch.
- Architect-finale compatibility is limited to Sakiko and delegates victory to `RunManager.Instance.WinRun()` when the current base dialogue table has no custom-character entry.
- Dazzling's STS1 visual/audio path is restored through a target-owned 0.6-second Godot node. Headless diagnostics validate the factory, texture, duration, and command ordering; the focused gameplay regression confirms the Dazzling trigger.
- The full-run harness rejects every disabled-card ID in captured saves, records six lifecycle snapshots, reloads each in a fresh process, verifies victory persistence, and confirms `current_run.save` is removed after victory.
- Kings pending reward state is carried by a hidden native run modifier keyed by player Net ID. The dedicated edge-case run proves persistence with no starter relic and no persistent Kings card, then selects a real native reward after fresh-process reload and observes the native deck add plus `AfterRewardTaken` clear.
- The final-profile diagnostic uses a bounded 1,800-frame clean exit after main-menu initialization. Godot's known headless shutdown RID/task cleanup messages are engine-level diagnostics; all acceptance scans remain strict for managed exceptions and mod/localization failures.

## Acceptance evidence

| Gate | Result | Artifact |
| --- | --- | --- |
| Working-tree release build | 0 warnings, 0 errors | `TogawaSakiko/.godot/mono/temp/bin/Release/TogawaSakiko.dll` |
| Isolated source restore/build/pack | Restore passed; build 0 warnings, 0 errors; package validation passed | `artifacts/n7-clean-source/native-v0.1.0-20260907-161507` |
| English loader | One initializer; N3/N5/N6/N7 pure suites passed | `artifacts/n7-release/native-loader-20260907-161706` |
| Simplified Chinese loader | One initializer; N3/N5/N6/N7 pure suites passed | `artifacts/n7-release/native-loader-20260907-161716` |
| N3 shared contracts | 31 pure assertions; nine actual-game markers; save reload passed; zero managed issues | `artifacts/n7-release/native-n3-contract-20260907-155027` |
| N5 cards/powers | 725 assertions; common/uncommon/rare/token/curse lifecycle markers passed; zero managed issues | `artifacts/n7-release/native-n5-batch-20260907-155052` |
| Kings no-card/no-relic carrier | 725 pure assertions; eight gameplay markers; native save, fresh-process reload, reward selection, deck add, and clear passed against the final clean package; zero managed issues | `artifacts/n7-release/native-v010-kings-final-clean-20260907-162304` |
| N6 relics/potions | 73 assertions; 15 actual-game markers; 11 relics; six potions; save reload passed; zero managed issues | `artifacts/n7-release/native-n6-contract-20260907-155227` |
| Sakiko and vanilla regression | Sakiko combat/reward/save/quit/reload plus Ironclad combat passed; Dazzling and one Hairband reward observed; zero managed issues | `artifacts/n7-release/native-gameplay-20260907-155247` |
| Natural full run | Victory; Monster/Elite/Boss/Shop/Rest Site covered; zero disabled cards; zero managed issues | `artifacts/n7-release/native-n7-full-run-20260907-155333` |
| Save/reload matrix | New run, normal combat reward, shop, rest site, boss reward, and act transition each reloaded in a fresh process; final completed profile also reloaded | `artifacts/n7-release/native-n7-full-run-20260907-155333/save-matrix` |
| No-BaseLib package | Manifest dependencies 0; BaseLib assembly reference false; excluded assembly/PCK tokens 0 | `Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko` |
| Companion-mod isolation | BaseLib 3.4.5 and Togawa initialized together; BaseLib applied 280 patches with 0 failed; all Togawa pure suites passed | `artifacts/n7-unrelated/oddmelt-20260907-135413/togawa-baselib-coexistence-20260907-135736` |
| Unrelated failure attribution | Clean Oddmelt snapshot failed before deployment on four obsolete override signatures; no Togawa source or runtime participated | `artifacts/n7-unrelated/oddmelt-20260907-135413/build-result.txt` |
| Final aggregate audit | Zero failures; clean build/format; installed and archive package validation; exact clean-stage hashes; 20 accepted logs across eight final artifact roots with zero managed/mod/localization issues | `artifacts/n7-release/final-audit-20260907-163230` |
| Release archive | One three-file self-contained mod folder plus installation instructions; package validator passed | `artifacts/release/TogawaSakiko-v0.1.0.zip` |

The full-run `summary.json` is the authoritative compact result for victory, room coverage, save stages, disabled-card count, managed-issue count, and final persistence.

## Package identity

The clean-snapshot staged package contains:

| File | SHA-256 |
| --- | --- |
| `TogawaSakiko.dll` | `E3F02DD5C037BB7F575C53EB4A275CB1DB8B972B967CFC3C83F19D823EEFD936` |
| `TogawaSakiko.json` | `D809D26326FEAC70A5EDC300C41E8BB2616E22B48C16EABFB37872B3C48FDB76` |
| `TogawaSakiko.pck` | `763E9E57FE318C224AA1DF2B0C40A1073A685421659FC44B55FFD805CAD21B12` |
| `TogawaSakiko.pdb` | `09F5798C938EF2DC7CA7E695A493A822B85B60F8DADE5F7FC3624E854BAB0EE4` |

The release archive omits the PDB. The installed validation copy retains the matching PDB for local diagnostics.

Release archive SHA-256: `1874114E544E55CED870B55BAA5DC2CC855550FAD7F08C38370BCA5C3C2EA3D0`.

## Commands

```powershell
& .\tools\Build-Sts1ParityInventory.ps1 -Sts1Root 'X:\Projects\SlayTheSpire2\0REPO\STS1-TogawaSakikoMod'
dotnet restore .\TogawaSakiko\TogawaSakiko.csproj
dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Release --no-restore
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:PackNativeMod -p:Configuration=Release
& .\tools\Test-NativePackage.ps1 -PackagePath 'Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko'
& .\tools\Invoke-NativeLoaderSmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release -Language eng -RequiredMarker 'Phase N7 audio pure contract tests passed'
& .\tools\Invoke-NativeLoaderSmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release -Language zhs -RequiredMarker 'Phase N7 audio pure contract tests passed'
& .\tools\Invoke-NativeN3ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release
& .\tools\Invoke-NativeN5BatchTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release
& .\tools\Invoke-NativeN6ContractTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release
& .\tools\Invoke-NativeGameplaySmokeTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release
& .\tools\Invoke-NativeN7FullRunTest.ps1 -GameRoot 'Z:\Steam\steamapps\common\Slay the Spire 2' -ArtifactRoot .\artifacts\n7-release -Seed N2SAKIKO001
```

## Final limitations and handoff

- Multiplayer is unsupported and unverified.
- Character combat/rest/merchant presentation is static because STS1 defines no animation controller.
- Four multiplayer hand images and the compatibility-only disabled `Weakness` portraits are replace-later placeholders at native dimensions.
- `WishFulfilled.wav` is missing from the STS1 source; no substitute was invented.
- `MonsterDivinityPower` omits continuous ambient particles/aura while preserving gameplay behavior.
- Multiplayer remains future work; its authority, synchronization, reconnect, replay, presentation, and acceptance requirements are recorded in `MULTIPLAYER_PORT_REQUIREMENTS_AND_TODO.md`.
- A clean Git checkout cannot contain the current uncommitted port until the user authorizes a checkpoint commit. The acceptance build therefore used a fresh isolated source snapshot with `.git`, `.claude`, prior `.godot`, `bin`, `obj`, and `artifacts` excluded, followed by a real restore/build/import/export. This preserves the no-commit instruction while proving the package does not rely on stale build state.

The validated snapshot-built package is installed at `Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko`; it is the only live mod folder.
