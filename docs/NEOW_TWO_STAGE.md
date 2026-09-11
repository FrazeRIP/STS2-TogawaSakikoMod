# Two-stage Neow rewards

Implemented 2026-09-10 for STS2 v0.111.0 / `41cef1ea`, package version 0.1.1.

## Player behavior

Standard eligible Sakiko starts offer Another Mask, The Blazing Hairband, then **Those are not what I'm looking for.** / **Choose a normal blessing instead.** The corresponding Chinese text is **这些不是我想要的。** / **改为选择普通祝福。** The two relic choices retain their existing text and effects.

The third option grants nothing and opens exactly three native Neow blessings. There is no Back option. Choices are generated once per event instance using the game's generator, owner eligibility, and existing event RNG. The solo event retains its custom ID; multiplayer retains the native Neow room and independent choices for each player. Vanilla characters and real custom-run modifiers retain their previous behavior.

The Third Movement is temporarily unavailable from this event and hidden from collection entries and navigation. It remains registered with the same stable ID, localization, assets, saved counter, and damage effect so existing inventories remain valid.

## Implementation

`SakikoStartingRewards` shares custom choices, owner-scoped stage state, native-generation caching, and duplicate-grant guards. Solo calls the base generator explicitly; multiplayer bypasses its replacement prefix only while generating that instance's native options. Native reward callbacks remain responsible for pickup effects and completion.

Stage changes use the native event state notification. Ancient history receives the actual reward options, excluding the navigation button. Completion and saving use the existing native lifecycle, without a new checkpoint or save schema. Reloading an unfinished event follows the game's normal restart behavior; generating again from the same state reproduces the choices.

The navigation button uses a 128 x 128 transparent SVG speech bubble through a button-specific icon patch. Native blessing localization is resolved through Neow's `ancients` entries even for the solo subclass. Catalog enumeration does not generate random rewards.

The presentation catalog generator now understands SVG dimensions and preserves the four existing native party-card portrait exports. Its recorded manifest-image dimensions also reflect the current source image. The multiplayer diagnostic's card-cost expectations were corrected to the current v0.1.1 definitions and upgraded-cost API; card behavior was not changed.

## Verification

Focused evidence lives under `artifacts/neow-two-stage`, using a separate test installation and isolated profiles. No Steam mod deployment is part of this change.

The starting-room harness accepts `-Choice 0/1/2`, `-NormalChoice 0/1/2`, `-Seed`, `-Language eng/zhs`, `-CaptureScreenshot`, and `-VerifyCollection`. Tests check native generation and RNG consumption at four seeds, custom reward effects, no reward on navigation, the three native positions, exact completion history, duplicate protection, and finished-save reconstruction. The normal third option for `OCEANSTART001` is Hefty Tablet and exercises a native card selection.

Final acceptance results and package hashes are recorded in `artifacts/neow-two-stage/acceptance.json`.

Accepted results: eight solo runs passed with zero managed issues, covering both custom rewards, all three native reward positions, English/Chinese rendered stages, collection filtering, and legacy-save compatibility. Four ENet scenarios passed (Sakiko host, Sakiko client, and duplicate-Sakiko parties with two different choice offsets), with eight successful replay perspectives.

Multiplayer/replay tests preceded the final collection-cache filter; that revision changed collection visibility only. The final staged DLL/PCK exactly match the installation used for the accepted collection/legacy-save check and final custom/native reward smoke runs. The acceptance JSON records this boundary and each replay's package identity. Physical controller hardware, Steam/WAN multiplayer, and reconnect were not tested.
