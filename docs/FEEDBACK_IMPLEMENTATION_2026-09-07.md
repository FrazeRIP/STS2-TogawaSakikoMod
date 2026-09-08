# Sakiko feedback implementation — 2026-09-07

> Current-state review, 2026-09-07: This feedback implementation is included in the authorized publication checkpoint; the no-commit sentence below describes the original session only. Generated mask, masked combat, campfire and merchant artwork are placeholders. The cavern image is supplied source art. All generated assets remain placeholders, not completed assets. See [current project state](CURRENT_STATE.md). This update supersedes conflicting current-state claims below; original records are retained as history.

Implemented in the native mod and installed into the local Slay the Spire 2 mods directory. No commit or push was made. The supplied cavern image is copied unchanged; its SHA-256 matches the source.

| Request | Result |
| --- | --- |
| Oversized moon effect when cards change zones | Reduced the custom card-trail sprite and particle sizes. This is the character's card movement trail. |
| Missing starting options | Fixed the internal Kings modifier interference and added the custom Ocean of Memories starting room for standard solo Sakiko runs. |
| Starting artwork and narrative | Uses the supplied Neow cavern image. Sakiko wakes somewhere unfamiliar; Neow offers a gift and directs her toward the tower's summit for answers. |
| Fixed starting choices | Another Mask, The Third Movement, and Blazing Hairband. The latter two retain their ported effects. |
| Another Mask | Replaces Monochrome Hairband and transforms the four starting Defends into Desire. Saved one-time guard prevents repeat conversion. Generated mask relic icons and masked combat sprite are included. |
| Small relic art | Normalized visible bounds of all 11 original relics and their outline/large variants. Transparent padding was responsible for their inconsistent apparent size. |
| Hairband animation | Added-card preview appears below the relics near the top left. Hold time increased to 0.8 seconds before moving to the deck. |
| Campfire and shop art | Replaced both sprites with generated painterly full figures. Campfire uses an elevated, seated three-quarter pose facing the fire, adjusted against the supplied reference and actual room rendering. |
| Card energy and frames | Uses the original STS1 crescent energy art for card costs and inline energy. Original piano frame art is adapted to native card geometry; curses use native Curse frames and materials. |
| Chinese spacing and duplicate keyword clauses | Generator removes unwanted spaces around dynamic values and clauses duplicated by native automatic keywords. Both languages pass all card-variant formatting checks. |
| Upgrade green text | Only changed fragments and values are green. Ave Mujica/Crychic highlight Element+/Phantoms+ without coloring their whole descriptions; Memento Mori/Alea and similar cards use the same corrected generator. |
| Missing tooltips and previews | Added Remove/Purge and Scry explanations. Element/Phantom previews cycle through all five cards once per second, including matching upgraded variants. Card-removal tips are not attached to effects that only remove powers. |
| Card-reference colors, Strength, names | Entire card names are highlighted together. Strength is gold. Memento Mori has its missing space. Double Tap references use One-Two Punch / 连环拳. |
| Newly gained Dazzling | A gain only triggers stacks that existed before that gain. Zero to one causes no attack; one to two triggers one stack. |
| Wish to Become Human | Retains one native two-hit attack, with a separate Dazzling application after each damaging hit. The diagnostic verifies gains of 2 then 2, with two prior stacks triggering on the second gain. |
| Kao | Glows for an enemy Buff intent. Combat growth is a damage modifier, so the native preview colors the increased number green. It notifies the hand after growth to prevent stale numbers. |
| Master of Melodia | Player/canonical Divinity presentation is renamed. The existing horned combat sprite takes precedence while active, then restores the masked or normal sprite with the same ground pivot. Enemy Divinity retains its existing presentation. |
| Memento Mori speed | Fast/Instant mode skips the serial cosmetic exhaust/removal previews while preserving sequential gameplay hooks and deck mutation. Normal mode retains its animations. |
| Extra turns | Native Ambergris already stacks as a counter. Verified two queued turns through two real consecutive transitions, consuming one stack each. No speculative global turn patch was added. |
| Ave Mujica → Symbol I: Fire | Fire now includes attacks that have started but have not finished resolving. Choosing Fire replays a copy of the active Ave Mujica and opens its second choice. |
| Oblivionis warning | Affected attacks and the source curse glow red in hand. The curse flashes when its effect sends an attack to Exhaust. |
| Worldview | Replacement attacks come from the current owner's character card pool. |
| Unlocks and card library | Sakiko cards/relics are automatically available for inspection. Added the native Sakiko library filter; rendered library shows all 88 cards. Fixed starting relics also appear in the relic collection. Vanilla progression is unchanged. |

## Verification

- Build and package: 0 warnings, 0 errors; package validator passed, no BaseLib dependency or excluded legacy content. 139 registered models; 345 textures, 52 audio assets, 11 standalone resources, 17 localization files.
- EN catalog: `artifacts/feedback-tests/feedback-final-catalog-eng-20260907-211205`.
- zh-Hans catalog: `artifacts/feedback-tests/feedback-final-catalog-zhs-20260907-211705`. Each language checks 361 entries, 722 formatted variants, and 176 card variants without duplicated keyword clauses.
- All three starting choices, repeated callbacks, native history, save and reload: `artifacts/feedback-tests/feedback-starting-choices-20260907-210825`.
- Gameplay: `artifacts/feedback-tests/feedback-final-n5-20260907-211821`. 725 pure assertions and all runtime batches passed, including the new feedback cases and two consecutive extra turns. Zero managed issues.
- Rendered EN/Another Mask: `artifacts/feedback-tests/feedback-final-render-eng-20260907-211707/ANOTHER_MASK`.
- Rendered zh-Hans/Blazing Hairband: `artifacts/feedback-tests/feedback-final-render-zhs-20260907-211952/BLAZING_HAIRBAND`.
- Both rendered runs verify actual sprite switching/restoration, Kao's live preview refresh, red warnings, timed Element rotation, selected library filter, and presence of the starting-relic collection subsection. Both passed with zero managed issues.
- Installed DLL/PDB/manifest/PCK match staged SHA-256 hashes: `artifacts/feedback-installed-hashes.json`. Previous installed package retained under `artifacts/feedback-installed-backup-20260907-210755`.

All runtime checks used isolated test profiles. Native engine startup `Invalid Task ID` messages occurred in some runs; they are recorded in the logs and are separate from the managed-error checks. The art is a static painterly adaptation, not native skeletal animation. The custom starting event remains limited to standard single-player Sakiko starts; vanilla characters and multiplayer starting rooms are preserved.
