# Phase N1 Native Bootstrap Execution Record

> Current-state review, 2026-09-07: Historical N1 evidence. The no-BaseLib contract remains active; current registration is 139 gameplay models. Later phases and feedback gameplay are implemented. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Status: complete

Executed: 2026-09-06

Game: Slay the Spire 2 `v0.111.0`, commit `41cef1ea`

Engine: MegaDot `4.5.1.m.14.mono.custom_build`

## Delivered checkpoint

- The canonical project compiles only `NativeCode/**/*.cs`. The BaseLib-era source and assets remain in place as migration evidence.
- The project references the game's `sts2.dll` and `0Harmony.dll` directly.
- `Godot.NET.Sdk` is pinned to `4.5.1`; the build-only mod analyzer is pinned to `0.1.9` and remains private.
- BaseLib is absent from canonical package references, generated NuGet assets, DLL assembly references, the manifest dependency list, and the staged package.
- Normal build, code staging, asset import, PCK export, validation, and live deployment are separate targets.
- Live deployment requires the explicit `AllowLocalDeploy=true` property. A blocked deployment exits before rebuilding or exporting.
- The manifest declares no dependencies and requires game version `0.111.0`.
- The one initializer registers Godot scripts from its executing assembly, applies only that assembly's Harmony patches, verifies the game version, verifies the mounted PCK probe, and registers zero gameplay models.
- The PCK exports only the bootstrap resource probe and current mod image plus Godot-generated dependencies.
- MegaDot-generated `.import` and `.uid` source metadata is retained in version control; only the `.godot/` cache is ignored.

## Repeatable commands

Run from the repository root after copying `TogawaSakiko/local.props.example` to the ignored `TogawaSakiko/local.props` and configuring local paths.

```powershell
dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Debug --nologo

dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj `
  -t:StageNativeCode `
  -p:Configuration=Debug `
  -nologo

dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj `
  -t:ImportModAssets `
  -nologo

dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj `
  -t:PackNativeMod `
  -p:Configuration=Debug `
  -nologo

.\tools\Test-NativePackage.ps1 `
  -PackagePath .\TogawaSakiko\artifacts\stage\TogawaSakiko
```

Deployment is intentionally separate:

```powershell
dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj `
  -t:DeployNativeMod `
  -p:Configuration=Debug `
  -p:AllowLocalDeploy=true `
  -nologo
```

With the validated package deployed and unrelated local mods removed, run the loader gate:

```powershell
.\tools\Invoke-NativeLoaderSmokeTest.ps1 `
  -GameRoot 'C:\Path\To\Slay the Spire 2' `
  -ArtifactRoot .\TogawaSakiko\artifacts\test-runtime `
  -RunName native-only
```

The smoke test uses a unique ignored user-data directory, enables mod loading only in that isolated settings file, disables Steam/workshop loading, launches headlessly, and checks the log through the game's `RUNNING MODDED` marker.

## Validation evidence

Code and package:

- Debug build: zero warnings and zero errors.
- Canonical compile items: one file, `NativeCode/Bootstrap/ModEntryPoint.cs`.
- Generated NuGet assets: zero BaseLib matches.
- Staged files: `TogawaSakiko.dll`, `TogawaSakiko.json`, `TogawaSakiko.pck`, and optional development PDB only.
- Manifest dependencies: zero.
- DLL BaseLib assembly references: false.
- PCK size at this checkpoint: 561,252 bytes.
- Godot global script class cache in the PCK source: empty.
- A normal build did not create or update the game's live `mods` directory.
- The deployment opt-in rejection left the staged PCK timestamp unchanged.

Native-only real-game launch:

- Local mod order contained only `Togawa Sakiko (TogawaSakiko)`.
- The game loaded the Togawa DLL and PCK.
- The initializer count was exactly one.
- The bootstrap marker count was exactly one.
- Game baseline and mounted PCK probe checks passed.
- The game reported `RUNNING MODDED` with one loaded mod.
- Pre-startup loader issue count was zero.
- The test reached the main menu and exited without forced termination.
- Evidence log: `TogawaSakiko/artifacts/test-runtime/phase-n1-final-native-only-20260906-151634/slay-the-spire-2.log`.

BaseLib-present but undeclared real-game launch:

- BaseLib `3.4.5` was installed temporarily beside Togawa from the retained package cache.
- Togawa's manifest still declared zero dependencies and its DLL still referenced no BaseLib assembly.
- BaseLib initialized first; Togawa then completed the same version, PCK probe, and native bootstrap checks.
- The game reported `RUNNING MODDED` with two loaded mods.
- Pre-startup loader issue count was zero.
- Evidence log: `TogawaSakiko/artifacts/test-runtime/baselib-present-undeclared-20260906-151411/slay-the-spire-2.log`.

The headless Godot `--quit-after` path emits engine task/resource cleanup messages after successful startup. The loader gate distinguishes these shutdown-only messages from errors before `RUNNING MODDED`.

The temporary live Togawa and BaseLib folders were removed after validation. The game installation's local `mods` directory returned to its original absent state. The ignored staged package and test logs remain available locally.

## Next gate

Phase N2 must preserve this exact no-BaseLib loader/package result while adding stable IDs and the smallest playable native character slice.
