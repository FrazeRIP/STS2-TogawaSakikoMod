# Togawa Sakiko v0.1.0 installation

Supported game build: Slay the Spire 2 `v0.111.0` (`41cef1ea`).

This release is self-contained. BaseLib is not required and should not be copied into the Togawa folder.

## Install

1. Exit Slay the Spire 2.
2. Extract the archive.
3. Copy the extracted `TogawaSakiko` folder into the game's `mods` directory.
4. Confirm the installed folder contains exactly:
   - `TogawaSakiko.dll`
   - `TogawaSakiko.json`
   - `TogawaSakiko.pck`
5. Start the game with mods enabled and select Togawa Sakiko.

For the recorded Windows installation, the final folder is:

```text
Z:\Steam\steamapps\common\Slay the Spire 2\mods\TogawaSakiko
```

When updating, replace all three files together so the DLL, manifest, and PCK remain from the same release.

## Support boundary

- Singleplayer is supported on the recorded game build.
- Multiplayer is not supported or claimed by v0.1.0.
- The nine cards disabled in STS1 are skipped and cannot enter normal generation.
- Custom acts, events, enemies, encounters, intents, endings, cutscenes, and exclusive music are not part of this port.
