# Current project state

Two-stage Neow update, 2026-09-10: standard Sakiko starts in solo and multiplayer now offer Another Mask, The Blazing Hairband, and a speech-bubble option to choose normal blessings. The third option opens the game's three native generated rewards with no Back option. The Third Movement is temporarily absent from starting choices and the relic collection; its stable ID and saved gameplay behavior remain supported. This supersedes the fixed three-relic descriptions below. See [two-stage implementation and verification](NEOW_TWO_STAGE.md).

Multiplayer branch update, 2026-09-08: multiplayer adaptations and two explicit test cards are implemented on `codex/multiplayer-adaptations`, package `0.2.0-multiplayer.dev`. Six native ENet scenarios and 14 replay perspectives passed; single-player regression includes full AutoSlay victory and six save/reload checkpoints. This remains a development branch with Steam/WAN, live reconnect, and broad release acceptance limits recorded in [multiplayer implementation and fresh evidence](MULTIPLAYER_IMPLEMENTATION.md). The v0.1.0 single-player records below retain their original package identity and acceptance scope.

Reviewed: 2026-09-07. This document supersedes current-state claims in earlier plans and phase records; those records retain their original implementation and acceptance history.

Neow dialogue revision, 2026-09-07: Sakiko's supplied EN/zh-Hans conversations play on visits 1, 2, and 3 respectively. Later visits use the native repeat pool (generic Neow dialogue plus conversations 2 and 3). Her dedicated room uses conversation 1 instead of the shared first-ever introduction; vanilla character dialogue is preserved. The three fixed rewards and their effects remain unchanged.

Starting-room presentation revision, 2026-09-07: standard solo Sakiko now reuses the game's original Neow ancient layout and animated background, including native dialogue, title, ambience, icons, and button behavior. The options remain fixed in order: Another Mask, The Third Movement, Blazing Hairband. The existing Ocean of Memories model ID and reward/save behavior are retained for compatibility. The custom full-screen scene, artwork, and narrative below describe the earlier presentation and are preserved as source assets but are no longer selected by the starting room. See [native Neow layout verification](NATIVE_NEOW_LAYOUT.md).

## Supported checkpoint

The native v0.1.0 single-player port has completed gameplay implementation for Phases N1–N7 and the September 7 feedback implementation. All generated assets remain placeholders; final artwork is unfinished. Target game: Slay the Spire 2 v0.111.0 (`41cef1ea`); engine: MegaDot `4.5.1.m.14.mono.custom_build`; C#: .NET 9. Multiplayer remains unsupported and unverified.

| Content | Current state |
| --- | --- |
| Cards | 86 enabled STS1 cards; 88 registered/library-visible models including Carefree and Weakness compatibility models. All nine STS1-disabled cards remain excluded from normal generation. |
| Powers | 25 STS1 player-relevant powers plus native Melodia support. |
| Relics | 11 STS1 relics plus the approved Another Mask relic: 12 total. |
| Potions | Six native potions. |
| Registration | 139 gameplay models in NativeModelCatalog. |
| Localization | English and Simplified Chinese; 17 packaged localization files. Source-inventory card descriptions cover 95 models and do not imply 95 playable cards. |
| Presentation | 345 textures, 52 audio assets, 11 standalone resources in the recorded feedback package. All generated art remains placeholder art, including rest/shop/mask and compatibility assets; rendering checks do not certify finished assets. |

The approved Ocean of Memories starting event is the sole exception to the custom-event exclusion. It applies to standard solo Sakiko starts, uses the supplied Neow cavern artwork, and preserves native Neow history/save lifecycle. Choices are Another Mask, The Third Movement, and Blazing Hairband. Another Mask replaces Monochrome Hairband and converts the four starting Defends into Desire once, with a saved guard. It enables the masked combat sprite; Master of Melodia takes precedence and restores the appropriate sprite afterward. The earlier Amnesia option is superseded.

Legacy custom acts, other events, enemies, encounters, intents, endings, cutscenes, exclusive music, and enemy-only compatibility powers remain excluded. The source-derived parity inventory measures STS1 scope; Another Mask and the opening event are native additions outside its original totals.

The support mechanic is now Melodia / 旋律, including `MelodiaPower` and `TOGAWASAKIKO-MELODIA_POWER`. This is a full identifier rename without a legacy save alias. The 10-stack threshold and player/enemy stance behavior are unchanged. Historical STS1 source names and quoted localization remain in the parity inventory; the localization generator translates their terminology into the native name.

Melodia validation: Release build and native package validation passed; English and Chinese N4 catalog checks passed. N5 gameplay diagnostics passed 725 pure assertions and all runtime markers with zero managed issues, including threshold consumption, Voice applications, Inner Cry gains, and player/enemy stance transitions. Evidence: `artifacts/melodia/melodia-n5-20260907-214820/summary.json`.

## Implementation and documentation authority

- Only `TogawaSakiko/NativeCode/**/*.cs` is compiled. `TogawaSakiko/TogawaSakikoCode` is preserved migration reference.
- Runtime dependencies are supplied by the game: sts2, Harmony, and Godot. No BaseLib mod, manifest dependency, or assembly reference is required.
- `NativeModelCatalog` owns registration, `NativeStableIds` owns model identity, and `NativeAssetPaths` plus presentation patches/factories own native resource routing.
- Canonical packaged resources live under `TogawaSakiko/TogawaSakiko`; feedback source art is retained under `TogawaSakiko/ArtSources/feedback` and excluded from the PCK.
- [Native workflow](NATIVE_FIRST_PORT_WORKFLOW.md) governs development. [Feedback implementation](FEEDBACK_IMPLEMENTATION_2026-09-07.md) records current behavior and detailed acceptance artifacts. N1–N7 files retain phase-specific evidence. Older structure/card/BaseLib guides are historical references.
- The user authorized documenting, committing, pushing, and merging this checkpoint into `main`, and removing Claude files. Earlier instructions to wait for a checkpoint phrase no longer apply to this publication.

## Feedback changes included

The checkpoint includes corrected Dazzling prior-stack triggers and Wish to Become Human per-hit gains; Kao intent glow and live damage refresh; Ave Mujica/Fire replay of an active attack; Oblivionis red exhaust warnings; owner-pool Worldview generation; Fast/Instant Memento Mori cosmetic skipping; native stacked extra-turn verification; hairband card previews; card-library/starting-relic visibility; native piano frames and crescent energy art; resized relic icons and card trails; new rest/shop art; and EN/zh-Hans spacing, keyword, upgrade-color, tooltip, and cycling-preview corrections.

## Verification and package identity

During this documentation checkpoint, the Release build was rerun successfully with zero warnings/errors. Existing feedback acceptance summaries were read from disk: 725 N5 assertions, all runtime batches, two consecutive extra turns, all three starting choices, and both EN/zh-Hans rendered runs passed with zero managed issues. These runtime runs were performed before this documentation refresh; they were not rerun as part of editing prose. Detailed paths and catalog checks are in the feedback record.

The original N7 archive and hashes describe the earlier N7 baseline. They must not be used as the identity of the later feedback package, even though the manifest version remains v0.1.0. No new hosted release or release archive is implied by this source merge.

Recorded feedback package SHA-256 values:

| File | SHA-256 |
| --- | --- |
| TogawaSakiko.dll | `89918031C6DD19BDEBBB54DD5F6D91BF29684F45FBC4A6231D6FFAEF154CC631` |
| TogawaSakiko.pdb | `B23B238EF472DB33C3656354492755F418924B58007DB3E1B61977EA0C11B008` |
| TogawaSakiko.json | `D809D26326FEAC70A5EDC300C41E8BB2616E22B48C16EABFB37872B3C48FDB76` |
| TogawaSakiko.pck | `6544823FD2F1B53D46A197E126794EED22CCFB121A02335C5CF7D87F2A6D88C9` |

Build with `dotnet build .\TogawaSakiko\TogawaSakiko.csproj -c Release --no-restore`. Package with `dotnet msbuild .\TogawaSakiko\TogawaSakiko.csproj -t:PackNativeMod -p:Configuration=Release`, then run `tools/Test-NativePackage.ps1 -PackagePath` against that stage. Deployment requires the explicit `AllowLocalDeploy=true` opt-in.

Remaining limitations: multiplayer is future work; four multiplayer hand images and disabled Weakness portraits remain placeholders; WishFulfilled.wav is absent from STS1; MonsterDivinity continuous ambient particles/aura remain omitted. The full-run N7 evidence predates the feedback changes; focused feedback coverage does not constitute a second full-run acceptance.

## Publication verification refresh

Fresh Release build, MegaDot import/export, and staged package validation passed during this checkpoint. The validator reports zero dependencies, no BaseLib assembly reference, and zero excluded assembly/PCK tokens. This fresh package was not deployed or rerun through the gameplay suite.

The pre-refresh installed package matched the then-staged package, but its PCK hash was F02C33A2DB8DE5C503C8417971D56F0EA36E0C4EBBF3C4066114C980332574BB, differing from the earlier feedback hash record above. The fresh Release DLL also differs from the earlier accepted DLL. Therefore earlier runtime evidence is historical coverage, not hash-bound acceptance of this freshly rebuilt package.

Fresh staged package identity:

| File | SHA-256 |
| --- | --- |
| TogawaSakiko.dll | `04551EF952CC7CB31165E5B22CDAEE577F36882E1FD8376AE4AE077935D4F040` |
| TogawaSakiko.json | `D809D26326FEAC70A5EDC300C41E8BB2616E22B48C16EABFB37872B3C48FDB76` |
| TogawaSakiko.pck | `F02C33A2DB8DE5C503C8417971D56F0EA36E0C4EBBF3C4066114C980332574BB` |
| TogawaSakiko.pdb | `C5ED05C6A6D5B27A9E080CF38338DDA1E437FE61816C23CE6F5417D90DBDC181` |
