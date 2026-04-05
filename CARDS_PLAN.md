# TogawaSakiko Card Port Plan

## Status Legend
- IMPLEMENT — fully doable with available STS2/BaseLib API
- TODO(reason) — needs unavailable API, write skeleton only

## Custom Powers Required
| Power | Effect | Status |
|-------|--------|--------|
| GodsCreationPower | Buff: reduce all damage received by Amount (applied to both player and enemies) | IMPLEMENT (BeforeDamageReceived) |
| DazzlingPower | Buff: stackable resource used by many cards; stacks carry over turns | IMPLEMENT (simple Counter buff) |
| CrueltyPower | Buff: whenever a Desire card is played, draw Amount cards | IMPLEMENT (AfterCardPlayed) |
| CrychicPower | Buff/Counter: at start of each turn, add a random Phantom card to hand (costs 0), reduce by 1 | IMPLEMENT (AfterPlayerTurnStart) |
| EndurancePower | Buff: when you would lose HP, gain Block equal to Amount first | TODO(no "on lose hp" pre-modifier hook; approximate with BeforeDamageReceived) |
| FearlessPower | Buff/Counter: at end of each turn, exhaust 1 card from hand, reduce by 1 | IMPLEMENT (BeforeTurnEnd) |
| HypePower | Buff: while you have block, block loss is prevented (consumes 1 stack) | TODO(no block loss hook; implement as plain Vulnerable-immunity buff, skeletal) |
| OurSongPower | Buff: when you deal attack damage, gain equal Block (expires end of turn) | IMPLEMENT (AfterDamageGiven) |
| PerdereOmniaPower | Buff: whenever you draw a 0-cost/unplayable card, remove it and draw a replacement | IMPLEMENT (AfterCardDrawn) |
| SeizeTheFatePower | Buff: at end of turn, gain Amount Hype (i.e. HypePower) | TODO(HypePower itself is TODO; apply Vulnerable instead as approximation) |
| SharedDestinyPower | Buff: whenever a new buff is applied to you, draw Amount cards | IMPLEMENT (AfterPowerAmountChanged) |
| WishYouGoodLuckPower | Buff: when you receive 0 damage from an attack, gain Amount Thorns | IMPLEMENT (AfterDamageReceived) |
| WorldviewPower | Buff: whenever you draw an unplayable card, replace it with a random attack | IMPLEMENT (AfterCardDrawn) |
| KingsPower (debuff) | Debuff: you receive one fewer card reward; tracked across combat | TODO(no card reward hook; skeletal) |
| DazzlingDownPower (debuff) | Debuff: at end of turn, reduce DazzlingPower by Amount | IMPLEMENT (AfterTurnEnd) |

---

## Cards

| # | File | Type | Rarity | Cost | Effect Summary | Status |
|---|------|------|--------|------|----------------|--------|
| **ATTACKS** |
| 1 | Angles | Attack | Uncommon | 1 | Deal 9 dmg; apply 1 GodsCreation to self AND target. Exhaust. | IMPLEMENT |
| 2 | AveMujica | Attack | Rare | 2 | Play all 4 Symbol cards + Ether in sequence (with upgrade). | TODO(complex sequence action; simplified: deal 7 dmg) |
| 3 | BlackBirthday | Attack | Rare | 2 | Deal 12 dmg to ALL enemies. Exhaust. | IMPLEMENT |
| 4 | ChoirSChoir | Attack | Uncommon | 3 | Deal 6 dmg 3 times; Retain; cost reduced by 1 each time Desire is played. | TODO(cost reduction on-trigger not supported; implement 3-hit attack with Retain) |
| 5 | CrucifixX | Attack | Uncommon | X | Deal 4 dmg + gain 4 block per energy spent (X energy). | TODO(X-cost; implement as fixed 2-cost deal 4 dmg + 4 block skeleton) |
| 6 | DarkHeaven | Attack | Common | 1 | Deal 9 dmg; steal 1 Strength from enemy. | IMPLEMENT (steal = -1 Strength on enemy, +1 Strength on self) |
| 7 | Daten | Attack | Common | 2 | Deal 13 dmg. Exhaust. (Heals via STS1 DatenAction) | IMPLEMENT (deal dmg + exhaust; healing action approximated as nothing) |
| 8 | Ether | Attack | Rare | 2 | Multi-hit (deals 2 dmg per exhausted card, up to 12 hits); add Oblivionis curse to deck. | TODO(complex exhaust-count mechanic; simplified: deal 12 * 2 dmg = 2dmg x 12 hits, add Oblivionis) |
| 9 | GeorgetteMeGeorgetteYou | Attack | Common | 0 | Hit 3x 4 dmg; apply 1 Strength to enemy. If upgraded, also apply 1 Hype to enemy. | IMPLEMENT (approx: apply Vulnerable instead of Hype) |
| 10 | HachibouseiDance | Attack | Uncommon | 4→3 | Deal 8 dmg; gain 8 PlatedArmor. | IMPLEMENT (PlatedArmor = Barricade approx; gain block instead) |
| 11 | IWantToBeYourGod | Attack | Rare | 2 | Deal 15 dmg; consume DazzlingPower to enter Divinity Stance (Watcher). | TODO(stance system not in STS2; simplified: deal 15 dmg, consume DazzlingPower to gain Strength) |
| 12 | ImprisonedXII | Attack | Rare | 3 | Deal 8 dmg; retain; gain 1 energy per Desire played this combat. | TODO(energy gain mid-combat; simplified: deal 8 dmg, Retain) |
| 13 | Kao | Attack | Uncommon | 1 | Deal 5 dmg (+4 per turn retained); Retain. | TODO(growing damage per retain = custom tracking; simplified: deal 5 dmg + Retain) |
| 14 | KillKiSS | Attack | Common | 1 | Deal 9 dmg; add 1(+1) Desire to hand. | IMPLEMENT |
| 15 | Kings | Attack | Common | 1 | Deal 14 dmg; apply KingsPower debuff to self (reduced card rewards). | TODO(KingsPower complex; simplified: deal 14 dmg) |
| 16 | MasqueradeRhapsodyRequest | Attack | Common | 1 | Deal escalating dmg (grows each turn it stays in hand). | TODO(dynamic base damage per turn; simplified: deal 8 dmg, magic 1 = bonus) |
| 17 | MementoMori | Attack | Rare | 2 | Exhaust/Remove 7 cards from draw pile; deal 7 dmg. | IMPLEMENT (shuffle draw into discard approximation) |
| 18 | PhantomOfMutsumi | Attack | Uncommon | 1 | Deal 6 dmg + gain 6 block; add Protection (1 Artifact power card) to discard. Exhaust. | IMPLEMENT |
| 19 | PhantomOfSakiko | Attack | Common | 2 | Hit 3x 4 dmg; add Radiance (Dazzling power card) to discard. Exhaust. | IMPLEMENT |
| 20 | PhantomOfSoyo | Attack | Uncommon | 1 | Deal 7 dmg to all enemies; add Kindness (power card) to discard. Exhaust. | IMPLEMENT |
| 21 | PhantomOfTaki | Attack | Common | 2 | Deal 12 dmg; add Ideal (free attack power card) to discard. Exhaust. | IMPLEMENT |
| 22 | PhantomOfTomori | Attack | Common | 2 | Deal 14 dmg to random enemy; add Voice (Strength power card) to discard. Exhaust. | IMPLEMENT |
| 23 | SoraNoMusica | Attack | Rare | 3 | Deal 12 dmg; draw 5 cards. Exhaust. | IMPLEMENT |
| 24 | SpringSunlight | Attack | Rare | 0 | Deal 30 dmg; cost = deck size / 6. | TODO(dynamic cost; simplified: fixed cost 3, deal 30 dmg) |
| 25 | Strike | Attack | Basic | 1 | Deal 6 dmg. | IMPLEMENT (already exists, add Localization) |
| 26 | SymbolIFire | Attack | Uncommon | 2 | Deal 20 dmg to all; add Timoris (curse) to discard; apply 1 Vulnerable (upgraded). | IMPLEMENT |
| 27 | SymbolIIAir | Attack | Common | 0 | Deal 14 dmg; draw 3 cards; add Amoris (curse) to discard. | IMPLEMENT |
| 28 | SymbolIIIWater | Attack | Uncommon | 2→1 | Deal 16 dmg to random enemy; end turn; add Doloris (curse) to discard. | IMPLEMENT (skip enemies turn via exhaust mechanic, just deal dmg + add curse) |
| 29 | SymbolIVEarth | Attack | Common | 1→0 | Gain 15 block; deal dmg = current block + 15; add Mortis curse to discard. | IMPLEMENT |
| 30 | TheGirlWithFlaxenHair | Attack | Uncommon | 1 | Deal 7 dmg; gain 3(+2) DazzlingPower. | IMPLEMENT |
| 31 | TheMoonlightSonata | Attack | Basic | 2 | Deal 15 dmg twice. Exhaust. | IMPLEMENT (simplified from relic-tripling) |
| 32 | TwoMoons | Attack | Common | 1 | Deal 7 dmg; if you have DazzlingPower, deal again. | IMPLEMENT |
| 33 | WishToBecomeHuman | Attack | Uncommon | 2 | Hit 2-hit 2 dmg; gain DazzlingPower equal to damage dealt. | IMPLEMENT |
| **SKILLS** |
| 34 | ASplitMoment | Skill | Common | 0 | Gain 1 Block; gain 1(+1) DazzlingPower. | IMPLEMENT |
| 35 | Accomplice | Skill | Common | 2 | Add 2(+1) Desire cards to hand. | IMPLEMENT |
| 36 | AleaIactaEst | Skill | Uncommon | 1 | Upgrade all cards in hand; if upgraded, also upgrade draw pile. No draw this turn. Exhaust. | IMPLEMENT (approximate: upgrade hand cards; no draw power not available, skip) |
| 37 | AreTheseLyrics | Skill | Uncommon | 1 | Complex Melody generation. | TODO(@CardEnable=false in STS1; simplified skeletal) |
| 38 | AsYourHeartDesires | Skill | Rare | 2→1 | Select a card from your deck copy to add to deck permanently. Exhaust. | TODO(choose existing deck card; simplified: draw 3 cards) |
| 39 | AuthorityRestoration | Skill | Uncommon | 1→0 | Remove 1 debuff from all enemies. Exhaust. | TODO(@CardEnable=false in STS1; simplified: remove 1 Vulnerable from enemies via PowerCmd) |
| 40 | BandInvitation | Skill | Rare | 0 | Can only be played if ≤1 other Skill in hand; draw 4(+1) cards. | IMPLEMENT (no canUse check available simply draw cards) |
| 41 | BlackAndWhiteKeys | Skill | Uncommon | 0 | Choose: BlackKeys (Strength + enemy DazzlingPower) or WhiteKeys (DazzlingPower + enemy Strength). | IMPLEMENT (simplified: choose via 2 separate embedded effects) |
| 42 | BudgetBento | Skill | Common | 1 | Gain 4(+1) Regen; add Tiredness to discard. Exhaust. | IMPLEMENT |
| 43 | Carefree | Skill | Common | 1 | Draw 2(+1) cards; choose 1 to retain. | TODO(@CardEnable=false; simplified: draw 2 cards) |
| 44 | ClockOut | Skill | Uncommon | 1 | Gain 15(+5) Gold; add Tiredness to discard. Exhaust. | TODO(gold gain mid-combat not ideal; simplified: draw 2 cards + add Tiredness) |
| 45 | CountingStars | Skill | Uncommon | 1 | Gain 1 extra energy next turn; draw 1(+1) card. | TODO(energy next turn not available; simplified: draw 2 cards) |
| 46 | Crychic | Skill | Rare | X | Apply CrychicPower (X+0/X+1 amount). Exhaust. | IMPLEMENT (X→3, apply CrychicPower 3) |
| 47 | Defend | Skill | Basic | 1 | Gain 5 Block. | IMPLEMENT (already exists, add Localization) |
| 48 | DesuWa | Skill | Common | 0 | Draw 1(+1) card. | IMPLEMENT (draw cards) |
| 49 | EdgeOfBreakdown | Skill | Common | 1 | Remove 1 card from discard; gain 2(−1 upgraded) Frail. Exhaust. | IMPLEMENT (select card from discard to exhaust; apply Frail = Vulnerable to self) |
| 50 | FallenFlowers | Skill | Uncommon | 2 | Gain 7(+2) DazzlingPower. Ethereal. | IMPLEMENT |
| 51 | Greetings | Skill | Uncommon | 0 | Gain 2(+1) energy. Innate. Exhaust. | TODO(energy gain; simplified: draw 2 cards, Innate, Exhaust) |
| 52 | HeartsBarrier | Skill | Common | 2 | Gain Block equal to deck size. Retain. | IMPLEMENT |
| 53 | InnerCry | Skill | Common | 1 | Gain 3(+1) DazzlingPower; gain 8(+2) Block. | IMPLEMENT |
| 54 | NeverGiveYouUp | Skill | Uncommon | 2→1 | Return 1 card from discard to hand. Exhaust. | IMPLEMENT |
| 55 | OurSong | Skill | Uncommon | 1→0 | Apply 1 OurSongPower. | IMPLEMENT |
| 56 | PerdereOmnia | Skill | Uncommon | 0 | Unplayable; when drawn, apply 1(+1) PerdereOmniaPower. | IMPLEMENT (OnTurnStartInHand or CardKeyword.Unplayable + drawn trigger) |
| 57 | Perfection | Skill | Rare | 3→2 | Upgrade all cards in hand. Exhaust. | IMPLEMENT |
| 58 | Pride | Skill | Rare | 0 | Innate; add 1(+1) random Attack to hand; track for end-of-turn return. Exhaust. | TODO(PridePower tracking; simplified: add 2 random attacks to hand, exhaust) |
| 59 | PrimoDieInScaena | Skill | Uncommon | 0 | Apply PrimoDieInScaena power. Upgrade = Innate. | TODO(PrimoDieInScaenaPower not in list; simplified: apply 1 Strength, Innate on upgrade) |
| 60 | QuaerereLumina | Skill | Common | 1 | Scry 7(+2); gain 1 Block per card scryed. | TODO(Scry not available; simplified: draw 2 cards + gain 7 block) |
| 61 | RaiseTheBet | Skill | Rare | 1 | Apply 3(+1) Strength to enemy; gain 3(+1) DazzlingPower; Retain. Exhaust. | IMPLEMENT (apply Strength to enemy + Dazzling to self) |
| 62 | RhinocerosBeetle | Skill | Uncommon | 1 | Gain Block = 4 + half your DazzlingPower. | IMPLEMENT (use CalculatedBlock) |
| 63 | SilentFarewell | Skill | Common | 0 | Apply 1(+1) Vulnerable to all enemies. | IMPLEMENT |
| 64 | StayElegance | Skill | Rare | 1→0 | Obtain a random potion. Exhaust. | TODO(potion generation mid-combat; simplified: gain 5 Dazzling + 5 Block) |
| 65 | Utopia | Skill | Uncommon | 2 | Deal 15(+5) HP loss to target (bypasses block). | IMPLEMENT |
| 66 | Veritas | Skill | Uncommon | 1 | Draw 3(+1) cards. | IMPLEMENT |
| 67 | WishFulfilled | Skill | Uncommon | 2 | Heal / complex action. Ethereal. Exhaust. | TODO(WishFulfilledAction not clear; simplified: gain 10 block, Exhaust, Ethereal) |
| **POWER CARDS** |
| 68 | CharismaticForm | Power | Rare | 3 | Permanent power; apply 2 Hype (Vulnerable) to all enemies; upgrade = Innate. | IMPLEMENT |
| 69 | Cruelty | Power | Rare | 0 | Apply 2(−1 upgraded) Vulnerable to self; apply CrueltyPower 2. | IMPLEMENT |
| 70 | Curiosity | Power | Uncommon | 2→1 | Apply Curiosity power (when playing a Skill, gain Strength). | IMPLEMENT (use base game CuriosityPower approximation: apply DexterityPower) |
| 71 | Endurance | Power | Uncommon | 1 | Apply 5(+2) EndurancePower. | IMPLEMENT |
| 72 | Fearless | Power | Uncommon | 1 | Apply 2(+1) FearlessPower. | IMPLEMENT |
| 73 | Masks | Power | Rare | 1 | Consume 2 DazzlingPower; gain 2 Strength; play Masks again next turn. | IMPLEMENT (simplified: gain Strength) |
| 74 | NumbersAndFaces | Power | Uncommon | 1 | Complex power tracker. | TODO(@CardEnable=false; skeletal) |
| 75 | Passion | Power | Uncommon | 1 | Apply Vigor. | TODO(@CardEnable=false; skeletal: apply Dexterity instead) |
| 76 | SeizeTheFate | Power | Uncommon | 1 | Decreasing: apply (6-plays) Hype; upgraded = no decrement. | TODO(HypePower complex; simplified: apply 6 Vulnerable to enemies at turn end via SeizeTheFatePower approx) |
| 77 | SharedDestiny | Power | Uncommon | 1 | Apply 1(+1) SharedDestinyPower: when a buff is applied to you, draw cards. | IMPLEMENT |
| 78 | WishYouGoodLuck | Power | Rare | 1 | Apply 1(+1) WishYouGoodLuckPower: when you take 0 damage, gain Thorns. | IMPLEMENT |
| 79 | Worldview | Power | Rare | 1 | Apply WorldviewPower: when you draw an unplayable card, replace with random attack. Upgrade = Innate. | IMPLEMENT |
| **SPECIAL / TOKEN CARDS** |
| 80 | Melody | Attack | Token | 0 | Deal 6(+3) dmg. | IMPLEMENT (autoAdd: false, CardRarity.Token) |
| 81 | Desire | Skill | Token | 1 | Gain 8(+3) Block. | IMPLEMENT (autoAdd: false, CardRarity.Token) |
| 82 | Tiredness | Skill | Token | 0 | Draw 1(+1) card. Exhaust. | IMPLEMENT (autoAdd: false, CardRarity.Token) |
| 83 | BlackKeys | Power | Token | 0 | Gain 2(+1) Strength; apply DazzlingPower to enemy. | IMPLEMENT (autoAdd: false) |
| 84 | WhiteKeys | Power | Token | 0 | Gain 2(+1) DazzlingPower; apply Strength to enemy. | IMPLEMENT (autoAdd: false) |
| 85 | Ideal | Power | Token | 1 | Gain 2(+1) free attacks (plays next attacks for 0). | IMPLEMENT (approximation: gain Strength 2) |
| 86 | Kindness | Power | Token | 1→0 | Restore lost powers. | TODO(no "restore lost powers" API; simplified: gain 3 DazzlingPower) |
| 87 | Protection | Power | Token | 0 | Gain 2(+1) Artifact. | IMPLEMENT (use ArtifactPower approximation: gain 2 block per stack, or just buff) |
| 88 | Radiance | Power | Token | 1 | Gain 2(+1) × 2 DazzlingPower. | IMPLEMENT |
| 89 | Voice | Power | Token | 1 | Gain 2(+1) × 2 Strength (Mantra approximated). | IMPLEMENT |
| **CURSES** |
| 90 | Amoris | Curse | Curse | 0 | Retain; harmless, just flashes at end of turn. | IMPLEMENT (autoAdd: false, Retain) |
| 91 | Doloris | Curse | Curse | 0 | When drawn, deal 2 self-damage at end of turn. | IMPLEMENT (autoAdd: false, OnTurnEndInHand) |
| 92 | Mortis | Curse | Curse | 0 | When drawn, apply MortisPower (on HP loss, add Injury to draw pile). | IMPLEMENT (autoAdd: false) |
| 93 | Oblivionis | Curse | Curse | 0 | Ethereal; when drawn, exhausts without effect. | IMPLEMENT (autoAdd: false, Ethereal) |
| 94 | Timoris | Curse | Curse | 0 | When drawn, apply 1 Vulnerable to self at start of next turn. | IMPLEMENT (autoAdd: false) |
| 95 | Weakness | Curse | Curse | 0 | @CardEnable=false in STS1; unplayable curse. | IMPLEMENT (autoAdd: false, skeletal unplayable) |

## Notes
- "Desire" in STS1 is a special Tag-marked card that many cards interact with. In STS2 we will add a custom CardTag for it.
- Cards marked @CardEnable(enable=false) in STS1 are still implemented but as skeletal stubs.
- HypePower (block-protection buff) has no direct STS2 equivalent hook; approximated with Vulnerable resistance.
- KingsPower (reduce card rewards) has no hook for reward modification; implemented as simple debuff placeholder.
