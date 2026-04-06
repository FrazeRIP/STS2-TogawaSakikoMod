# STS2 Modding Wiki — `sts2.dll` & `BaseLib.dll`

This document explains the architecture, systems, and functions available when modding **Slay the Spire 2** using the `sts2.dll` game assembly and the `BaseLib 0.2.6` modding framework. It is intended as a comprehensive reference for mod authors.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Mod Entry Point](#2-mod-entry-point)
3. [The Model System](#3-the-model-system)
4. [Cards](#4-cards)
5. [Powers](#5-powers)
6. [Relics](#6-relics)
7. [Potions](#7-potions)
8. [Characters](#8-characters)
9. [Pool System](#9-pool-system)
10. [Dynamic Variable System](#10-dynamic-variable-system)
11. [Localization System](#11-localization-system)
12. [Combat Hook System](#12-combat-hook-system)
13. [Command Classes](#13-command-classes)
14. [Utility Systems](#14-utility-systems)
15. [Config System](#15-config-system)
16. [Encounters, Events & Orbs](#16-encounters-events--orbs)
17. [Harmony Patching](#17-harmony-patching)
18. [Asset Loading](#18-asset-loading)

---

## 1. Overview

### Two assemblies, one modding framework

| Assembly | Purpose |
|---|---|
| `sts2.dll` | The core game. Contains all base types: `CardModel`, `PowerModel`, `RelicModel`, `CharacterModel`, `CombatState`, `Player`, `Creature`, and all enums. |
| `BaseLib.dll` | The mod framework. Provides `Custom*` abstract base classes, the `[Pool]` registration system, localization record helpers, `ConstructedCardModel`, config UI, and utilities like `SpireField` and `WeightedList`. |

You never modify `sts2.dll` directly. Instead:
- **Inherit** from BaseLib's `Custom*` classes to add new content.
- **Harmony-patch** `sts2.dll` types to modify existing behavior.
- **Register** new content via the `[Pool]` attribute and `ModelDb`.

### How content gets into the game

```
Your .cs class
  → decorated with [Pool(typeof(YourPool))]
    → BaseLib's CustomContentDictionary.AddModel() is called at startup
      → content appears in ModelDb and becomes available in-game
```

---

## 2. Mod Entry Point

Every mod needs exactly one entry point class:

```csharp
using MegaCrit.Sts2.Core.Modding;
using HarmonyLib;

[ModInitializer("Initialize")]
public static class MyModMain
{
    public static void Initialize()
    {
        var harmony = new Harmony("com.mymod.id");
        harmony.PatchAll();
        // Register configs, etc.
    }
}
```

**Key rules:**
- The class must be `static`.
- The method named in `[ModInitializer("MethodName")]` is called once by the game when the mod loads.
- Call `harmony.PatchAll()` here to activate all `[HarmonyPatch]` classes in your assembly.
- Register `ModConfig` instances here via `ModConfigRegistry.Register()`.

---

## 3. The Model System

### AbstractModel — the root of everything

Every piece of game content (`CardModel`, `PowerModel`, `RelicModel`, etc.) ultimately inherits from `AbstractModel`. It provides:

| Member | Description |
|---|---|
| `Id` (`ModelId`) | Unique identifier for this content. Derived from the class name. |
| `IsCanonical` | `true` on the "template" instance; combat instances are copies. |
| `ShouldReceiveCombatHooks` | Override to control whether this object receives combat events. |
| ~55 virtual `Task` hook methods | Called by the game engine during combat (see §12). |

### ModelDb — the static content registry

`ModelDb` is a static lookup table of all registered content:

```csharp
// Look up a canonical instance by type
CardModel strike = ModelDb.Card<StrikeTogawaSakiko>();
PowerModel vuln  = ModelDb.Power<VulnerablePower>();

// Get all content of a type
foreach (var card in ModelDb.AllCards) { ... }

// Get a pool
CardPoolModel pool = ModelDb.CardPool<MyCardPool>();
```

`ModelDb` is populated during game startup. Mods add to it via BaseLib's registration hooks.

---

## 4. Cards

### Inheritance chain

```
AbstractModel
  └── CardModel          (sts2.dll)  — base game card type
        └── CustomCardModel        (BaseLib) — adds custom visuals + localization
              └── ConstructedCardModel      (BaseLib) — fluent builder API (recommended)
                    └── YourCard
```

### CardModel properties (sts2.dll)

```csharp
string Title                    // localized name
LocString Description           // localized description with variable interpolation
CardType Type                   // Attack, Skill, Power, Status, Curse, Quest
CardRarity Rarity               // Basic, Common, Uncommon, Rare, ...
TargetType TargetType           // AnyEnemy, AllEnemies, Self, ...
CardEnergyCost EnergyCost       // current cost (can be modified)
bool IsUpgraded                 // whether this card has been upgraded
int CurrentUpgradeLevel         // 0 = base, 1 = upgraded
DynamicVarSet DynamicVars       // damage/block/power variables for this card
IReadOnlySet<CardKeyword> Keywords  // Exhaust, Ethereal, Innate, etc.
Player Owner                    // the player who owns this card
CombatState? CombatState        // non-null during combat
PileType? Pile                  // which pile this card is currently in
bool GainsBlock                 // auto-detected from BlockVar presence
```

### ConstructedCardModel — the fluent builder (BaseLib)

`ConstructedCardModel` is the recommended base class. It exposes a **fluent API called in the constructor** to declare damage, block, power variables, and keywords:

```csharp
[Pool(typeof(MyCardPool))]
public class MyCard : ConstructedCardModel
{
    public MyCard() : base(cost: 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 3)          // damage starts at 8, +3 on upgrade
            .WithBlock(4)                  // also gains block
            .WithPower<StrengthPower>(2)   // shows a hover tip for Strength
            .WithKeywords(CardKeyword.Exhaust)
            .WithTags(CardTag.Strike);
    }
}
```

**All `With*` methods must be called in the constructor** — they configure the card template. Calling them at runtime (e.g., inside `OnPlay`) does nothing useful and will cause errors.

| Builder method | What it does |
|---|---|
| `WithDamage(base, upgrade)` | Adds a `DamageVar` named `"Damage"`. Shows `!Damage!` in descriptions. |
| `WithBlock(base, upgrade)` | Adds a `BlockVar` named `"Block"`. Sets `GainsBlock = true`. |
| `WithCards(base, upgrade)` | Adds a `CardsVar` named `"Cards"`. |
| `WithPower<T>(base, upgrade)` | Adds a `PowerVar<T>` and a hover tooltip for that power type. |
| `WithVar(name, base, upgrade)` | Adds a generic `DynamicVar` by name. |
| `WithCalculatedDamage(base, bonus, props, upgrade)` | Damage with a runtime multiplier function. |
| `WithCalculatedBlock(base, bonus, props, upgrade)` | Block with a runtime multiplier function. |
| `WithKeywords(params CardKeyword[])` | Adds keyword icons to the card. |
| `WithTags(params CardTag[])` | Tags for game logic (Strike, Defend, Shiv, etc.). |
| `WithTip(TooltipSource)` | Adds a hover tooltip. |

### Card types and rarities

```
CardType:   Attack | Skill | Power | Status | Curse | Quest
CardRarity: Basic | Common | Uncommon | Rare | Ancient | Event | Token | Status | Curse | Quest
TargetType: Self | AnyEnemy | AllEnemies | RandomEnemy | AnyPlayer | AnyAlly | AllAllies
```

### Playing a card — OnPlay

```csharp
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    // play.Target = the targeted Creature (null for Self/AllEnemies)
    await CommonActions.CardAttack(this, play).Execute(ctx);  // deal damage
    await CommonActions.CardBlock(this, play);                // gain block
    await CommonActions.ApplySelf<StrengthPower>(this);       // apply power to self
    await CommonActions.Draw(this, ctx);                      // draw cards
}
```

The `CardPlay` object carries the target and context. Always `await` every async call.

### Special card hooks

```csharp
// Called each end of turn while this card is in hand
public override bool HasTurnEndInHandEffect => true; // must return true to enable
public override async Task OnTurnEndInHand(PlayerChoiceContext ctx) { ... }
```

### Card keywords

```
Exhaust   — removed from play after use
Ethereal  — exhausted at end of turn if still in hand
Innate    — always appears in opening hand
Unplayable — cannot be played manually
Retain    — kept in hand on turn end
Sly       — free to play
Eternal   — never removed from hand by pile operations
```

### Special card variable types (BaseLib)

| Type | Description | Usage |
|---|---|---|
| `ExhaustiveVar` | Card exhausts after N plays per combat | `new ExhaustiveVar(3)` |
| `PersistVar` | Card exhausts after N plays per turn | `new PersistVar(2)` |
| `RefundVar` | Refunds N energy when played | `new RefundVar(1)` |

### Token and Curse cards

Cards that should not appear in draft pools use `autoAdd: false`:

```csharp
public class MyTokenCard : TogawaSakikoCard
{
    public MyTokenCard() : base(0, CardType.Attack, CardRarity.Token,
        TargetType.AnyEnemy, autoAdd: false) { }
}
```

---

## 5. Powers

Powers are persistent buffs/debuffs applied to `Creature` instances. They receive all combat hooks and tick each turn.

### Inheritance chain

```
AbstractModel
  └── PowerModel          (sts2.dll)
        └── CustomPowerModel        (BaseLib) — adds custom icon + localization
              └── YourPower
```

### PowerModel properties

```csharp
PowerType Type           // Buff, Debuff, or None
PowerStackType StackType // Counter (stackable) or Single (exists or doesn't)
int Amount               // current stack count
int AmountOnTurnStart    // snapshot of Amount at turn start
bool AllowNegative       // whether Amount can go below 0
bool IsInstanced         // whether multiple instances can coexist
Creature Owner           // the creature this power is applied to
Creature? Applier        // who applied it (may be null)
DynamicVarSet DynamicVars // for variable display in descriptions
```

### Defining a power

```csharp
public class MyBuff : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Fires every time the player's turn starts
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (Owner.Side == CombatSide.Player)
        {
            await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
        }
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            Title: "My Buff",
            Description: "At the start of your turn, gain !Amount! Strength.",
            SmartDescription: "At the start of your turn, gain {Amount} Strength."
        );
}
```

**Description vs SmartDescription:**
- `Description` — shown in static contexts (tooltips, deck viewer). Safe to use `!Var!` syntax.
- `SmartDescription` — shown during combat with live values. Use `{Amount}` for the power's `Amount` property.

### Power lifecycle hooks

```csharp
// Called just before this power is applied
public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? src)

// Called just after being applied/stacked
public override Task AfterApplied(Creature? applier, CardModel? src)

// Called when the power is fully removed
public override Task AfterRemoved(Creature oldOwner)

// Whether this power should be cleaned up when its owner dies
public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
```

### Applying powers

```csharp
// From a card's OnPlay:
await PowerCmd.Apply<WeakPower>(play.Target, 2, Owner.Creature, this);

// To self from a card:
await CommonActions.ApplySelf<StrengthPower>(this);

// With explicit amount:
await PowerCmd.Apply<VulnerablePower>(enemy, 3, Owner.Creature, cardSource);

// Remove a power:
await PowerCmd.Remove<WeakPower>(target);

// Modify stack count:
await PowerCmd.ModifyAmount(existingPower, -1, Owner, null);
```

### Temporary powers (BaseLib)

`CustomTemporaryPowerModel` wraps another power so it lasts N turns, then removes itself:

```csharp
public class MyTemporaryStrength : CustomTemporaryPowerModel
{
    public override PowerModel InternallyAppliedPower => ModelDb.Power<StrengthPower>();
    public override AbstractModel OriginModel => this;
    protected override Func<Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc =>
        (target, amount, applier, src, silent) =>
            PowerCmd.Apply<StrengthPower>(target, amount, applier, src, silent);
}
```

---

## 6. Relics

Relics are persistent items held by the player that grant passive effects. They receive all combat hooks.

### Inheritance chain

```
AbstractModel
  └── RelicModel          (sts2.dll)
        └── CustomRelicModel        (BaseLib)
              └── YourRelic
```

### Defining a relic

```csharp
[Pool(typeof(MyRelicPool))]
public class MyRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // Fires when the relic is first obtained (outside combat)
    public override async Task AfterObtained() { }

    // Fires when removed
    public override async Task AfterRemoved() { }

    // Fires at combat start
    public override async Task BeforeCombatStart()
    {
        Flash(); // visual flash effect (only valid on RelicModel)
        // ... logic
    }

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            Title: "My Relic",
            Description: "At the start of each combat, gain 1 energy.",
            Flavor: "Still humming."
        );
}
```

### Relic rarities

```
Starter | Common | Uncommon | Rare | Shop | Event | Ancient
```

### Relic upgrade

```csharp
// Override to replace this relic with an upgraded version on upgrade events
public override RelicModel? GetUpgradeReplacement() => new MyUpgradedRelic();
```

### Counter display

```csharp
public override bool ShowCounter => true;        // shows a number badge
public override int DisplayAmount => _counter;   // what number to show
public override bool IsStackable => false;       // whether multiple copies stack
```

---

## 7. Potions

Potions are consumable one-use items.

### Inheritance chain

```
AbstractModel
  └── PotionModel          (sts2.dll)
        └── CustomPotionModel        (BaseLib)
              └── YourPotion
```

### Required overrides

```csharp
[Pool(typeof(MyPotionPool))]
public class MyPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly; // or AnyTime, Automatic
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
    {
        await DamageCmd.Attack(10m).Targeting(target!).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new PotionLoc("My Potion", "Deal 10 damage.");
}
```

---

## 8. Characters

Characters are the playable classes. They define starting HP, deck, relics, card pool, and visual assets.

### Inheritance chain

```
AbstractModel
  └── CharacterModel          (sts2.dll)
        └── CustomCharacterModel        (BaseLib)
              └── PlaceholderCharacterModel   (BaseLib) — use while assets aren't ready
                    └── YourCharacter
```

### Defining a character

```csharp
public class MyCharacter : CustomCharacterModel
{
    public override Color NameColor => new Color("ff6600");
    public override CharacterGender Gender => CharacterGender.Female;
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<MyCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyPotionPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<MyStrike>(), ModelDb.Card<MyStrike>(),
        ModelDb.Card<MyDefend>(), ModelDb.Card<MyDefend>(),
    };

    public override IReadOnlyList<RelicModel> StartingRelics =>
        new[] { ModelDb.Relic<MyStarterRelic>() };
}
```

### Visual asset paths

Override these to hook in custom images/animations:

```csharp
public override string? CustomVisualPath => null;             // Godot scene for character sprite
public override string? CustomIconTexturePath => "...";       // small character icon
public override string? CustomCharacterSelectIconPath => "..."; // character select portrait
public override string? CustomMapMarkerPath => "...";         // map node marker
public override string? CustomRestSiteAnimPath => null;       // rest site animation
```

### Animation setup

```csharp
public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    => SetupAnimationState(controller,
        idleName: "Idle",
        attackName: "Attack",
        hitName: "Hit",
        deadName: "Dead",
        relaxedName: "Relaxed");
```

---

## 9. Pool System

Pools are the game's registries for each content type. Content is added to pools at startup via the `[Pool]` attribute.

### Pool types

| Pool class | Content it holds |
|---|---|
| `CustomCardPoolModel` | Cards for a specific character |
| `CustomRelicPoolModel` | Relics for a specific character |
| `CustomPotionPoolModel` | Potions for a specific character |

### Defining a pool

```csharp
public class MyCardPool : CustomCardPoolModel
{
    // Cards are added automatically via [Pool(typeof(MyCardPool))] on each card class.
    // You only need to override if you have special pool-level behavior.

    public override string CardFrameMaterialPath => "card_frame_red"; // visual theme
    public override Color ShaderColor => new Color("FF8800");
    public override bool IsShared => false; // true = appears in ALL characters' rewards
    public override string? BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string? TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
```

### How [Pool] works

When the game loads, BaseLib scans all assemblies for classes with `[Pool(typeof(T))]` and registers them:

```csharp
[Pool(typeof(MyCardPool))]            // required attribute
public class MyCard : ConstructedCardModel
{
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy,
        showInCardLibrary: true,
        autoAdd: true)               // autoAdd=true → added to the pool automatically
    { }
}
```

- `autoAdd: true` (default) — content is auto-registered with the pool named in `[Pool]`.
- `autoAdd: false` — content is **not** added to any pool (use for Token/Curse cards that are generated at runtime, not drafted).

---

## 10. Dynamic Variable System

Dynamic variables power the card/power description system. They represent numbers that can change based on upgrades, powers like Strength, and other modifiers.

### Core types

| Type | Name key | Description |
|---|---|---|
| `DamageVar` | `"Damage"` | Scales with Strength, Weak, Vulnerable, etc. |
| `BlockVar` | `"Block"` | Scales with Dexterity, Frail, etc. |
| `CardsVar` | `"Cards"` | A count of cards (draw amount, etc.) |
| `PowerVar<T>` | Class name of T | Amount of a specific power to apply; adds hover tooltip |
| `DynamicVar` | Custom | Generic numeric variable |
| `BoolVar` | Custom | Boolean flag variable |
| `CalculatedDamageVar` | `"Damage"` | Damage + runtime bonus function |
| `CalculatedBlockVar` | `"Block"` | Block + runtime bonus function |

### Declaring variables

In `CustomCardModel`, override `CanonicalVars`:
```csharp
protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
{
    new DamageVar(8, DamageProps.card).WithUpgrade(3),  // 8 base, +3 upgraded
    new BlockVar(5, BlockProps.card),
    new PowerVar<WeakPower>(2),
};
```

In `ConstructedCardModel`, use the fluent builder instead:
```csharp
WithDamage(8, upgrade: 3)
    .WithBlock(5)
    .WithPower<WeakPower>(2);
```

### Accessing variables at runtime

```csharp
// Named accessors (throw if not present):
DamageVar dmg  = DynamicVars.Damage;
BlockVar  blk  = DynamicVars.Block;
CardsVar  draw = DynamicVars.Cards;

// Access a custom power's var:
DynamicVar pwrVar = DynamicVars.Power<MyCustomPower>();  // BaseLib extension

// Generic by key:
DynamicVar myVar = DynamicVars["MyVarName"];
bool exists = DynamicVars.ContainsKey("MyVarName");

// Reading value:
int n    = dmg.IntValue;
decimal d = dmg.BaseValue;
```

### Upgrade values

```csharp
// WithUpgrade() stores the delta to add on upgrade:
new DamageVar(6, DamageProps.card).WithUpgrade(3)  // → 6 base, 9 upgraded
```

When a card is upgraded, `UpgradeValueBy()` is called internally and `FinalizeUpgrade()` locks it in.

### Description interpolation

In localization strings, `!VarName!` is replaced with the variable's display value:
```
"Deal !Damage! damage. Gain !Block! Block. Apply !WeakPower! Weak."
```
The name must match `DynamicVar.Name` exactly (e.g., `"Damage"`, `"Block"`, `"WeakPower"`).

### ValueProp flags

`ValueProp` controls how damage/block is calculated:

```csharp
[Flags]
enum ValueProp
{
    Unblockable   = 2,   // bypasses block
    Unpowered     = 4,   // bypasses Strength, Dexterity, Weak, etc.
    Move          = 8,   // "from a card or monster move" — enables Strength scaling
    SkipHurtAnim  = 0x10
}

// Common presets:
DamageProps.card           = ValueProp.Move                         // normal card damage
DamageProps.cardUnpowered  = ValueProp.Unpowered | ValueProp.Move  // ignores STR/Weak
DamageProps.cardHpLoss     = ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move
BlockProps.card            = ValueProp.Move                         // normal card block
```

---

## 11. Localization System

Localization is provided in-memory via the `Localization` property on every `ILocalizationProvider`. BaseLib injects these values when the game looks up that type's loc table key.

### Localization records

All records have implicit conversion to `List<(string, string)>`:

#### CardLoc
```csharp
public override List<(string, string)>? Localization =>
    new CardLoc(
        Title: "Strike",
        Description: "Deal !Damage! damage.",
        // Optional extra keys:
        ("upgradedDescription", "Deal !Damage! damage. Upgraded!")
    );
```

#### PowerLoc
```csharp
new PowerLoc(
    Title: "Strength",
    Description: "Increases attack damage.",           // static (outside combat)
    SmartDescription: "Increases attack damage by {Amount}."  // live (in combat)
)
```
Use `{Amount}` (curly braces) in `SmartDescription` for the power's runtime `Amount` value.

#### RelicLoc
```csharp
new RelicLoc(Title: "...", Description: "...", Flavor: "...")
```

#### PotionLoc
```csharp
new PotionLoc(Title: "...", Description: "...")
```

#### MonsterLoc
```csharp
new MonsterLoc(
    Name: "Goblin",
    MoveTitles: new[] { ("ATTACK", "Scratch"), ("DEFEND", "Cower") }
)
```

#### CharacterLoc
A larger record covering name, pronouns, end-of-turn pings, and other character-specific strings:
```csharp
new CharacterLoc(
    Title: "Sakiko", TitleObject: "the Composer",
    PronounObject: "her", PronounSubject: "she",
    PronounPossessive: "her", PossessiveAdjective: "her",
    ...
)
```

### How the lookup works

Each model's `Id.Entry` maps to a key in a loc table:
- Cards → `"cards"` table, keys like `"mycard.title"`, `"mycard.description"`
- Powers → `"powers"` table
- Relics → `"relics"` table
- etc.

Override `ILocalizationProvider.LocTable` to use a custom table name.

---

## 12. Combat Hook System

Every `AbstractModel` subclass can override ~55 virtual `Task` methods that fire at specific points during combat. These are the core mechanism for implementing card and power effects.

### Hook firing order (turn structure)

```
BeforeCombatStart()
BeforeCombatStartLate()
  ↓ (combat loop)
  BeforeSideTurnStart(side)
  AfterSideTurnStart(side)
  AfterPlayerTurnStartEarly(ctx, player)
  AfterPlayerTurnStart(ctx, player)
  AfterPlayerTurnStartLate(ctx, player)
  AfterHandDraw (via BeforeHandDraw / AfterCardDrawn)
  BeforePlayPhaseStart(ctx, player)
    [player plays cards → BeforeCardPlayed, AfterCardPlayed, AfterCardPlayedLate]
  BeforeTurnEndVeryEarly(ctx, side)
  BeforeTurnEndEarly(ctx, side)
  BeforeTurnEnd(ctx, side)
  AfterTurnEnd(ctx, side)
  AfterTurnEndLate(ctx, side)
AfterCombatEnd(room)
AfterCombatVictory(room)
```

### Key hooks reference

| Hook | When it fires |
|---|---|
| `BeforeCombatStart()` | Before any combat setup; good for initial effects |
| `AfterPlayerTurnStart(ctx, player)` | Start of player's turn (after card draw) |
| `BeforeTurnEnd(ctx, side)` | End of side's turn (before discard) |
| `AfterTurnEnd(ctx, side)` | End of side's turn (after discard) |
| `BeforeCardPlayed(cardPlay)` | Before a card resolves |
| `AfterCardPlayed(ctx, cardPlay)` | After a card resolves |
| `AfterCardDrawn(ctx, card, fromHandDraw)` | After a specific card is drawn |
| `AfterCardChangedPiles(card, oldPile, source)` | Any time a card moves between piles |
| `AfterCardExhausted(ctx, card, causedByEthereal)` | When any card is exhausted |
| `AfterDamageGiven(ctx, dealer, result, props, target, src)` | After this model deals damage |
| `AfterDamageReceived(ctx, target, result, props, dealer, src)` | After target takes damage |
| `BeforeDamageReceived(ctx, target, amount, props, dealer, src)` | Before damage is applied |
| `AfterBlockGained(creature, amount, props, src)` | After any block is gained |
| `AfterCurrentHpChanged(creature, delta)` | After HP changes |
| `BeforeDeath(creature)` | Just before a creature would die |
| `AfterDeath(ctx, creature, wasRemovalPrevented, deathAnimLength)` | After death |
| `AfterPowerAmountChanged(power, amount, applier, src)` | After any power stack changes |
| `AfterRoomEntered(room)` | When the player enters any room |
| `AfterMapGenerated(map, actIndex)` | After act map is generated |
| `AfterRewardTaken(player, reward)` | When the player takes a reward |

### Which objects receive hooks

- **`CardModel`**: Only when `Pile?.IsCombatPile == true` (i.e., in Hand, Draw, Discard, Exhaust during a combat). Cards in the deck between combats do **not** receive hooks.
- **`PowerModel`**: Always during combat (`ShouldReceiveCombatHooks = true`).
- **`RelicModel`**: Always during combat.
- **`CharacterModel`**: Always.

### Checking combat side

```csharp
// In a power or relic hook:
if (Owner.Side == CombatSide.Player)
{
    // Only fire for the player's side
}
```

---

## 13. Command Classes

Command classes are the primary way to trigger game effects. Always `await` them.

### DamageCmd

```csharp
// Build an attack command, then chain fluent modifiers, then execute:
await DamageCmd.Attack(damage: 12m)
    .FromCard(this)
    .Targeting(play.Target)
    .WithHitCount(3)
    .Execute(ctx);

// Convenience — uses card's DamageVar automatically:
await CommonActions.CardAttack(this, play, hitCount: 2).Execute(ctx);

// Target all opponents:
await DamageCmd.Attack(8m)
    .TargetingAllOpponents(CombatState)
    .Execute(ctx);
```

### PowerCmd

```csharp
// Apply a power to a target
await PowerCmd.Apply<WeakPower>(target, amount: 2, applier: Owner.Creature, cardSource: this);

// Apply to multiple targets
await PowerCmd.Apply<WeakPower>(CombatState.Enemies, 2, Owner.Creature, this);

// Remove a power
await PowerCmd.Remove<WeakPower>(target);

// Modify stack count (delta can be negative)
await PowerCmd.ModifyAmount(existingPowerInstance, -1, Owner.Creature, null);
```

### CreatureCmd

```csharp
// Gain block
await CreatureCmd.GainBlock(creature, blockVar, cardPlay, fast: false);
await CreatureCmd.GainBlock(creature, amount: 8m, BlockProps.card, cardPlay);

// Deal direct damage (not a card attack — bypasses attack hooks)
await CreatureCmd.Damage(ctx, target, amount: 5m, DamageProps.nonCardUnpowered, dealer: null);

// Kill
await CreatureCmd.Kill(creature, force: false);

// Lose block
await CreatureCmd.LoseBlock(creature, amount: 3m);

// Summon a monster
await CreatureCmd.Add<MyMonster>(CombatState);
```

### CardPileCmd

```csharp
// Draw cards
await CardPileCmd.Draw(ctx, count: 2, player: Owner);

// Move a card to a pile
await CardPileCmd.Add(card, PileType.Hand);
await CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom);

// Add a freshly created card to combat (triggers "generated" hooks)
await CardPileCmd.AddGeneratedCardToCombat(new MelodyCard(), PileType.Hand, addedByPlayer: true);

// Preview and add cards (shows a visual preview to the player)
await CardPileCmd.AddToCombatAndPreview<MyCard>(Owner.Creature, PileType.Hand, count: 1, addedByPlayer: true);

// Remove from deck permanently
await CardPileCmd.RemoveFromDeck(card, showPreview: true);

// Remove from current combat (no visual)
await CardPileCmd.RemoveFromCombat(card);

// Shuffle the draw pile
await CardPileCmd.Shuffle(ctx, player: Owner);

// Add a curse to deck (without showing it to the player in combat)
await CardPileCmd.AddCurseToDeck<TimorisCard>(Owner);
```

### CardSelectCmd

```csharp
// Let player choose cards from a grid
var chosen = await CardSelectCmd.FromSimpleGrid(
    ctx,
    cards: someCardList,
    owner: Owner,
    prefs: new CardSelectorPrefs(prompt, count: 1));

// Convenience (CommonActions):
var card = await CommonActions.SelectSingleCard(
    this, new LocString("cards", "myprompt"), ctx, PileType.Discard);
```

### CommonActions — convenience wrappers

```csharp
// Card attack using the card's Damage variable
CommonActions.CardAttack(card, play, hitCount: 1)          // → AttackCommand
CommonActions.CardAttack(card, target, hitCount: 1)

// Card block using the card's Block variable
await CommonActions.CardBlock(this, play);

// Draw using the card's Cards variable
await CommonActions.Draw(this, ctx);

// Apply a power to target using the card's PowerVar<T>
await CommonActions.Apply<WeakPower>(target, this);
await CommonActions.Apply<WeakPower>(target, this, amount: 3m);

// Apply to self
await CommonActions.ApplySelf<StrengthPower>(this);
await CommonActions.ApplySelf<StrengthPower>(this, amount: 2m);

// Card selection
await CommonActions.SelectCards(this, prompt, ctx, PileType.Hand, count: 2);
await CommonActions.SelectSingleCard(this, prompt, ctx, PileType.Discard);
```

---

## 14. Utility Systems

### SpireField — attaching data to existing objects

`SpireField<TKey, TVal>` is a `ConditionalWeakTable`-backed dictionary that lets you attach extra data to game objects without modifying them:

```csharp
// Declare as a static field — lives for the lifetime of the process
public static readonly SpireField<CardModel, int> TimesPlayed =
    new SpireField<CardModel, int>(() => 0);

// Use it:
int count = TimesPlayed[card];      // read (returns default if never set)
TimesPlayed[card] = count + 1;      // write
```

#### SavedSpireField — persistent across saves

```csharp
public static readonly SavedSpireField<RelicModel, bool> HasActivated =
    new SavedSpireField<RelicModel, bool>(() => false, "HasActivated");
```

Supported value types: `int`, `bool`, `string`, `ModelId`, `Enum`, `SerializableCard`, and arrays/lists of those.

### WeightedList — random selection with weights

```csharp
var list = new WeightedList<string>();
list.Add("Common", weight: 10);
list.Add("Uncommon", weight: 5);
list.Add("Rare", weight: 1);

string result = list.GetRandom(rng);           // pick one
string drawn  = list.GetRandom(rng, remove: true); // pick and remove
```

Alternatively, items can implement `IWeighted { int Weight { get; } }` and be added with `list.Add(item)`.

### TooltipSource — hover tooltips

Used with `ConstructedCardModel.WithTip()` to show a tooltip when hovering over part of a card:

```csharp
// From a type (shows that model's tooltip):
.WithTip(typeof(ExhaustKeyword))
.WithTip(typeof(StrengthPower))

// From a CardKeyword:
.WithTip(CardKeyword.Exhaust)
```

### ModelExtensions

```csharp
// Build a localization sub-key for a model:
string key = myCard.LocKey("prompt");  // → "mycard.prompt"
```

### DynamicVarExtensions

```csharp
// Add upgrade delta:
new DamageVar(6, DamageProps.card).WithUpgrade(3)

// Add a hover tooltip to this variable:
new DamageVar(6, DamageProps.card).WithTooltip("damage_tooltip")

// Calculate actual block value after modifiers:
decimal finalBlock = blockVar.CalculateBlock(creature, BlockProps.card, cardPlay);
```

### ActModel extensions (BaseLib)

```csharp
// Get the act number (1, 2, 3) from an ActModel:
int actNum = actModel.ActNumber();
// Returns: 1 = Overgrowth/Underdocks, 2 = Hive, 3 = Glory, -1 = unknown
```

---

## 15. Config System

BaseLib provides an in-game settings UI for mods. Register your config in `Initialize()`:

```csharp
ModConfigRegistry.Register("MyMod", new MyModConfig());
```

### SimpleModConfig (recommended)

Automatically generates UI from public static properties:

```csharp
public class MyModConfig : SimpleModConfig
{
    // bool → toggle switch
    public static bool EnableFeature { get; set; } = true;

    // double → slider (use [SliderRange] to set bounds)
    [SliderRange(0.5, 2.0)]
    [SliderLabelFormat("0.0×")]
    public static double DamageMultiplier { get; set; } = 1.0;

    // Enum → dropdown
    public static DifficultyMode Difficulty { get; set; } = DifficultyMode.Normal;

    // string → text input
    public static string ModPrefix { get; set; } = "MOD";
}
```

**Config attributes:**

| Attribute | Effect |
|---|---|
| `[ConfigIgnore]` | Exclude from file and UI entirely |
| `[ConfigHideInUI]` | Save to file but hide from settings UI |
| `[ConfigSection("Header")]` | Insert a section header above this property |
| `[ConfigButton("Label")]` | Mark a method as a button in the UI |
| `[ConfigVisibleWhen("PropName")]` | Show only when `PropName` is true |
| `[SliderRange(min, max)]` | Sets bounds for a double slider |
| `[HoverTipsByDefault]` | Adds hover tooltips to all options |

### Accessing config values

Properties are static, so access them anywhere:
```csharp
if (MyModConfig.EnableFeature)
{
    // ...
}
float mult = (float)MyModConfig.DamageMultiplier;
```

---

## 16. Encounters, Events & Orbs

### CustomEncounterModel

Registers a custom combat encounter (monster room, elite, or boss):

```csharp
public class MyBossEncounter : CustomEncounterModel
{
    public MyBossEncounter() : base(RoomType.Boss) { }

    // Only appear in specific acts
    public override bool IsValidForAct(ActModel act) => act.ActNumber() == 3;

    // Optional: custom background
    public override BackgroundAssets? CustomEncounterBackground(ActModel act, Rng rng) => null;
}
```

### CustomEventModel

Registers a custom event (story room):

```csharp
public class MyEvent : CustomEventModel
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new[]
    {
        new EventOption("Option A", ctx => { /* handle */ return Task.CompletedTask; }),
        new EventOption("Leave",    ctx => Task.CompletedTask),
    };

    public override List<(string, string)>? Localization => /* ... */;
}
```

### CustomOrbModel

Registers a custom Orb (Defect-style channeling):

```csharp
public class MyOrb : CustomOrbModel
{
    public override bool IncludeInRandomPool => true;

    // Fires each turn the orb is channeled (passive)
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player) { }

    // Fires when evoked
    public override async Task AfterOrbEvoked(PlayerChoiceContext ctx, OrbModel orb, IEnumerable<Creature> targets) { }
}
```

### CustomAncientModel

Registers a custom Ancient Shrine event:

```csharp
public class MyAncient : CustomAncientModel
{
    public MyAncient() : base() { }

    protected override OptionPools MakeOptionPools => new OptionPools(
        left: MakePool(AncientOption<MyRelicA>(weight: 3), AncientOption<MyRelicB>(weight: 1)),
        right: MakePool(relicInstanceC, relicInstanceD)
    );

    public override bool IsValidForAct(ActModel act) => true;
}
```

---

## 17. Harmony Patching

Harmony lets you modify game behavior without touching source code.

### Prefix (runs before original method)

```csharp
[HarmonyPatch(typeof(SomeGameClass), "SomeMethod")]
public static class MySomePatch
{
    [HarmonyPrefix]
    public static bool Prefix(SomeGameClass __instance, ref ReturnType __result, ArgType arg)
    {
        // Return true  → run the original method afterward
        // Return false → skip the original method entirely
        return true;
    }
}
```

### Postfix (runs after original method)

```csharp
[HarmonyPatch(typeof(SomeGameClass), "SomeMethod")]
public static class MySomePatch
{
    [HarmonyPostfix]
    public static void Postfix(SomeGameClass __instance, ReturnType __result)
    {
        // __result contains the return value (for ref types, can be modified)
    }
}
```

### Special Harmony parameters

| Parameter name | Type | Description |
|---|---|---|
| `__instance` | `T` | The instance being operated on (for instance methods) |
| `__result` | `ref ReturnType` | The return value (postfix can modify via `ref`) |
| `__state` | `ref T` | Shared state passed from prefix to postfix |
| `___fieldName` | any | Accesses a private field named `fieldName` |

### Krafs Publicizer

The project uses `Krafs.Publicizer` to make internal STS2 members accessible:

```xml
<!-- In .csproj -->
<Publicize Include="sts2" />
```

This lets you access any `internal` or `private` member of `sts2.dll` directly from C# without reflection.

---

## 18. Asset Loading

### How STS2 loads mod assets

Assets are loaded via Godot's `ResourceLoader`. Mod assets live under the mod's folder in the `.pck` file. Paths are always relative to the mod root:

```
TogawaSakiko/images/card_portraits/big/striketogawasakiko.png
TogawaSakiko/images/powers/crueltypower.png
TogawaSakiko/audio/music/GMGU.ogg
```

### Naming convention

All asset filenames are derived from the **lowercased class name** (after stripping the mod prefix via `Id.Entry.RemovePrefix().ToLowerInvariant()`):

| Class name | Expected filename |
|---|---|
| `StrikeTogawaSakiko` | `striketogawasakiko.png` |
| `AnglesCard` | `anglescard.png` |
| `CrueltyPower` | `crueltypower.png` |
| `BlazingHairband` | `blazinghairband.png` |

### Fallback system

All `Custom*` base classes check `ResourceLoader.Exists(path)` and fall back to a placeholder:

```csharp
// In TogawaSakikoCard:
public override string CustomPortraitPath
{
    get
    {
        var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
        return ResourceLoader.Exists(path) ? path : "card.png".BigCardImagePath();
    }
}
```

### Card portrait sizes

| Usage | Recommended size |
|---|---|
| `CustomPortraitPath` / `card_portraits/big/` | 606×852 (full art) or 1000×760 (normal art) |
| `PortraitPath` / `card_portraits/` | 250×350 (full art scaled) or 250×190 (normal art scaled) |
| Power icon (`powers/`) | 32×32 |
| Power big icon (`powers/big/`) | 64×64 |
| Relic icon (`relics/`) | 64×64 |
| Relic large icon (`relics/big/`) | 128×128 |

### Path helper methods (BaseLib — ResourcePaths / StringExtensions)

```csharp
"filename.png".BigCardImagePath()   // → "TogawaSakiko/images/card_portraits/big/filename.png"
"filename.png".CardImagePath()      // → "TogawaSakiko/images/card_portraits/filename.png"
"filename.png".PowerImagePath()     // → "TogawaSakiko/images/powers/filename.png"
"filename.png".BigPowerImagePath()  // → "TogawaSakiko/images/powers/big/filename.png"
"filename.png".RelicImagePath()     // → "TogawaSakiko/images/relics/filename.png"
"filename.png".BigRelicImagePath()  // → "TogawaSakiko/images/relics/big/filename.png"
"filename.png".CharacterUiPath()    // → "TogawaSakiko/images/charui/filename.png"
"filename.png".ImagePath()          // → "TogawaSakiko/images/filename.png"
"filename.wav".AudioPath()          // → "TogawaSakiko/audio/filename.wav"  (via ResourcePaths.Audio)
```

---

## Quick Reference — Common Patterns

### Minimal card

```csharp
[Pool(typeof(MyPool))]
public class MyCard : ConstructedCardModel
{
    public MyCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(ctx);

    public override List<(string, string)>? Localization =>
        new CardLoc("My Card", "Deal !Damage! damage.");
}
```

### Minimal power

```csharp
public class MyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (Owner.Side == CombatSide.Player)
            await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc("My Power", "Gain !Amount! Strength each turn.", "Gain {Amount} Strength each turn.");
}
```

### Minimal relic

```csharp
[Pool(typeof(MyRelicPool))]
public class MyRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeCombatStart()
    {
        Flash(); // OK on relics
    }

    public override List<(string, string)>? Localization =>
        new RelicLoc("My Relic", "At combat start, do something.", "Mysterious.");
}
```

### Multi-hit attack

```csharp
await CommonActions.CardAttack(this, play, hitCount: 3).Execute(ctx);
```

### Apply power to all enemies

```csharp
await PowerCmd.Apply<WeakPower>(CombatState.Enemies, 2, Owner.Creature, this);
```

### Draw cards

```csharp
await CardPileCmd.Draw(ctx, 2, Owner);
// or if card has a CardsVar:
await CommonActions.Draw(this, ctx);
```

### Create a token card and add to hand

```csharp
await CardPileCmd.AddGeneratedCardToCombat(new MyToken(), PileType.Hand, addedByPlayer: true);
```

### Check act number

```csharp
public override bool IsValidForAct(ActModel act) => act.ActNumber() is 1 or 2;
```
