# Sakiko Ancient rewards

Implemented and locally validated on 2026-09-10 against installed STS2 v0.111.0. This is a source and staged-package change, not a Steam installation or published release.

## Behavior

- Archaic Tooth maps Moonlight Sonata to The Third Movement through the native transformation dictionary. Native setup, card previews, one-card transformation, saved reward fields, upgrades, and enchantments are retained. No Moonlight Sonata means no Tooth option.
- The Third Movement is an Ancient Attack costing 1 energy, with Exhaust and 30 damage (45 upgraded). It first runs Wish Fulfilled's existing selection/purge routine, then attacks if combat and the target remain valid. Selection covers removable cards in hand, draw, and discard; existing persistent-removal hooks and Utopia replacement behavior remain shared. No eligible selection still permits damage. It does not add Moonlight Sonata's post-combat card-removal reward.
- Charismatic Form is now Ancient and Dusty Tome grants its upgraded version, including Innate. Its stable ID, cost, and gameplay effects are unchanged. Existing saved copies retain their upgrade level and now use Ancient presentation.
- Both cards disable combat and modifier generation. Native rarity filtering excludes them from card/potion generation, normal rewards, shops, and random transformations. Archaic Tooth's transformation list excludes The Third Movement from Dusty Tome's choices. Charismatic Form is its sole eligible Sakiko card.
- As Your Heart Desires may copy either already-owned card into the deck. Copying retains upgrade level and enchantment; its implementation is unchanged.
- Touch of Orobas recognizes a non-melted Monochrome Hairband, Blazing Hairband, or Another Mask on its receiving Sakiko player and replaces the first eligible inventory entry with Enchanted Hairband. Enchanted Hairband cannot be refined again or appear in ordinary relic generation.
- Enchanted Hairband shares Blazing Hairband's victory conditions, card pool, one-award-per-combat guard, persistent addition, and preview. Its generated card is upgraded before insertion. The current eligible pool contains only cards with an upgrade. Another Mask's previous Defend transformations remain, while normal relic-removal presentation removes the mask appearance.

The new card ID is `TOGAWASAKIKO-THE_THIRD_MOVEMENT_CARD`; the relic ID is `TOGAWASAKIKO-ENCHANTED_HAIRBAND`. The older `TOGAWASAKIKO-THE_THIRD_MOVEMENT` relic is preserved separately for save compatibility. Gameplay model registration increases from 141 to 143.

## Artwork and localization

The supplied card PNGs are used directly in the native Ancient full-art layout. Enchanted Hairband uses the supplied 128px PNG at its original resolution. Its alpha channel is identical to Blazing Hairband's, so the existing outline is reused. Original supplied artwork and previous Charismatic Form portraits are retained under `TogawaSakiko/ArtSources/ancient-rewards` and excluded from the packaged content selection.

English and Simplified Chinese catalogs include The Third Movement / 第三乐章 and Enchanted Hairband / 附魔发带. Charismatic Form / 魅魔形态 retains its existing text and gameplay keyword handling. Catalog generation preserves the new native-only text and includes the added packaged textures.

## Validation

Final package identity:

| File | SHA-256 |
| --- | --- |
| TogawaSakiko.dll | F0AC7A8F25DF3B0B6A69A12BEAAA5271EF86383C305318FC5B12FC579256DC33 |
| TogawaSakiko.pck | C2FA3F11BEBCE9DF6681FB4DEA2A828D4F10435A5F887AB71C985368D510F2DB |
| TogawaSakiko.json | 7E7316E096CB0BDCD68431D99D7F4F842847EEA02B7CB884DE77D168D5916917 |
| TogawaSakiko.pdb | 4F0C904AC6E8B458509B906C55F63DD51245D3FFD7D7A841C79CAE35AD780131 |

- Release/Debug build and PCK export passed; staged package validation found zero external mod dependencies or excluded content tokens.
- Rendered English and Chinese Ancient suites each passed 149 assertions, with zero managed issues. Coverage includes Darv's actual option generation, Tooth/Tome pickups after serialized setup, all three Orobas replacements, melted/absent prerequisites, the native locked Orobas option, vanilla mappings, multiple-player owner isolation, upgraded and enchanted transformation, upgraded victory awards, repeated-victory suppression, persistent copying, inventory save reconstruction, normal reward/shop/transform exclusion, 30/45 damage, Exhaust, empty purge candidates, purge before damage, and a killing blow.
- English and Chinese N4 catalogs each passed: 371 localization entries, 742 formatted variants, 355 textures, 52 audio resources, 11 standalone resources, and 17 localization files. Existing startup N3/N5/N6 contracts also passed.
- Rendered screenshots were inspected for base/upgraded full-art layout, localized text, and relic transparency.

Evidence is under `artifacts/ancient-rewards/final/`: `ancient-eng-20260910-224945`, `ancient-zhs-20260910-224945`, and the two `ancient-catalog-*-20260910-224946` directories. Gameplay summaries bind the checks to the hashes above. Test processes are stopped after their success marker. Earlier failed harness attempts outside `final` are retained as development evidence, not acceptance results.

Reproduce against an isolated game folder containing the staged package:

```powershell
./tools/Invoke-AncientRewardTests.ps1 -GameRoot artifacts/ancient-rewards/game -ArtifactRoot artifacts/ancient-rewards -Language eng -Rendered
./tools/Invoke-AncientRewardTests.ps1 -GameRoot artifacts/ancient-rewards/game -ArtifactRoot artifacts/ancient-rewards -Language zhs -Rendered
```

These tests verify native serialization and multiple-player ownership in process. They do not constitute a fresh Steam/WAN multiplayer or full-run acceptance test. No existing user saves were modified, and the Steam mod installation was not updated.
