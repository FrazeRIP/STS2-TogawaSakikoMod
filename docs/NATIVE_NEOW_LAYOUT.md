# Native Neow starting room

Revision: 2026-09-07. Target: STS2 v0.111.0 (`41cef1ea`).

Dialogue revision: Sakiko's supplied English and Simplified Chinese conversations now use the native dialogue system. Conversation 1 plays on her first visit, conversation 2 on her second, and conversation 3 on her third. The dedicated Sakiko room skips the shared first-ever introduction so it cannot replace conversation 1. From the fourth visit onward, Neow's native random repeat pool contains the generic conversations plus Sakiko conversations 2 and 3; conversation 1 remains a one-time milestone. Native visit history counts completed runs (wins plus losses), so reloading a run does not advance the conversation.

The spoken text is preserved exactly, with native speaker portraits and localized Continue buttons. `(Playing piano)` / `（演奏钢琴）` is the supplied dialogue stage direction. The three reward options remain fixed and independent of dialogue selection. Existing original-character conversations and Neow's shared introduction for vanilla characters are preserved.

Standard solo Sakiko starts reuse the game's `ancient_event_layout.tscn` and `events/background_scenes/neow.tscn` directly. Neow owns the animated cavern, dialogue and speaker icon, title banner, ambience, option sizing, controller navigation, and Proceed behavior. No game scene or game asset is copied into the mod.

The three choices remain Another Mask, The Third Movement, and Blazing Hairband, in that order for every seed. Their text and reward effects are unchanged. The `TOGAWASAKIKO-OCEAN_OF_MEMORIES` event ID remains stable for saved runs and ancient choice history. The existing standard/solo/Sakiko/first-room boundary is unchanged, including exclusion of real custom-run modifiers.

The old custom scene, controller, option patch, artwork, and narrative are retained in source. The active room uses the native ancient layout, so the custom controller and its click interception do not run. Narrow model-scoped patches route its background, icons, dialogue set, and presentation localization to Neow while reward localization remains in the mod's event table.

## Verification

Repeat-pool follow-up: conversation 3 now repeats after its third-visit milestone, alongside conversation 2 and generic Neow dialogue. Conversation 1's Chinese answer line is now exactly `你..想得到.. 答案... ..在...塔顶....`. Release build and package validation passed; a rendered Chinese visit-1 test and an English visit-3 test passed with zero managed issues, including checks that both conversations 2 and 3 appear in the later repeat pool. The updated Chinese line was visually inspected. Evidence: `artifacts/neow-repeat/neow-zhs-revised-20260907-233326/summary.json` and `artifacts/neow-repeat/neow-eng-repeat-20260907-233355/summary.json`.

Dialogue revision acceptance: Release build and MegaDot package validation passed. All six rendered tests (visits 1, 2, and 3 in both English and Simplified Chinese) passed with zero managed issues. Each run verifies the milestone/repeat selection rules, exact speaker order, localized speech and Continue keys, real native Continue/reward clicks, and the existing relic/deck/history/save-reload contracts. Each conversation line was captured, and representative frames from all three conversations were visually inspected in both languages. The catalogs contain 17 matching new keys per language: 10 speech lines and 7 Continue labels. Consolidated evidence: `artifacts/neow-dialogue/summary.json`; tested package identity: `artifacts/neow-dialogue/package-hashes.json`.

The original layout-only acceptance follows as historical evidence:

- Release build: passed with zero warnings and errors.
- MegaDot import/export and native package validation: passed; no BaseLib dependency or excluded content.
- All three English rendered starting-choice tests passed with zero managed issues: fixed choices, native scene identity, native dialogue availability on first/repeat visits, exact relic/deck changes, duplicate-choice prevention, saved choice history, finished-event reload, and vanilla Neow boundary checks. Reward generation was checked against multiple event seeds.
- English evidence: `artifacts/neow-native/native-neow-eng-20260907-230916/summary.json`; per-choice directories contain the rendered room and game log.
- Simplified Chinese rendered test (Another Mask): passed with zero managed issues; all three visible options and native Neow dialogue were inspected in both languages. Evidence: `artifacts/neow-native/native-neow-zhs-20260907-231038/summary.json`.
- The first test attempt exposed a diagnostic timing assumption: autoslay reached the room while the native animated background was still preloading. The diagnostic now waits for the mutable event and native options; the clean rerun above is the acceptance result.

Tests use `artifacts/neow-native/game` with only TogawaSakiko and isolated save/settings profiles. The staged package is under `TogawaSakiko/artifacts/stage/TogawaSakiko`. The live Steam mod folder is unchanged.
