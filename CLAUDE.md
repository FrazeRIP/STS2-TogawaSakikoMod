# STS2 Mod Development Guide

This is a Slay the Spire 2 mod written in C# targeting .NET with Godot. It uses **BaseLib 0.2.6** as the modding framework. All mod content is registered via BaseLib's `CustomContentDictionary` and the `[Pool]` attribute system.

## Project Structure

- Language: C# (.NET)
- Engine: Godot 4 (game engine, Godot types like `Node2D`, `Texture2D` available)
- Modding framework: BaseLib 0.2.6 (`BaseLib.dll`)
- Core game: STS2 (`sts2.dll`)

## Mod Initialization

Every mod needs an entry point class decorated with `[ModInitializer("Initialize")]`:

```csharp
using MegaCrit.Sts2.Core.Modding;
using HarmonyLib;

[ModInitializer("Initialize")]
public static class MyModMain
{
    public static void Initialize()
    {
        var harmony = new Harmony("MyMod.ModId");
        harmony.PatchAll();
        // Register configs, etc.
    }
}
```

---

## Core Namespaces

| Namespace | Contents |
|-----------|----------|
| `BaseLib.Abstracts` | All Custom* base classes and localization records |
| `BaseLib.Utils` | SpireField, WeightedList, TooltipSource, PoolAttribute |
| `BaseLib.Config` | ModConfig, SimpleModConfig |
| `BaseLib.Cards.Variables` | ExhaustiveVar, PersistVar, RefundVar |
| `BaseLib.Extensions` | DynamicVarExtensions, DynamicVarSetExtensions, ActModelExtensions |
| `BaseLib.Patches.Content` | CustomContentDictionary, CustomKeywords, CustomEnums |
| `MegaCrit.Sts2.Core.Models` | CardModel, PowerModel, RelicModel, PotionModel, MonsterModel, CharacterModel, AbstractModel, ModelDb |
| `MegaCrit.Sts2.Core.Commands` | DamageCmd, PowerCmd, CreatureCmd, CardPileCmd, CardSelectCmd |
| `MegaCrit.Sts2.Core.Localization.DynamicVars` | DynamicVar, DamageVar, BlockVar, PowerVar<T>, CardsVar, etc. |
| `MegaCrit.Sts2.Core.ValueProps` | ValueProp, DamageProps, BlockProps |
| `MegaCrit.Sts2.Core.Entities.Cards` | CardType, CardRarity, TargetType, CardKeyword, CardTag, PileType |
| `MegaCrit.Sts2.Core.Entities.Powers` | PowerType, PowerStackType |
| `MegaCrit.Sts2.Core.Entities.Relics` | RelicRarity |
| `MegaCrit.Sts2.Core.Entities.Potions` | PotionRarity, PotionUsage |
| `MegaCrit.Sts2.Core.Rooms` | RoomType |

---

## Key Enums

```csharp
// Cards
enum CardType    { None, Attack, Skill, Power, Status, Curse, Quest }
enum CardRarity  { None, Basic, Common, Uncommon, Rare, Ancient, Event, Token, Status, Curse, Quest }
enum TargetType  { None, Self, AnyEnemy, AllEnemies, RandomEnemy, AnyPlayer, AnyAlly, AllAllies, TargetedNoCreature, Osty }
enum CardKeyword { None, Exhaust, Ethereal, Innate, Unplayable, Retain, Sly, Eternal }
enum CardTag     { None, Strike, Defend, Minion, OstyAttack, Shiv }
enum PileType    { None, Draw, Hand, Discard, Exhaust, Play, Deck }

// Powers
enum PowerType      { None, Buff, Debuff }
enum PowerStackType { None, Counter, Single }

// Relics
enum RelicRarity { None, Starter, Common, Uncommon, Rare, Shop, Event, Ancient }

// Potions
enum PotionRarity { None, Common, Uncommon, Rare, Event, Token }
enum PotionUsage  { None, CombatOnly, AnyTime, Automatic }

// Rooms
enum RoomType { Unassigned, Monster, Elite, Boss, Treasure, Shop, Event, RestSite, Map }
```

---

## ValueProp (Damage/Block Flags)

```csharp
[Flags]
public enum ValueProp
{
    Unblockable = 2,
    Unpowered   = 4,
    Move        = 8,      // means "from a card or monster move"
    SkipHurtAnim = 0x10
}

// Convenience constants in DamageProps / BlockProps:
DamageProps.card           = ValueProp.Move                        // normal card damage
DamageProps.cardUnpowered  = ValueProp.Unpowered | ValueProp.Move // bypass STR/Weak
DamageProps.cardHpLoss     = ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move
DamageProps.monsterMove    = ValueProp.Move
DamageProps.nonCardUnpowered = ValueProp.Unpowered

BlockProps.card            = ValueProp.Move
BlockProps.cardUnpowered   = ValueProp.Unpowered | ValueProp.Move
```

---

## ICustomModel & ILocalizationProvider

```csharp
// Marker interface - all custom types implement this
public interface ICustomModel { }

// Provides localization key-value pairs returned from Localization property
public interface ILocalizationProvider
{
    string? LocTable => null;  // optional: override loc table name
    List<(string, string)>? Localization { get; }
}
```

---

## AbstractModel (sts2.dll base for all model types)

All game models inherit from `AbstractModel`. It provides:

```csharp
public abstract class AbstractModel : IComparable<AbstractModel>
{
    public ModelId Id { get; }
    public bool IsCanonical { get; }  // true = canonical/immutable instance
    public abstract bool ShouldReceiveCombatHooks { get; }

    // ALL combat hook methods (virtual, return Task.CompletedTask by default):
    public virtual Task AfterActEntered() { }
    public virtual Task BeforeAttack(AttackCommand command) { }
    public virtual Task AfterAttack(AttackCommand command) { }
    public virtual Task AfterBlockCleared(Creature creature) { }
    public virtual Task BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource) { }
    public virtual Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource) { }
    public virtual Task AfterBlockBroken(Creature creature) { }
    public virtual Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source) { }
    public virtual Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source) { }
    public virtual Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card) { }
    public virtual Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) { }
    public virtual Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) { }
    public virtual Task AfterCardEnteredCombat(CardModel card) { }
    public virtual Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer) { }
    public virtual Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal) { }
    public virtual Task BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type) { }
    public virtual Task BeforeCardPlayed(CardPlay cardPlay) { }
    public virtual Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay) { }
    public virtual Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay) { }
    public virtual Task AfterCardRetained(CardModel card) { }
    public virtual Task BeforeCombatStart() { }
    public virtual Task BeforeCombatStartLate() { }
    public virtual Task AfterCombatEnd(CombatRoom room) { }
    public virtual Task AfterCombatVictory(CombatRoom room) { }
    public virtual Task AfterCreatureAddedToCombat(Creature creature) { }
    public virtual Task AfterCurrentHpChanged(Creature creature, decimal delta) { }
    public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource) { }
    public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) { }
    public virtual Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource) { }
    public virtual Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource) { }
    public virtual Task BeforeDeath(Creature creature) { }
    public virtual Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength) { }
    public virtual Task AfterGoldGained(Player player) { }
    public virtual Task AfterEnergyReset(Player player) { }
    public virtual Task AfterEnergySpent(CardModel card, int amount) { }
    public virtual Task BeforeFlush(Player player) { }
    public virtual Task AfterForge(decimal amount, Player forger, AbstractModel? source) { }
    public virtual Task BeforeHandDraw(Player player, PlayerChoiceContext playerChoiceContext) { }
    public virtual Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player) { }
    public virtual Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent) { }
    public virtual Task AfterMapGenerated(ActMap map, int actIndex) { }
    public virtual Task AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay) { }
    public virtual Task AfterModifyingCardPlayCount(CardModel card) { }
    public virtual Task AfterModifyingDamageAmount(CardModel? cardSource) { }
    public virtual Task AfterModifyingHandDraw() { }
    public virtual Task AfterModifyingPowerAmountReceived(PowerModel power) { }
    public virtual Task AfterModifyingPowerAmountGiven(PowerModel power) { }
    public virtual Task AfterModifyingRewards() { }
    public virtual Task BeforeRewardsOffered(Player player, IReadOnlyList<Reward> rewards) { }
    public virtual Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb) { }
    public virtual Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets) { }
    public virtual Task AfterOstyRevived(Creature osty) { }
    public virtual Task BeforePotionUsed(PotionModel potion, Creature? target) { }
    public virtual Task AfterPotionUsed(PotionModel potion, Creature? target) { }
    public virtual Task AfterPotionDiscarded(PotionModel potion) { }
    public virtual Task AfterPotionProcured(PotionModel potion) { }
    public virtual Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource) { }
    public virtual Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource) { }
    public virtual Task AfterPreventingDeath(Creature creature) { }
    public virtual Task AfterRestSiteHeal(Player player, bool isMimicked) { }
    public virtual Task AfterRestSiteSmith(Player player) { }
    public virtual Task AfterRewardTaken(Player player, Reward reward) { }
    public virtual Task BeforeRoomEntered(AbstractRoom room) { }
    public virtual Task AfterRoomEntered(AbstractRoom room) { }
    public virtual Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler) { }
    public virtual Task AfterStarsSpent(int amount, Player spender) { }
    public virtual Task AfterStarsGained(int amount, Player gainer) { }
    public virtual Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount) { }
    public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState) { }
    public virtual Task AfterSideTurnStart(CombatSide side, CombatState combatState) { }
    public virtual Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player) { }
    public virtual Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) { }
    public virtual Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player) { }
    public virtual Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player) { }
    public virtual Task BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side) { }
    public virtual Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side) { }
    public virtual Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) { }
    public virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) { }
    public virtual Task AfterTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side) { }
}
```

---

## CardModel (sts2.dll)

```csharp
public abstract class CardModel : AbstractModel
{
    // Constructor
    protected CardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)

    // Identity
    public string Title { get; }
    public LocString Description { get; }
    public virtual CardType Type { get; }
    public virtual CardRarity Rarity { get; }
    public virtual TargetType TargetType { get; }
    public virtual CardPoolModel Pool { get; }

    // Cost
    public CardEnergyCost EnergyCost { get; }
    public int BaseStarCost { get; set; }
    public virtual int CanonicalStarCost => -1;  // -1 = no star cost

    // Dynamic Variables
    public DynamicVarSet DynamicVars { get; }
    protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    // Keywords/Tags
    public virtual IEnumerable<CardKeyword> CanonicalKeywords => Array.Empty<CardKeyword>();
    public IReadOnlySet<CardKeyword> Keywords { get; }
    public virtual IEnumerable<CardTag> Tags { get; }
    protected virtual HashSet<CardTag> CanonicalTags => new HashSet<CardTag>();

    // State
    public Player Owner { get; }
    public CombatState? CombatState { get; }
    public bool IsUpgraded { get; }
    public int CurrentUpgradeLevel { get; }
    public virtual int MaxUpgradeLevel => 1;
    public bool GainsBlock { get; }  // auto-detected from BlockVar presence

    // Hooks (override in cards)
    protected virtual Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.CompletedTask;
    public virtual Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
        => Task.CompletedTask;
    public virtual bool HasTurnEndInHandEffect => false;
    public override bool ShouldReceiveCombatHooks => Pile?.IsCombatPile ?? false;
}
```

---

## CustomCardModel (BaseLib)

```csharp
public abstract class CustomCardModel : CardModel, ICustomModel, ILocalizationProvider
{
    // Constructor - autoAdd=true automatically registers with pool via [Pool] attribute
    public CustomCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)

    // Overrides for block auto-detection
    public override bool GainsBlock { get; }  // true if any BlockVar or CalculatedBlockVar present

    // Custom visuals
    public virtual Texture2D? CustomFrame => null;
    public Material? CustomFrameMaterial { get; }
    public virtual Material? CreateCustomFrameMaterial => null;
    public virtual string? CustomPortraitPath => null;
    public virtual Texture2D? CustomPortrait => null;

    // Localization
    public virtual List<(string, string)>? Localization => null;
}
```

**Required attribute:** `[Pool(typeof(YourCardPoolModel))]` on the card class.

**Example card:**

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;

[Pool(typeof(MyCharacterCardPool))]
public class MyStrikeCard : CustomCardModel
{
    public MyStrikeCard() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new DamageVar(6, DamageProps.card).WithUpgrade(3) };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Strike", "Deal !Damage! damage.");
}
```

---

## ConstructedCardModel (BaseLib - Fluent Builder)

`ConstructedCardModel` extends `CustomCardModel` with a fluent API for defining variables and keywords. Use this when you want a cleaner card construction syntax.

```csharp
public abstract class ConstructedCardModel : CustomCardModel
{
    protected ConstructedCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)

    // Fluent builder methods (call in constructor):
    protected ConstructedCardModel WithDamage(int baseVal, int upgrade = 0);
    protected ConstructedCardModel WithBlock(int baseVal, int upgrade = 0);
    protected ConstructedCardModel WithCards(int baseVal, int upgrade = 0);
    protected ConstructedCardModel WithVars(params DynamicVar[] vars);
    protected ConstructedCardModel WithVar(string name, int baseVal, int upgrade = 0);
    protected ConstructedCardModel WithPower<T>(int baseVal, int upgrade = 0) where T : PowerModel;
    protected ConstructedCardModel WithPower<T>(string name, int baseVal, int upgrade = 0) where T : PowerModel;
    protected ConstructedCardModel WithCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus, ValueProp props = DamageProps.card, int upgrade = 0, int bonusUpgrade = 0);
    protected ConstructedCardModel WithCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus, ValueProp props = BlockProps.card, int upgrade = 0, int bonusUpgrade = 0);
    protected ConstructedCardModel WithKeywords(params CardKeyword[] keywords);
    protected ConstructedCardModel WithTags(params CardTag[] tags);
    protected ConstructedCardModel WithTip(TooltipSource tipSource);

    // Properties set from fluent calls (sealed):
    protected sealed override IEnumerable<DynamicVar> CanonicalVars { get; }
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords { get; }
    protected sealed override HashSet<CardTag> CanonicalTags { get; }
}
```

**Example ConstructedCardModel:**

```csharp
[Pool(typeof(MyCharacterCardPool))]
public class MyPowerfulStrike : ConstructedCardModel
{
    public MyPowerfulStrike() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(12, upgrade: 4)
            .WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Powerful Strike", "Deal !Damage! damage. Exhaust.");
}
```

---

## PowerModel (sts2.dll)

```csharp
public abstract class PowerModel : AbstractModel
{
    // Required overrides
    public abstract PowerType Type { get; }           // Buff, Debuff, or None
    public abstract PowerStackType StackType { get; } // Counter (stackable) or Single

    // Common properties
    public int Amount { get; set; }
    public int AmountOnTurnStart { get; }
    public virtual int DisplayAmount => Amount;
    public virtual bool AllowNegative => false;
    public virtual bool IsInstanced => false;
    public Creature Owner { get; }
    public Creature? Applier { get; set; }
    public Creature? Target { get; set; }
    public DynamicVarSet DynamicVars { get; }
    protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    // Localization
    public virtual LocString Title => new LocString("powers", Id.Entry + ".title");
    public virtual LocString Description => new LocString("powers", Id.Entry + ".description");

    // Hooks (override these)
    public virtual Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
        => Task.CompletedTask;
    public virtual Task AfterApplied(Creature? applier, CardModel? cardSource)
        => Task.CompletedTask;
    public virtual Task AfterRemoved(Creature oldOwner)
        => Task.CompletedTask;
    public virtual bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    // All AbstractModel hooks are also available (AfterTurnEnd, AfterCardPlayed, etc.)
    public override bool ShouldReceiveCombatHooks => true;
}
```

---

## CustomPowerModel (BaseLib)

```csharp
public abstract class CustomPowerModel : PowerModel, ICustomPower, ICustomModel, ILocalizationProvider
{
    public virtual string? CustomPackedIconPath => null;   // path to .tres atlas sprite
    public virtual string? CustomBigIconPath => null;      // path to .png
    public virtual string? CustomBigBetaIconPath => null;
    public virtual List<(string, string)>? Localization => null;
}
```

**Example power:**

```csharp
public class MyStrengthBuff : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            Title: "My Strength",
            Description: "At the start of your turn, gain !Amount! Strength.",
            SmartDescription: "At the start of your turn, gain {Amount} Strength."
        );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
    }
}
```

---

## RelicModel (sts2.dll)

```csharp
public abstract class RelicModel : AbstractModel
{
    // Required
    public abstract RelicRarity Rarity { get; }

    // Properties
    public Player Owner { get; set; }
    public DynamicVarSet DynamicVars { get; }
    protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
    public virtual bool IsUsedUp => false;
    public virtual bool ShowCounter => false;
    public virtual int DisplayAmount => 0;
    public virtual bool IsStackable => false;
    public int FloorAddedToDeck { get; set; }
    public bool IsWax { get; set; }
    public bool IsMelted { get; set; }

    // Localization
    public virtual LocString Title { get; }
    public LocString Description { get; }
    public LocString Flavor { get; }

    // Hooks
    public virtual Task AfterObtained() => Task.CompletedTask;
    public virtual Task AfterRemoved() => Task.CompletedTask;
    public override bool ShouldReceiveCombatHooks => true;
    // All AbstractModel hooks available
}
```

---

## CustomRelicModel (BaseLib)

```csharp
public abstract class CustomRelicModel : RelicModel, ICustomModel, ILocalizationProvider
{
    public CustomRelicModel(bool autoAdd = true)

    public virtual List<(string, string)>? Localization => null;
    public virtual RelicModel? GetUpgradeReplacement() => null;
}
```

**Required attribute:** `[Pool(typeof(YourRelicPoolModel))]`

**Example relic:**

```csharp
[Pool(typeof(MyCharacterRelicPool))]
public class MyStarterRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            Title: "My Starter Relic",
            Description: "At the start of each combat, gain 1 energy.",
            Flavor: "Power from within."
        );

    public override async Task BeforeCombatStart()
    {
        // gain energy logic
    }
}
```

---

## PotionModel (sts2.dll)

```csharp
public abstract class PotionModel : AbstractModel
{
    // Required
    public abstract PotionRarity Rarity { get; }
    public abstract PotionUsage Usage { get; }
    public abstract TargetType TargetType { get; }

    // Properties
    public Player Owner { get; }
    public DynamicVarSet DynamicVars { get; }
    protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    // Localization
    public LocString Title { get; }
    public LocString Description { get; }

    // Hook
    protected virtual Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
        => Task.CompletedTask;
    public override bool ShouldReceiveCombatHooks => true;
}
```

---

## CustomPotionModel (BaseLib)

```csharp
public abstract class CustomPotionModel : PotionModel, ICustomModel, ILocalizationProvider
{
    public CustomPotionModel(bool autoAdd = true)

    public virtual string? CustomPackedImagePath => null;
    public virtual string? CustomPackedOutlinePath => null;
    public virtual List<(string, string)>? Localization => null;
}
```

**Required attribute:** `[Pool(typeof(YourPotionPoolModel))]`

---

## MonsterModel (sts2.dll)

```csharp
public abstract class MonsterModel : AbstractModel
{
    // Required
    public abstract int MinInitialHp { get; }
    public abstract int MaxInitialHp { get; }
    protected abstract MonsterMoveStateMachine GenerateMoveStateMachine();

    // Properties
    public virtual bool IsHealthBarVisible => true;
    public Creature Creature { get; }
    public CombatState CombatState { get; }
    public virtual LocString Title { get; }
    public override bool ShouldReceiveCombatHooks => true;
}
```

---

## CustomMonsterModel (BaseLib)

```csharp
public abstract class CustomMonsterModel : MonsterModel, ICustomModel, ISceneConversions
{
    public virtual string? CustomVisualPath => null;
    public virtual string? CustomAttackSfx => null;
    public virtual string? CustomCastSfx => null;
    public virtual string? CustomDeathSfx => null;

    public virtual NCreatureVisuals? CreateCustomVisuals() => null;
    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null;

    // Static helper to set up animation states
    public static CreatureAnimator SetupAnimationState(
        MegaSprite controller,
        string idleName,
        string? deadName = null, bool deadLoop = false,
        string? hitName = null, bool hitLoop = false,
        string? attackName = null, bool attackLoop = false,
        string? castName = null, bool castLoop = false);

    public void RegisterSceneConversions();
}
```

---

## CustomPetModel (BaseLib)

```csharp
// Pets are monsters that do nothing by default (sit at the player's side)
public abstract class CustomPetModel(bool visibleHp) : CustomMonsterModel()
{
    public override bool IsHealthBarVisible => visibleHp;
    // GenerateMoveStateMachine is already implemented (does nothing)
}
```

---

## CharacterModel (sts2.dll)

```csharp
public abstract class CharacterModel : AbstractModel
{
    // Required
    public abstract Color NameColor { get; }
    public abstract CharacterGender Gender { get; }
    public abstract int StartingHp { get; }
    public abstract int StartingGold { get; }
    public abstract CardPoolModel CardPool { get; }
    public abstract RelicPoolModel RelicPool { get; }
    public abstract PotionPoolModel PotionPool { get; }
    public abstract IEnumerable<CardModel> StartingDeck { get; }
    public abstract IReadOnlyList<RelicModel> StartingRelics { get; }
    public abstract float AttackAnimDelay { get; }
    public abstract float CastAnimDelay { get; }
    protected abstract CharacterModel? UnlocksAfterRunAs { get; }

    // Optional
    public override int StartingGold => 99;      // default
    public virtual int MaxEnergy => 3;
    public virtual int BaseOrbSlotCount => 0;
    public virtual IReadOnlyList<PotionModel> StartingPotions => Array.Empty<PotionModel>();

    // Localization
    public LocString Title { get; }
    public LocString TitleObject { get; }
    public LocString PronounObject { get; }
    public LocString PronounSubject { get; }
    public LocString PronounPossessive { get; }
    public LocString PossessiveAdjective { get; }
}

public enum CharacterGender { Male, Female, NonBinary }
```

---

## CustomCharacterModel (BaseLib)

```csharp
public abstract class CustomCharacterModel : CharacterModel, ICustomModel, ILocalizationProvider, ISceneConversions
{
    public CustomCharacterModel()  // auto-registers via ModelDbCustomCharacters.Register

    // Visual paths (all optional - provide custom assets)
    public virtual string? CustomVisualPath => null;
    public virtual string? CustomTrailPath => null;
    public virtual string? CustomIconTexturePath => null;
    public virtual string? CustomIconPath => null;
    public virtual Control? CustomIcon => null;
    public virtual CustomEnergyCounter? CustomEnergyCounter => null;
    public virtual string? CustomEnergyCounterPath => null;
    public virtual string? CustomRestSiteAnimPath => null;
    public virtual string? CustomMerchantAnimPath => null;
    public virtual string? CustomArmPointingTexturePath => null;
    public virtual string? CustomArmRockTexturePath => null;
    public virtual string? CustomArmPaperTexturePath => null;
    public virtual string? CustomArmScissorsTexturePath => null;
    public virtual string? CustomCharacterSelectBg => null;
    public virtual string? CustomCharacterSelectIconPath => null;
    public virtual string? CustomCharacterSelectLockedIconPath => null;
    public virtual string? CustomCharacterSelectTransitionPath => null;
    public virtual string? CustomMapMarkerPath => null;
    public virtual string? CustomAttackSfx => null;
    public virtual string? CustomCastSfx => null;
    public virtual string? CustomDeathSfx => null;

    // Override defaults
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;
    protected override CharacterModel? UnlocksAfterRunAs => null;

    // Localization
    public virtual List<(string, string)>? Localization => null;

    // Visual/animation creation
    public virtual NCreatureVisuals? CreateCustomVisuals() => null;
    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null;

    // Static helper (same as CustomMonsterModel but adds Relaxed state)
    public static CreatureAnimator SetupAnimationState(
        MegaSprite controller, string idleName,
        string? deadName = null, bool deadLoop = false,
        string? hitName = null, bool hitLoop = false,
        string? attackName = null, bool attackLoop = false,
        string? castName = null, bool castLoop = false,
        string? relaxedName = null, bool relaxedLoop = true);

    public void RegisterSceneConversions();
}
```

---

## Pool Models

### CustomCardPoolModel

```csharp
public abstract class CustomCardPoolModel : CardPoolModel, ICustomModel, ICustomEnergyIconPool
{
    public override string CardFrameMaterialPath => "card_frame_red";
    public virtual Color ShaderColor => new Color("FFFFFF");
    public virtual bool IsShared => false;   // true = appears in ALL characters' pools
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;
    public virtual Texture2D? CustomFrame(CustomCardModel card) => null;
    protected override CardModel[] GenerateAllCards() => Array.Empty<CardModel>();
    // Cards are added automatically via [Pool] attribute + autoAdd=true
}
```

### CustomRelicPoolModel

```csharp
public abstract class CustomRelicPoolModel : RelicPoolModel, ICustomModel, ICustomEnergyIconPool
{
    public virtual bool IsShared => false;
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;
    protected override IEnumerable<RelicModel> GenerateAllRelics() => Array.Empty<RelicModel>();
    // Relics added via [Pool] attribute + autoAdd=true
}
```

### CustomPotionPoolModel

```csharp
public abstract class CustomPotionPoolModel : PotionPoolModel, ICustomModel, ICustomEnergyIconPool
{
    public virtual bool IsShared => false;
    public virtual string? BigEnergyIconPath => null;
    public virtual string? TextEnergyIconPath => null;
    protected override IEnumerable<PotionModel> GenerateAllPotions() => Array.Empty<PotionModel>();
}
```

---

## CustomEncounterModel (BaseLib)

```csharp
public abstract class CustomEncounterModel : EncounterModel, ICustomModel
{
    protected CustomEncounterModel(RoomType roomType, bool autoAdd = true)
    // roomType: RoomType.Monster, RoomType.Elite, or RoomType.Boss

    public override RoomType RoomType { get; }
    public virtual string? CustomScenePath => null;
    public abstract bool IsValidForAct(ActModel act);
    public virtual BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => null;
}
```

---

## CustomEventModel (BaseLib)

```csharp
public abstract class CustomEventModel : EventModel, ICustomModel, ILocalizationProvider
{
    public virtual List<(string, string)>? Localization => null;
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        => throw new NotImplementedException();
}
```

---

## CustomOrbModel (BaseLib)

```csharp
public abstract class CustomOrbModel : OrbModel, ICustomModel, ILocalizationProvider
{
    public virtual string? CustomIconPath => null;
    public virtual string? CustomSpritePath => null;
    public virtual bool IncludeInRandomPool => false;
    public virtual string? CustomPassiveSfx => null;
    public virtual string? CustomEvokeSfx => null;
    public virtual string? CustomChannelSfx => null;
    public virtual List<(string, string)>? Localization => null;
    public virtual Node2D? CreateCustomSprite() => null;
    // Constructor auto-adds to RegisteredOrbs list
}
```

---

## CustomAncientModel (BaseLib)

For creating custom Ancient Shrine events.

```csharp
public abstract class CustomAncientModel : AncientEventModel, ICustomModel, ILocalizationProvider
{
    public CustomAncientModel(bool autoAdd = true, bool logDialogueLoad = false)

    protected abstract OptionPools MakeOptionPools { get; }
    public OptionPools OptionPools { get; }

    public virtual List<(string, string)>? Localization => null;
    public virtual string? CustomScenePath => null;
    public virtual string? CustomMapIconPath => null;
    public virtual string? CustomMapIconOutlinePath => null;
    public virtual string? CustomRunHistoryIconPath => null;
    public virtual string? CustomRunHistoryIconOutlinePath => null;

    public virtual bool IsValidForAct(ActModel act) => true;
    public virtual bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;

    // Static helpers for building option pools
    public static WeightedList<AncientOption> MakePool(params RelicModel[] options);
    public static WeightedList<AncientOption> MakePool(params AncientOption[] options);
    public static AncientOption AncientOption<T>(
        int weight = 1,
        Func<T, RelicModel>? relicPrep = null,
        Func<T, IEnumerable<RelicModel>>? makeAllVariants = null) where T : RelicModel;
}
```

---

## CustomTemporaryPowerModel (BaseLib)

For powers that expire after N turns.

```csharp
public abstract class CustomTemporaryPowerModel : CustomPowerModel, ITemporaryPower
{
    protected abstract Func<Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc { get; }
    public abstract PowerModel InternallyAppliedPower { get; }
    public abstract AbstractModel OriginModel { get; }
    protected virtual bool UntilEndOfOtherSideTurn => false;
    protected virtual int LastForXExtraTurns => 0;

    public override PowerType Type => InternallyAppliedPower.Type;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;
    public override bool IsInstanced => LastForXExtraTurns != 0;

    // Lifecycle - handles auto-removal, don't override unless you know what you're doing
    public override Task BeforeApplied(...);
    public override Task AfterPowerAmountChanged(...);
    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side);
}
```

---

## CustomPile (BaseLib)

For custom card piles displayed in combat.

```csharp
public abstract class CustomPile : CardPile
{
    public CustomPile(PileType pileType)

    public virtual bool NeedsCustomTransitionVisual => false;
    public abstract bool CardShouldBeVisible(CardModel card);
    public abstract Vector2 GetTargetPosition(CardModel model, Vector2 size);
    public virtual NCard? GetNCard(CardModel card) => null;
    public virtual bool CustomTween(Tween tween, CardModel card, NCard cardNode, CardPile oldPile) => false;
}
```

---

## Localization Records

All localization types are C# records with implicit conversion to `List<(string, string)>`. Return them from the `Localization` property.

### CardLoc

```csharp
public record CardLoc(string Title, string Description, params (string, string)[] ExtraLoc)
// Usage:
public override List<(string, string)>? Localization =>
    new CardLoc("Strike", "Deal !Damage! damage.");
// With extra keys:
new CardLoc("Strike", "Deal !Damage! damage.", ("upgradedDescription", "Deal !Damage! damage. Upgraded."));
```

### PowerLoc

```csharp
public record PowerLoc(string Title, string Description, string SmartDescription, params (string, string)[] ExtraLoc)
// Description = shown outside combat (static text ok)
// SmartDescription = shown in combat (can include dynamic vars like {Amount})
public override List<(string, string)>? Localization =>
    new PowerLoc("Strength", "Increases attack damage by !Amount!.", "Increases attack damage by {Amount}.");
```

### RelicLoc

```csharp
public record RelicLoc(string Title, string Description, string Flavor, params (string, string)[] ExtraLoc)
public override List<(string, string)>? Localization =>
    new RelicLoc("Burning Blood", "At the end of combat, heal 6 HP.", "It still burns.");
```

### PotionLoc

```csharp
public record PotionLoc(string Title, string Description, params (string, string)[] ExtraLoc)
public override List<(string, string)>? Localization =>
    new PotionLoc("Attack Potion", "Gain 2 !Damage!.");
```

### CharacterLoc

```csharp
public record CharacterLoc(
    string Title, string TitleObject, string Description,
    string PronounObject, string PronounSubject, string PronounPossessive, string PossessiveAdjective,
    string AromaPrinciple, string EndTurnPingAlive, string EndTurnPingDead,
    string EventDeathPrevention, string GoldMonologue,
    string CardsModifierTitle, string CardsModifierDescription,
    params (string, string)[] ExtraLoc)
```

### MonsterLoc

```csharp
public record MonsterLoc(string Name, IEnumerable<(string, string)> MoveTitles, params (string, string)[] ExtraLoc)
// MoveTitles: list of (moveId, localizedTitle) pairs
// These get stored as "moves.{moveId}.title" in the loc table
public override List<(string, string)>? Localization =>
    new MonsterLoc("Goblin",
        new[] { ("ATTACK", "Scratch"), ("DEFEND", "Cower") });
```

### EncounterLoc

```csharp
public record EncounterLoc(string Title, string LossText, params (string, string)[] ExtraLoc)
public override List<(string, string)>? Localization =>
    new EncounterLoc("Goblin Camp", "The goblins overwhelm you...");
```

### OrbLoc

```csharp
public record OrbLoc(string Title, string Description, string SmartDescription, params (string, string)[] ExtraLoc)
```

---

## Dynamic Variables (DynamicVar)

Dynamic variables are used in card/power/relic descriptions and for game calculations.

### Core DynamicVar

```csharp
public class DynamicVar
{
    public string Name { get; }
    public decimal BaseValue { get; set; }
    public decimal PreviewValue { get; set; }
    public int IntValue => (int)BaseValue;
    public bool WasJustUpgraded { get; }

    public DynamicVar(string name, decimal baseValue)
    public void UpgradeValueBy(decimal addend);  // use for upgrades
    public void FinalizeUpgrade();

    // BaseLib extension:
    public DynamicVar WithUpgrade(decimal upgradeValue);  // from DynamicVarExtensions
}
```

### Standard DynamicVar Types

```csharp
// Damage variable (shows modified by Strength/Weak/etc.)
new DamageVar(decimal damage, ValueProp props)         // name = "Damage"
new DamageVar(string name, decimal damage, ValueProp props)

// Block variable (shows modified by Dexterity/etc.)
new BlockVar(decimal block, ValueProp props)           // name = "Block"
new BlockVar(string name, decimal block, ValueProp props)

// Cards variable (number of cards)
new CardsVar(int count)                               // name = "Cards"

// Power variable (amount to apply, also adds hover tip for that power)
new PowerVar<T>(decimal amount)                       // name = typeof(T).Name e.g. "StrengthPower"
new PowerVar<T>(string name, decimal amount)

// Generic numeric variable
new DynamicVar("MyVar", 5m)

// Calculated damage (base + multiplier * bonus function)
// Use ConstructedCardModel.WithCalculatedDamage() instead

// Bool variable (for flags)
new BoolVar("MyFlag", false)
```

### DynamicVarSet Accessors

`DynamicVarSet` is the collection returned by `card.DynamicVars` / `power.DynamicVars` / `relic.DynamicVars`:

```csharp
// Named accessors (throw if not present):
DynamicVarSet vars = card.DynamicVars;
vars.Damage              // DamageVar
vars.Block               // BlockVar
vars.CalculatedDamage    // CalculatedDamageVar
vars.CalculatedBlock     // CalculatedBlockVar
vars.Cards               // CardsVar
vars.Energy              // EnergyVar
vars.Repeat              // RepeatVar
vars.Strength            // PowerVar<StrengthPower>
vars.Weak                // PowerVar<WeakPower>
vars.Vulnerable          // PowerVar<VulnerablePower>
vars.Poison              // PowerVar<PoisonPower>
vars.Doom                // PowerVar<DoomPower>
vars.Dexterity           // PowerVar<DexterityPower>
vars["MyVar"]            // DynamicVar by key (throws if missing)
vars.ContainsKey("key")  // bool - check if key exists

// BaseLib extension for custom powers:
vars.Power<MyCustomPower>()  // PowerVar<MyCustomPower> by type name key
```

### Description Interpolation

In localization strings, `!VarName!` interpolates the preview value of a DynamicVar with formatting. The name used in the localization string must match the `DynamicVar.Name` property:

```
"Deal !Damage! damage."         // uses DamageVar named "Damage"
"Apply !StrengthPower! Strength." // uses PowerVar<StrengthPower>
"Draw !Cards! cards."           // uses CardsVar named "Cards"
"Gain !MyVar! gold."            // uses DynamicVar named "MyVar"
```

---

## Card Variables (BaseLib-specific)

### ExhaustiveVar

A card can only be played N times per combat before it exhausts itself.

```csharp
public class ExhaustiveVar : DynamicVar
{
    public const string Key = "Exhaustive";
    public ExhaustiveVar(decimal exhaustiveCount) : base("Exhaustive", exhaustiveCount)
    // Automatically adds tooltip hover tip
}
// Usage in card:
protected override IEnumerable<DynamicVar> CanonicalVars =>
    new DynamicVar[] { new DamageVar(8, DamageProps.card), new ExhaustiveVar(3) };
```

### PersistVar

A card can only be played N times per turn before it exhausts.

```csharp
public class PersistVar : DynamicVar
{
    public const string Key = "Persist";
    public PersistVar(decimal persistCount) : base("Persist", persistCount)
}
```

### RefundVar

A card refunds N energy when played.

```csharp
public class RefundVar : DynamicVar
{
    public const string Key = "Refund";
    public RefundVar(decimal refundAmount) : base("Refund", refundAmount)
}
```

---

## Pool Registration System

Cards, relics, and potions are added to their pool via:

1. Decorate the class with `[Pool(typeof(YourPoolType))]` from `BaseLib.Utils`
2. Use `autoAdd = true` in the constructor (default)
3. BaseLib calls `CustomContentDictionary.AddModel(type)` which calls `ModHelper.AddModelToPool(poolType, modelType)`

```csharp
// PoolAttribute from BaseLib.Utils:
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class PoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}

// The pool and model types must be compatible:
// CardModel types go into CardPoolModel subclasses
// RelicModel types go into RelicPoolModel subclasses
// PotionModel types go into PotionPoolModel subclasses
```

---

## CommonActions (BaseLib Utility)

```csharp
public static class CommonActions
{
    // Attack using card's damage variable (auto-detects Damage or CalculatedDamage)
    public static AttackCommand CardAttack(CardModel card, CardPlay play, int hitCount = 1,
        string? vfx = null, string? sfx = null, string? tmpSfx = null);
    public static AttackCommand CardAttack(CardModel card, Creature? target, int hitCount = 1, ...);
    public static AttackCommand CardAttack(CardModel card, Creature? target, decimal damage, int hitCount = 1, ...);

    // Block using card's Block variable
    public static Task<decimal> CardBlock(CardModel card, CardPlay play);
    public static Task<decimal> CardBlock(CardModel card, BlockVar blockVar, CardPlay play);

    // Draw using card's Cards variable
    public static Task<IEnumerable<CardModel>> Draw(CardModel card, PlayerChoiceContext context);

    // Apply power using card's PowerVar<T>
    public static Task<T?> Apply<T>(Creature target, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel;
    public static Task<T?> Apply<T>(Creature target, CardModel card, bool silent = false) where T : PowerModel;
    public static Task<T?> Apply<T>(Creature target, CardModel? card, decimal amount, bool silent = false) where T : PowerModel;
    public static Task<T?> ApplySelf<T>(CardModel card, bool silent = false) where T : PowerModel;
    public static Task<T?> ApplySelf<T>(CardModel card, decimal amount, bool silent = false) where T : PowerModel;

    // Card selection
    public static Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString prompt,
        PlayerChoiceContext context, PileType pileType, int count = 1);
    public static Task<CardModel?> SelectSingleCard(CardModel card, LocString prompt,
        PlayerChoiceContext context, PileType pileType);
}
```

---

## Command Classes (Direct Game Commands)

### DamageCmd

```csharp
public static class DamageCmd
{
    public static AttackCommand Attack(decimal damagePerHit);
    public static AttackCommand Attack(CalculatedDamageVar calculatedDamageVar);
}
// AttackCommand fluent API:
// .WithHitCount(int n)
// .FromCard(CardModel card)
// .Targeting(Creature target)
// .TargetingAllOpponents(CombatState state)
// .TargetingRandomOpponents(CombatState state, bool unique)
// .WithHitFx(string? vfx, string? sfx, string? tmpSfx)
// .Execute(PlayerChoiceContext ctx)  -- await this
```

### PowerCmd

```csharp
public static class PowerCmd
{
    public static Task<T?> Apply<T>(Creature target, decimal amount, Creature? applier,
        CardModel? cardSource, bool silent = false) where T : PowerModel;
    public static Task<IReadOnlyList<T>> Apply<T>(IEnumerable<Creature> targets, decimal amount,
        Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel;
    public static Task Apply(PowerModel power, Creature target, decimal amount, Creature? applier,
        CardModel? cardSource, bool silent = false);
    public static Task Remove<T>(Creature creature) where T : PowerModel;
    public static Task Remove(PowerModel? power);
    public static Task<int> ModifyAmount(PowerModel power, decimal offset, Creature? applier,
        CardModel? cardSource, bool silent = false);
}
```

### CreatureCmd

```csharp
public static class CreatureCmd
{
    public static Task<Creature> Add<T>(CombatState combatState, string? slotName = null) where T : MonsterModel;
    public static Task<Creature> Add(MonsterModel monster, CombatState state,
        CombatSide side = CombatSide.Enemy, string? slotName = null);
    public static Task<decimal> GainBlock(Creature creature, BlockVar blockVar, CardPlay? cardPlay, bool fast = false);
    public static Task<decimal> GainBlock(Creature creature, decimal amount, ValueProp props,
        CardPlay? cardPlay, bool fast = false);
    public static Task LoseBlock(Creature creature, decimal amount);
    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext ctx, Creature target,
        DamageVar damageVar, CardModel cardSource);
    public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext ctx, Creature target,
        decimal amount, ValueProp props, CardModel? cardSource);
    public static Task Kill(Creature creature, bool force = false);
}
```

### CardPileCmd

```csharp
public static class CardPileCmd
{
    public static Task<IEnumerable<CardModel>> Draw(PlayerChoiceContext ctx, decimal count, Player player,
        bool fromHandDraw = false);
    public static Task<CardPileAddResult> Add(CardModel card, PileType newPileType,
        CardPilePosition position = CardPilePosition.Bottom, AbstractModel? source = null, bool skipVisuals = false);
    public static Task<CardPileAddResult> AddGeneratedCardToCombat(CardModel card, PileType newPileType,
        bool addedByPlayer, CardPilePosition position = CardPilePosition.Bottom);
    public static Task RemoveFromDeck(CardModel card, bool showPreview = true);
    public static Task RemoveFromCombat(CardModel card, bool skipVisuals = false);
    public static Task Shuffle(PlayerChoiceContext ctx, Player player);
    public static Task AddCurseToDeck<T>(Player owner) where T : CardModel;
    public static Task AddToCombatAndPreview<T>(Creature target, PileType pileType, int count,
        bool addedByPlayer, CardPilePosition position = CardPilePosition.Bottom) where T : CardModel;
}
```

### CardSelectCmd

```csharp
public static class CardSelectCmd
{
    public static Task<IEnumerable<CardModel>> FromSimpleGrid(PlayerChoiceContext ctx,
        IEnumerable<CardModel> cards, Player owner, CardSelectorPrefs prefs);
}
// CardSelectorPrefs(LocString prompt, int count)
// CardSelectorPrefs(LocString prompt, int minCount, int maxCount)
```

---

## SpireField (BaseLib Utility)

`SpireField<TKey, TVal>` is a ConditionalWeakTable-backed field for attaching extra data to existing game objects (similar to the SpireField concept from STS1 modding).

```csharp
public class SpireField<TKey, TVal> where TKey : class
{
    public SpireField(Func<TVal?> defaultVal);        // default factory (ignores key)
    public SpireField(Func<TKey, TVal?> defaultVal);   // default factory (receives key)

    public TVal? this[TKey obj] { get; set; }  // get/set value for a specific object
    public TVal? Get(TKey obj);
    public void Set(TKey obj, TVal? val);
}

// SavedSpireField<TKey, TVal> - persisted across saves
// Supported TVal types: int, bool, string, ModelId, Enum, SerializableCard, arrays of those, List<SerializableCard>
public class SavedSpireField<TKey, TVal> : SpireField<TKey, TVal>, ISavedSpireField
{
    public SavedSpireField(Func<TVal?> defaultVal, string name);
    public SavedSpireField(Func<TKey, TVal?> defaultVal, string name);
    // name becomes "{TKey.Name}_{name}" for save key uniqueness
}
```

**Usage:**

```csharp
// Attach extra data to a CardModel
public static readonly SpireField<CardModel, int> TimesPlayed =
    new SpireField<CardModel, int>(() => 0);

// Read/write:
int count = TimesPlayed[card];
TimesPlayed[card] = count + 1;

// Saved version (persisted):
public static readonly SavedSpireField<RelicModel, bool> HasTriggered =
    new SavedSpireField<RelicModel, bool>(() => false, "HasTriggered");
```

---

## WeightedList (BaseLib Utility)

```csharp
public class WeightedList<T> : IList<T>
{
    public int Count { get; }
    public T this[int index] { get; set; }

    public void Add(T item);                    // weight = 1, or item.Weight if IWeighted
    public void Add(T item, int weight);
    public void Insert(int index, T item, int weight = 1);
    public bool Remove(T val);
    public void RemoveAt(int index);
    public void Clear();

    public T GetRandom(Rng rng);
    public T GetRandom(Rng rng, bool remove);   // remove after drawing
}

// Items can implement IWeighted for automatic weighting:
public interface IWeighted { int Weight { get; } }
```

---

## TooltipSource (BaseLib Utility)

```csharp
public class TooltipSource
{
    public TooltipSource(Func<CardModel, IHoverTip> tip);
    public IHoverTip Tip(CardModel card);

    // Implicit conversions:
    public static implicit operator TooltipSource(Type t);          // from PowerModel/CardModel/PotionModel type
    public static implicit operator TooltipSource(CardKeyword kw);  // from CardKeyword
    public static implicit operator TooltipSource(StaticHoverTip tip);
}
// Used in ConstructedCardModel.WithTip()
```

---

## Config System

### ModConfig (abstract base)

```csharp
public abstract class ModConfig
{
    // Constructor - auto-detects config file path from namespace or explicit filename
    public ModConfig(string? filename = null)

    // Required override
    public abstract void SetupConfigUI(Control optionContainer);

    // Properties exposed as config options must be:
    // - public static (not instance)
    // - have both getter and setter
    // - NOT decorated with [ConfigIgnore]

    public string ModPrefix { get; }
    public bool HasSettings();
    public bool HasVisibleSettings();
    public void Save();
    public void Load();
    public void SaveDebounced(int delayMs = 1000);
    public void Changed();  // invoke after programmatic property changes

    // Static access
    public static void Load<T>() where T : ModConfig;
    public static void SaveDebounced<T>(int delayMs = 1000) where T : ModConfig;
}
```

**Attributes for config properties:**

```csharp
[ConfigIgnore]          // exclude from config UI and file
[ConfigHideInUI]        // include in file but hide from UI
[SliderRange(min, max)] // for double properties shown as slider
[SliderLabelFormat("0.00")] // format string for slider label
[ConfigSection("Header")] // section header above this property
[ConfigButton("ButtonLabel")] // mark a method as a config button
[ConfigVisibleWhen("OtherProp")] // only show when OtherProp is true
[HoverTipsByDefault]    // add hover tips to all options by default
```

**Registration:** Call `ModConfigRegistry.Register("MyModId", new MyConfig())` in your mod initializer.

### SimpleModConfig (BaseLib - ready-to-use)

`SimpleModConfig` auto-generates UI from public static properties. Most mods should use this.

```csharp
public class SimpleModConfig : ModConfig
{
    // Auto-generates UI for all public static properties:
    // bool → toggle
    // double → slider (use [SliderRange])
    // string → text input
    // Enum → dropdown

    public override void SetupConfigUI(Control optionContainer)
        // default: GenerateOptionsForAllProperties(optionContainer) + restore defaults button

    // Override to customize UI layout (call base methods as needed):
    protected NConfigOptionRow CreateToggleOption(PropertyInfo property, bool addHoverTip = false);
    protected NConfigOptionRow CreateSliderOption(PropertyInfo property, bool addHoverTip = false);
    protected NConfigOptionRow CreateDropdownOption(PropertyInfo property, bool addHoverTip = false);
    protected NConfigOptionRow CreateLineEditOption(PropertyInfo property, bool addHoverTip = false);
    protected NConfigOptionRow CreateButton(string rowLabelKey, string buttonLabelKey, Action onPressed, bool addHoverTip = false);
    protected MarginContainer CreateSectionHeader(string labelName, bool alignToTop = false);
}
```

**Example config:**

```csharp
public class MyModConfig : SimpleModConfig
{
    public static bool EnableBonusEffects { get; set; } = true;
    public static double DamageMultiplier { get; set; } = 1.0;
    public static MyDifficultyEnum Difficulty { get; set; } = MyDifficultyEnum.Normal;
}

// In mod initializer:
ModConfigRegistry.Register("MyMod", new MyModConfig());

// Access anywhere:
if (MyModConfig.EnableBonusEffects) { ... }
```

---

## CustomEnums (BaseLib)

For adding new values to existing STS2 enums at runtime:

```csharp
public static class CustomEnums
{
    // Generate a unique key for a new enum value
    public static object GenerateKey(Type enumType);
}

// Typically used with [CustomEnum] attribute and Harmony patches
// Example: adding a new CardTag value
public static class MyTags
{
    public static readonly CardTag MyCustomTag =
        (CardTag)CustomEnums.GenerateKey(typeof(CardTag));
}
```

---

## CustomKeywords (BaseLib)

```csharp
public static class CustomKeywords
{
    public readonly struct KeywordInfo
    {
        public readonly string Key;
        public readonly AutoKeywordPosition AutoPosition;
        public static implicit operator string(KeywordInfo info) => info.Key;
    }

    // Dictionary of registered custom keyword IDs
    public static readonly Dictionary<int, KeywordInfo> KeywordIDs;
}
```

---

## ModelDb (sts2.dll - Static Game Database)

```csharp
public static class ModelDb
{
    // All model collections
    public static IEnumerable<CardModel> AllCards { get; }
    public static IEnumerable<CardPoolModel> AllCardPools { get; }
    public static IEnumerable<CharacterModel> AllCharacters { get; }
    public static IEnumerable<MonsterModel> Monsters { get; }
    public static IEnumerable<EncounterModel> AllEncounters { get; }
    public static IEnumerable<PowerModel> AllPowers { get; }
    public static IEnumerable<RelicModel> AllRelics { get; }
    public static IEnumerable<RelicPoolModel> AllRelicPools { get; }
    public static IEnumerable<PotionModel> AllPotions { get; }
    public static IEnumerable<PotionPoolModel> AllPotionPools { get; }
    public static IEnumerable<EventModel> AllEvents { get; }
    public static IEnumerable<ActModel> Acts { get; }
    public static IEnumerable<OrbModel> Orbs { get; }

    // Lookup helpers
    public static ModelId GetId<T>() where T : AbstractModel;
    public static ModelId GetId(Type type);
    public static T? GetByIdOrNull<T>(ModelId id) where T : AbstractModel;
    public static T GetById<T>(ModelId id) where T : AbstractModel;

    // Pool accessors
    public static CardPoolModel CardPool<T>() where T : CardPoolModel;
    public static RelicPoolModel RelicPool<T>() where T : RelicPoolModel;
    public static PotionPoolModel PotionPool<T>() where T : PotionPoolModel;
    public static T Power<T>() where T : PowerModel;    // gets canonical PowerModel instance

    // Registration (used by BaseLib, rarely called directly in mods)
    public static void Inject(Type type);
    public static void Remove(Type type);
}
```

---

## ActModel Extensions (BaseLib)

```csharp
public static class ActModelExtensions
{
    // Get act number (1 = Overgrowth/Underdocks, 2 = Hive, 3 = Glory, -1 = unknown)
    public static int ActNumber(this ActModel actModel);
}
// Game acts: Overgrowth (Act 1), Underdocks (Act 1), Hive (Act 2), Glory (Act 3)
```

---

## ModelExtensions (BaseLib)

```csharp
public static class ModelExtensions
{
    // Build a localization key like "MyCard.mySubKey"
    public static string LocKey(this AbstractModel model, string subKey);
}
```

---

## DynamicVarExtensions (BaseLib)

```csharp
public static class DynamicVarExtensions
{
    // Add upgrade amount to a var (stored separately, applied on card upgrade)
    public static DynamicVar WithUpgrade(this DynamicVar dynamicVar, decimal upgradeValue);

    // Add a tooltip hover tip to this var (shown when hovering card)
    public static DynamicVar WithTooltip(this DynamicVar var, string? locKey = null, string locTable = "static_hover_tips");

    // Calculate block after modifiers (useful in custom block calculations)
    public static decimal CalculateBlock(this DynamicVar var, Creature creature, ValueProp props,
        CardPlay? cardPlay = null, CardModel? cardSource = null);
}
```

---

## DynamicVarSetExtensions (BaseLib)

```csharp
public static class DynamicVarSetExtensions
{
    // Access a custom power's var by type name key
    public static DynamicVar Power<T>(this DynamicVarSet vars) where T : PowerModel;
    // equivalent to vars[typeof(T).Name]
}
```

---

## IHealAmountModifier (BaseLib Hook)

Implement this interface on a `PowerModel` to modify healing amounts:

```csharp
public interface IHealAmountModifier
{
    decimal ModifyHealAdditive(Creature creature, decimal amount) => 0m;
    decimal ModifyHealMultiplicative(Creature creature, decimal amount) => 1m;
}
```

---

## Full Card Example (Complete)

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

[Pool(typeof(SakikoCardPool))]
public class TwilightBlade : ConstructedCardModel
{
    public TwilightBlade() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, upgrade: 3)
            .WithPower<StrengthPower>(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await CommonActions.ApplySelf<StrengthPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Twilight Blade", "Deal !Damage! damage. Gain !StrengthPower! Strength.");
}
```

---

## Full Power Example (Complete)

```csharp
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

public class NocturnalFuryPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Nocturnal Fury",
            "At the start of your turn, deal !Amount! damage to ALL enemies.",
            "At the start of your turn, deal {Amount} damage to ALL enemies."
        );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (Owner.Side == CombatSide.Player)
        {
            await DamageCmd.Attack(Amount)
                .TargetingAllOpponents(CombatState)
                .Execute(ctx);
        }
    }
}
```

---

## Full Relic Example (Complete)

```csharp
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

[Pool(typeof(SakikoRelicPool))]
public class MidnightCrystal : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Midnight Crystal",
            "Whenever you play a Power card, gain 1 energy.",
            "Crystallized starlight from the midnight sky."
        );

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type == CardType.Power)
        {
            Flash();
            // gain energy logic via hook system
        }
    }
}
```

---

## Common Patterns

### Pattern: Multi-hit Attack

```csharp
protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
{
    // 3-hit attack using card's Damage variable
    await CommonActions.CardAttack(this, play, hitCount: 3).Execute(ctx);
}
```

### Pattern: Apply Power to Target

```csharp
// Apply power with amount from card var
await CommonActions.Apply<WeakPower>(play.Target, this);  // uses PowerVar<WeakPower>
await CommonActions.Apply<WeakPower>(play.Target, this, 2m);  // explicit amount

// Apply to self
await CommonActions.ApplySelf<StrengthPower>(this);

// Apply to all enemies
await PowerCmd.Apply<WeakPower>(CombatState.Enemies, Amount, Owner.Creature, this);
```

### Pattern: Draw Cards

```csharp
// Draw using Cards dynamic var
await CommonActions.Draw(this, choiceContext);

// Draw specific amount
await CardPileCmd.Draw(choiceContext, 2, Owner);
```

### Pattern: Add Card to Hand

```csharp
// Add a copy of another card type to hand
await CardPileCmd.AddToCombatAndPreview<MyOtherCard>(Owner.Creature, PileType.Hand, 1, addedByPlayer: true);

// Add generated card
var newCard = new MyCard();
await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, addedByPlayer: true);
```

### Pattern: Check Owner Side

```csharp
// In AbstractModel hooks, check if effect is for our side
public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
{
    if (Owner.Side == CombatSide.Player)  // CombatSide.Player or CombatSide.Enemy
    {
        // ...
    }
}
```

### Pattern: Act-Specific Encounter

```csharp
public class MyBossEncounter : CustomEncounterModel
{
    public MyBossEncounter() : base(RoomType.Boss) { }

    public override bool IsValidForAct(ActModel act) => act.ActNumber() == 3;
}
```

### Pattern: Character Card Pool

```csharp
// The character defines which pool they use:
public class SakikoCharacter : CustomCharacterModel
{
    public override CardPoolModel CardPool => ModelDb.CardPool<SakikoCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<SakikoRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<SakikoPotionPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        new MyStrikeCard(), new MyStrikeCard(), new MyStrikeCard(),
        new MyStrikeCard(), new MyStrikeCard(),
        new MyDefendCard(), new MyDefendCard(), new MyDefendCard(),
        new MyDefendCard(),
    };

    public override IReadOnlyList<RelicModel> StartingRelics =>
        new RelicModel[] { new MyStarterRelic() };
}
```

### Pattern: Harmony Patch

```csharp
[HarmonyPatch(typeof(SomeGameClass), "SomeMethod")]
public static class MySomePatch
{
    [HarmonyPrefix]
    public static bool Prefix(SomeGameClass __instance, ref ReturnType __result)
    {
        // return true = run original, return false = skip original
        return true;
    }

    [HarmonyPostfix]
    public static void Postfix(SomeGameClass __instance, ReturnType __result)
    {
        // runs after original
    }
}
```

---

## Localization File Structure

Localization is provided in-memory via the `Localization` property. The BaseLib hooks intercept the game's localization lookup and inject these values when the type's ID is looked up.

The loc table used depends on the model type:
- Cards: `"cards"` table, key = `{TypeName.ToLower()}.{field}`
- Powers: `"powers"` table
- Relics: `"relics"` table
- Potions: `"potions"` table
- Characters: `"characters"` table
- Monsters: `"monsters"` table

The `ILocalizationProvider.LocTable` property can override the default table.

In description text, variables are interpolated using `!VarName!` syntax.

---

## Notes and Gotchas

1. **All card OnPlay methods must be `protected override async Task`** - even if they don't await.
2. **`autoAdd = true` (default) requires the `[Pool(...)]` attribute** - forgetting the attribute throws at startup.
3. **`ConstructedCardModel` seals `CanonicalVars`** - you cannot override it; use `WithVars()`, `WithDamage()`, etc. in the constructor instead.
4. **`CustomCardModel.GainsBlock` is auto-detected** - any `BlockVar` or `CalculatedBlockVar` in `CanonicalVars` makes this true.
5. **`SavedSpireField` supports limited types** - only `int`, `bool`, `string`, `ModelId`, `Enum`, `SerializableCard`, and arrays/lists of those.
6. **`WeightedList.GetRandom` throws on empty list** - always check `Count > 0` first.
7. **Power hooks fire on canonical instances** - `ShouldReceiveCombatHooks` is `true` for all models inheriting PowerModel.
8. **Card hooks only fire when in a combat pile** - `ShouldReceiveCombatHooks` returns `Pile?.IsCombatPile ?? false`.
9. **`ITemporaryPower` / `CustomTemporaryPowerModel`** - do NOT nest TemporaryPowerModels; log warning and skip if attempted.
10. **`CharacterGender`** - use `CharacterGender.Male`, `Female`, or `NonBinary` (from `MegaCrit.Sts2.Core.Entities.Characters`).
11. **`CombatSide`** - use `CombatSide.Player` or `CombatSide.Enemy` (from `MegaCrit.Sts2.Core.Combat`).
12. **`ValueProp.Move` (= 8)** is the flag for "this damage/block comes from a card or monster move", affecting Strength/Dexterity scaling.
