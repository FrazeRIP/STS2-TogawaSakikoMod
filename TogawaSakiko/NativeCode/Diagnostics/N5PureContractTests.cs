using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Modifiers;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Tracking;
using SakikoCrueltyPower = TogawaSakiko.NativeCode.Models.Powers.CrueltyPower;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N5PureContractTests
{
    public const int AssertionCount = 725;
    private static bool _ran;
    private static int _assertions;

    public static void Run()
    {
        if (_ran)
        {
            return;
        }

        ValidateGreetings();
        ValidateTiredness();
        ValidateMelody();
        ValidateIdeal();
        ValidateProtection();
        ValidateRadiance();
        ValidateKindness();
        ValidateLostPowerSelection();
        ValidateAmoris();
        ValidateDoloris();
        ValidateMortis();
        ValidateOblivionis();
        ValidateTimoris();
        ValidateCursePowers();
        ValidateBlackKeys();
        ValidateWhiteKeys();
        ValidateBlackAndWhiteKeys();
        ValidateMantraAndDivinity();
        ValidateVoice();
        ValidateInnerCry();
        ValidateMementoMori();
        ValidateBudgetBento();
        ValidatePhantomCards();
        ValidateSymbolIIAir();
        ValidateDarkHeaven();
        ValidateGeorgetteMeGeorgetteYou();
        ValidateHeartsBarrier();
        ValidateDaten();
        ValidateKillKiSS();
        ValidateSymbolIVEarth();
        ValidateQuaerereLumina();
        ValidateKings();
        ValidateAccomplice();
        ValidateCarefree();
        ValidateDesuWa();
        ValidateEdgeOfBreakdown();
        ValidateMasqueradeRhapsodyRequest();
        ValidateWeakness();
        ValidateClockOut();
        ValidateFallenFlowers();
        ValidateHachibouseiDance();
        ValidatePhantomOfMutsumi();
        ValidatePhantomOfSoyo();
        ValidateRhinocerosBeetle();
        ValidateCountingStars();
        ValidateRemainingUncommonCards();
        ValidateRemainingUncommonPowers();
        ValidateRareCards();
        ValidateRarePowers();
        ValidateWishFulfilledSelection();
        ValidateVoiceRoutes();
        Require(_assertions == AssertionCount, $"expected {AssertionCount} assertions, executed {_assertions}");
        _ran = true;
    }

    private static void ValidateGreetings()
    {
        GreetingsCard card = ModelDb.Card<GreetingsCard>();
        Require(card.EnergyCost.Canonical == 0, "Greetings cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon, "Greetings type and rarity");
        Require(card.TargetType == TargetType.Self, "Greetings target");
        Require(card.Keywords.SetEquals([CardKeyword.Innate, CardKeyword.Exhaust]), "Greetings keywords");
        Require(card.DynamicVars.Energy.BaseValue == 2m, "Greetings energy");
        Require(Upgraded(card).DynamicVars.Energy.BaseValue == 3m, "Greetings upgraded energy");
    }

    private static void ValidateTiredness()
    {
        TirednessCard card = ModelDb.Card<TirednessCard>();
        Require(card.EnergyCost.Canonical == 0, "Tiredness cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Token, "Tiredness type and rarity");
        Require(card.TargetType == TargetType.Self, "Tiredness target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Tiredness keywords");
        Require(card.DynamicVars.Cards.BaseValue == 1m, "Tiredness draw");
        Require(Upgraded(card).DynamicVars.Cards.BaseValue == 2m, "Tiredness upgraded draw");
    }

    private static void ValidateMelody()
    {
        MelodyCard card = ModelDb.Card<MelodyCard>();
        Require(card.EnergyCost.Canonical == 0, "Melody cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Token, "Melody type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Melody target");
        Require(card.DynamicVars.Damage.BaseValue == 6m, "Melody damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 9m, "Melody upgraded damage");
    }

    private static void ValidateIdeal()
    {
        IdealCard card = ModelDb.Card<IdealCard>();
        Require(card.EnergyCost.Canonical == 1, "Ideal cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "Ideal type and rarity");
        Require(card.TargetType == TargetType.Self, "Ideal target");
        Require(card.DynamicVars[nameof(FreeAttackPower)].BaseValue == 2m, "Ideal free attacks");
        Require(Upgraded(card).DynamicVars[nameof(FreeAttackPower)].BaseValue == 3m, "Ideal upgraded free attacks");
    }

    private static void ValidateProtection()
    {
        ProtectionCard card = ModelDb.Card<ProtectionCard>();
        Require(card.EnergyCost.Canonical == 0, "Protection cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "Protection type and rarity");
        Require(card.TargetType == TargetType.Self, "Protection target");
        Require(card.DynamicVars.Repeat.BaseValue == 2m, "Protection repetitions");
        Require(Upgraded(card).DynamicVars.Repeat.BaseValue == 3m, "Protection upgraded repetitions");
    }

    private static void ValidateRadiance()
    {
        RadianceCard card = ModelDb.Card<RadianceCard>();
        Require(card.EnergyCost.Canonical == 1, "Radiance cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "Radiance type and rarity");
        Require(card.TargetType == TargetType.Self, "Radiance target");
        Require(card.DynamicVars.Repeat.BaseValue == 2m, "Radiance repetitions");
        Require(card.DynamicVars[nameof(DazzlingPower)].BaseValue == 2m, "Radiance Dazzling per repetition");
        RadianceCard upgraded = Upgraded(card);
        Require(upgraded.DynamicVars.Repeat.BaseValue == 3m, "Radiance upgraded repetitions");
        Require(upgraded.DynamicVars[nameof(DazzlingPower)].BaseValue == 2m, "Radiance upgraded Dazzling per repetition");
    }

    private static void ValidateKindness()
    {
        KindnessCard card = ModelDb.Card<KindnessCard>();
        Require(card.EnergyCost.Canonical == 1, "Kindness cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "Kindness type and rarity");
        Require(card.TargetType == TargetType.Self, "Kindness target");
        Require(Upgraded(card).EnergyCost.GetWithModifiers(CostModifiers.None) == 0, "Kindness upgraded cost");
    }

    private static void ValidateLostPowerSelection()
    {
        PowerChangeLedger ledger = new();
        CreatureIdentity player = new(0, CombatSide.Player, new ModelId("CHARACTER", "PLAYER"), 1);
        CreatureIdentity enemy = new(1, CombatSide.Enemy, new ModelId("MONSTER", "ENEMY"), null);
        ModelId strength = ModelDb.GetId<StrengthPower>();
        ModelId artifact = ModelDb.GetId<ArtifactPower>();
        ModelId weak = ModelDb.GetId<WeakPower>();

        ledger.Record(LossEvent(1, player, strength, PowerType.Buff, -2m));
        ledger.Record(LossEvent(2, player, strength, PowerType.Buff, -1m));
        ledger.Record(LossEvent(2, player, artifact, PowerType.Buff, -1m));
        ledger.Record(LossEvent(2, enemy, strength, PowerType.Buff, -9m));
        ledger.Record(LossEvent(2, player, weak, PowerType.Debuff, -5m));

        var withPrevious = LostPowerRestorationCommand.SelectLostBuffs(ledger, 2, player, includePreviousRound: true);
        Require(withPrevious.Length == 2, "Kindness selected unrelated power losses");
        Require(withPrevious[0].PowerModelId == strength && withPrevious[1].PowerModelId == artifact, "Kindness selection order");
        Require(withPrevious[0].Amount == 3, "Kindness current and previous Strength aggregation");
        Require(withPrevious[1].Amount == 1, "Kindness Artifact amount");

        var currentOnly = LostPowerRestorationCommand.SelectLostBuffs(ledger, 2, player, includePreviousRound: false);
        Require(currentOnly.Length == 2, "Kindness current-only selection count");
        Require(currentOnly[0].Amount == 1, "Kindness current-only Strength amount");
        Require(currentOnly.All(selection => selection.PowerModelId != weak), "Kindness included a debuff loss");
    }

    private static void ValidateAmoris()
    {
        AmorisCard card = ModelDb.Card<AmorisCard>();
        Require(card.EnergyCost.Canonical == -1, "Amoris unplayable cost");
        Require(card.Type == CardType.Curse && card.Rarity == CardRarity.Curse, "Amoris type and rarity");
        Require(card.TargetType == TargetType.None, "Amoris target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable, CardKeyword.Retain]), "Amoris keywords");
        Require(card.MaxUpgradeLevel == 0, "Amoris upgrade policy");
        Require(card.ShouldRetainThisTurn, "Amoris retain predicate");
        Require(!card.HasTurnEndInHandEffect, "Amoris must not enter the end-in-hand play queue");
    }

    private static void ValidateDoloris()
    {
        DolorisCard card = ModelDb.Card<DolorisCard>();
        Require(card.EnergyCost.Canonical == -1, "Doloris unplayable cost");
        Require(card.Type == CardType.Curse && card.Rarity == CardRarity.Curse, "Doloris type and rarity");
        Require(card.TargetType == TargetType.None, "Doloris target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable]), "Doloris keywords");
        Require(card.MaxUpgradeLevel == 0, "Doloris upgrade policy");
        Require(card.HasTurnEndInHandEffect, "Doloris end-in-hand hook");
        Require(card.DynamicVars.Damage.BaseValue == 2m, "Doloris damage");
    }

    private static void ValidateMortis()
    {
        MortisCard card = ModelDb.Card<MortisCard>();
        Require(card.EnergyCost.Canonical == -1, "Mortis unplayable cost");
        Require(card.Type == CardType.Curse && card.Rarity == CardRarity.Curse, "Mortis type and rarity");
        Require(card.TargetType == TargetType.None, "Mortis target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable]), "Mortis keywords");
        Require(card.MaxUpgradeLevel == 0, "Mortis upgrade policy");
        Require(card.HasTurnEndInHandEffect, "Mortis end-in-hand hook");
        Require(card.DynamicVars[nameof(MortisPower)].BaseValue == 1m, "Mortis duration");
    }

    private static void ValidateOblivionis()
    {
        OblivionisCard card = ModelDb.Card<OblivionisCard>();
        Require(card.EnergyCost.Canonical == -1, "Oblivionis unplayable cost");
        Require(card.Type == CardType.Curse && card.Rarity == CardRarity.Curse, "Oblivionis type and rarity");
        Require(card.TargetType == TargetType.None, "Oblivionis target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable, CardKeyword.Ethereal]), "Oblivionis keywords");
        Require(card.MaxUpgradeLevel == 0, "Oblivionis upgrade policy");
        Require(!card.HasTurnEndInHandEffect, "Oblivionis must use native Ethereal cleanup");
    }

    private static void ValidateTimoris()
    {
        TimorisCard card = ModelDb.Card<TimorisCard>();
        Require(card.EnergyCost.Canonical == -1, "Timoris unplayable cost");
        Require(card.Type == CardType.Curse && card.Rarity == CardRarity.Curse, "Timoris type and rarity");
        Require(card.TargetType == TargetType.None, "Timoris target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable]), "Timoris keywords");
        Require(card.MaxUpgradeLevel == 0, "Timoris upgrade policy");
        Require(card.HasTurnEndInHandEffect, "Timoris end-in-hand hook");
        Require(card.DynamicVars[nameof(TimorisPower)].BaseValue == 1m, "Timoris amount");
    }

    private static void ValidateCursePowers()
    {
        DolorisPower doloris = ModelDb.Power<DolorisPower>();
        MortisPower mortis = ModelDb.Power<MortisPower>();
        OblivionisPower oblivionis = ModelDb.Power<OblivionisPower>();
        TimorisPower timoris = ModelDb.Power<TimorisPower>();
        Require(doloris.Type == PowerType.Debuff && doloris.StackType == PowerStackType.Counter, "Doloris power metadata");
        Require(mortis.Type == PowerType.Debuff && mortis.StackType == PowerStackType.Counter, "Mortis power metadata");
        Require(oblivionis.Type == PowerType.Debuff && oblivionis.StackType == PowerStackType.Counter, "Oblivionis power metadata");
        Require(timoris.Type == PowerType.Debuff && timoris.StackType == PowerStackType.Counter, "Timoris power metadata");
    }

    private static void ValidateBlackKeys()
    {
        BlackKeysCard card = ModelDb.Card<BlackKeysCard>();
        Require(card.EnergyCost.Canonical == -1, "Black Keys option cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "Black Keys type and rarity");
        Require(card.TargetType == TargetType.None, "Black Keys target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable]), "Black Keys option keyword");
        Require(!card.CanBeGeneratedInCombat, "Black Keys random generation policy");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "Black Keys amount");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 3m, "Black Keys upgraded amount");
    }

    private static void ValidateWhiteKeys()
    {
        WhiteKeysCard card = ModelDb.Card<WhiteKeysCard>();
        Require(card.EnergyCost.Canonical == -1, "White Keys option cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "White Keys type and rarity");
        Require(card.TargetType == TargetType.None, "White Keys target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable]), "White Keys option keyword");
        Require(!card.CanBeGeneratedInCombat, "White Keys random generation policy");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "White Keys amount");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 3m, "White Keys upgraded amount");
    }

    private static void ValidateBlackAndWhiteKeys()
    {
        BlackAndWhiteKeysCard card = ModelDb.Card<BlackAndWhiteKeysCard>();
        Require(card.EnergyCost.Canonical == 0, "Black and White Keys cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon,
            "Black and White Keys type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Black and White Keys target");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "Black and White Keys amount");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 3m,
            "Black and White Keys upgraded amount");
        Require(card.Keywords.Count == 0, "Black and White Keys unexpected keyword");
    }

    private static void ValidateMantraAndDivinity()
    {
        MantraPower mantra = ModelDb.Power<MantraPower>();
        MonsterDivinityPower divinity = ModelDb.Power<MonsterDivinityPower>();
        Require(mantra.Type == PowerType.Buff, "Mantra power type");
        Require(mantra.StackType == PowerStackType.Counter, "Mantra stack type");
        Require(divinity.Type == PowerType.Buff, "Divinity power type");
        Require(divinity.StackType == PowerStackType.Single, "Divinity stack type");
        Require(!PowerCopyCommand.GetCompatibility(divinity).Supported, "Divinity copy policy");
    }

    private static void ValidateVoice()
    {
        VoiceCard card = ModelDb.Card<VoiceCard>();
        Require(card.EnergyCost.Canonical == 1, "Voice cost");
        Require(card.Type == CardType.Power && card.Rarity == CardRarity.Token, "Voice type and rarity");
        Require(card.TargetType == TargetType.Self, "Voice target");
        Require(card.Keywords.Count == 0, "Voice unexpected keyword");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "Voice repetitions");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 3m, "Voice upgraded repetitions");
    }

    private static void ValidateInnerCry()
    {
        InnerCryCard card = ModelDb.Card<InnerCryCard>();
        Require(card.EnergyCost.Canonical == 1, "Inner Cry cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common, "Inner Cry type and rarity");
        Require(card.TargetType == TargetType.Self, "Inner Cry target");
        Require(card.GainsBlock, "Inner Cry block declaration");
        Require(card.Keywords.Count == 0, "Inner Cry unexpected keyword");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 3m, "Inner Cry Mantra");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 4m, "Inner Cry upgraded Mantra");
        Require(card.DynamicVars.Block.BaseValue == 8m, "Inner Cry Block");
        Require(Upgraded(card).DynamicVars.Block.BaseValue == 10m, "Inner Cry upgraded Block");
    }

    private static void ValidateMementoMori()
    {
        MementoMoriCard card = ModelDb.Card<MementoMoriCard>();
        Require(card.EnergyCost.Canonical == 2, "Memento Mori cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Rare, "Memento Mori type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Memento Mori target");
        Require(card.Keywords.Count == 0, "Memento Mori unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 7m, "Memento Mori damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 13m, "Memento Mori upgraded damage");
        Require(MementoMoriCard.DrawPileWindowSize == 7, "Memento Mori draw-pile window");

        CardModel[] drawPile = Enumerable.Range(0, 8)
            .Select(_ => ModelDb.Card<DefendTogawaSakiko>().ToMutable())
            .ToArray();
        drawPile[2].AddKeyword(CardKeyword.Eternal);
        CardModel[] selected = MementoMoriCard.SelectTopRemovableCards(
            drawPile,
            MementoMoriCard.DrawPileWindowSize);
        Require(selected.Length == 6, "Memento Mori removable selection count");
        Require(selected.SequenceEqual(drawPile.Take(7).Where(candidate => candidate != drawPile[2])),
            "Memento Mori selection order and top-seven boundary");
        Require(MementoMoriCard.SelectTopRemovableCards(drawPile, 0).Length == 0,
            "Memento Mori non-positive window");
    }

    private static void ValidateBudgetBento()
    {
        BudgetBentoCard card = ModelDb.Card<BudgetBentoCard>();
        Require(card.EnergyCost.Canonical == 1, "Budget Bento cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common, "Budget Bento type and rarity");
        Require(card.TargetType == TargetType.Self, "Budget Bento target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Budget Bento keywords");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 4m, "Budget Bento Regen");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 5m, "Budget Bento upgraded Regen");
    }

    private static void ValidatePhantomCards()
    {
        PhantomOfSakikoCard sakiko = ModelDb.Card<PhantomOfSakikoCard>();
        Require(sakiko.EnergyCost.Canonical == 2, "Phantom of Sakiko cost");
        Require(sakiko.Type == CardType.Attack && sakiko.Rarity == CardRarity.Common,
            "Phantom of Sakiko type and rarity");
        Require(sakiko.TargetType == TargetType.AnyEnemy, "Phantom of Sakiko target");
        Require(sakiko.Keywords.SetEquals([CardKeyword.Exhaust]), "Phantom of Sakiko keywords");
        Require(sakiko.DynamicVars.Damage.BaseValue == 4m, "Phantom of Sakiko damage");
        Require(Upgraded(sakiko).DynamicVars.Damage.BaseValue == 4m,
            "Phantom of Sakiko upgraded damage policy");

        PhantomOfTakiCard taki = ModelDb.Card<PhantomOfTakiCard>();
        Require(taki.EnergyCost.Canonical == 2, "Phantom of Taki cost");
        Require(taki.Type == CardType.Attack && taki.Rarity == CardRarity.Common,
            "Phantom of Taki type and rarity");
        Require(taki.TargetType == TargetType.AnyEnemy, "Phantom of Taki target");
        Require(taki.Keywords.SetEquals([CardKeyword.Exhaust]), "Phantom of Taki keywords");
        Require(taki.DynamicVars.Damage.BaseValue == 12m, "Phantom of Taki damage");
        Require(Upgraded(taki).DynamicVars.Damage.BaseValue == 12m,
            "Phantom of Taki upgraded damage policy");

        PhantomOfTomoriCard tomori = ModelDb.Card<PhantomOfTomoriCard>();
        Require(tomori.EnergyCost.Canonical == 2, "Phantom of Tomori cost");
        Require(tomori.Type == CardType.Attack && tomori.Rarity == CardRarity.Common,
            "Phantom of Tomori type and rarity");
        Require(tomori.TargetType == TargetType.RandomEnemy, "Phantom of Tomori target");
        Require(tomori.Keywords.SetEquals([CardKeyword.Exhaust]), "Phantom of Tomori keywords");
        Require(tomori.DynamicVars.Damage.BaseValue == 14m, "Phantom of Tomori damage");
        Require(Upgraded(tomori).DynamicVars.Damage.BaseValue == 14m,
            "Phantom of Tomori upgraded damage policy");
    }

    private static void ValidateSymbolIIAir()
    {
        SymbolIIAirCard card = ModelDb.Card<SymbolIIAirCard>();
        Require(card.EnergyCost.Canonical == 0, "Symbol II: Air cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Symbol II: Air type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Symbol II: Air target");
        Require(card.Keywords.Count == 0, "Symbol II: Air unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 14m, "Symbol II: Air damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 14m, "Symbol II: Air upgraded damage policy");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 3m, "Symbol II: Air draw");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 4m, "Symbol II: Air upgraded draw");
    }

    private static void ValidateDarkHeaven()
    {
        DarkHeavenCard card = ModelDb.Card<DarkHeavenCard>();
        Require(card.EnergyCost.Canonical == 1, "Dark Heaven cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Dark Heaven type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Dark Heaven target");
        Require(card.Keywords.Count == 0, "Dark Heaven unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 9m, "Dark Heaven damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 9m, "Dark Heaven upgraded damage policy");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 1m, "Dark Heaven Strength steal");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Dark Heaven upgraded Strength steal");
    }

    private static void ValidateGeorgetteMeGeorgetteYou()
    {
        GeorgetteMeGeorgetteYouCard card = ModelDb.Card<GeorgetteMeGeorgetteYouCard>();
        Require(card.EnergyCost.Canonical == 0, "Georgette Me, Georgette You cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Georgette Me, Georgette You type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Georgette Me, Georgette You target");
        Require(card.Keywords.Count == 0, "Georgette Me, Georgette You unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 4m, "Georgette Me, Georgette You damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 5m,
            "Georgette Me, Georgette You upgraded damage");
    }

    private static void ValidateHeartsBarrier()
    {
        HeartsBarrierCard card = ModelDb.Card<HeartsBarrierCard>();
        HeartsBarrierCard upgraded = Upgraded(card);
        Require(card.EnergyCost.Canonical == 2, "Heart's Barrier cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common,
            "Heart's Barrier type and rarity");
        Require(card.TargetType == TargetType.Self, "Heart's Barrier target");
        Require(card.GainsBlock, "Heart's Barrier block declaration");
        Require(card.Keywords.Count == 0, "Heart's Barrier base keyword policy");
        Require(upgraded.Keywords.SetEquals([CardKeyword.Retain]), "Heart's Barrier upgraded Retain");
        Require(card.DynamicVars.CalculationBase.BaseValue == 0m, "Heart's Barrier calculation base");
        Require(card.DynamicVars.CalculationExtra.BaseValue == 1m, "Heart's Barrier deck-card multiplier");
        Require(card.DynamicVars.CalculatedBlock.Props == ValueProp.Move, "Heart's Barrier Block properties");
    }

    private static void ValidateDaten()
    {
        DatenCard card = ModelDb.Card<DatenCard>();
        Require(card.EnergyCost.Canonical == 2, "Daten cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common, "Daten type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Daten target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Daten keywords");
        Require(card.DynamicVars.Damage.BaseValue == 13m, "Daten damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 17m, "Daten upgraded damage");

        CardModel[] pool =
        [
            ModelDb.Card<StrikeTogawaSakiko>().ToMutable(),
            ModelDb.Card<DefendTogawaSakiko>().ToMutable(),
            ModelDb.Card<TirednessCard>().ToMutable(),
            ModelDb.Card<DesireCard>().ToMutable()
        ];
        CardModel[] first = DatenCard.SelectPurgeCandidates(pool, DatenCard.CandidateCount, new Rng(1701uL));
        CardModel[] repeated = DatenCard.SelectPurgeCandidates(pool, DatenCard.CandidateCount, new Rng(1701uL));
        Require(first.Length == DatenCard.CandidateCount, "Daten candidate count");
        Require(first.ToHashSet(ReferenceEqualityComparer.Instance).Count == DatenCard.CandidateCount,
            "Daten candidate uniqueness");
        Require(first.All(candidate => pool.Contains(candidate, ReferenceEqualityComparer.Instance)),
            "Daten candidate pool boundary");
        Require(first.SequenceEqual(repeated, ReferenceEqualityComparer.Instance),
            "Daten deterministic seeded selection");
        Require(DatenCard.SelectPurgeCandidates(pool, 0, new Rng(1uL)).Length == 0,
            "Daten non-positive selection count");
        Require(DatenCard.SelectPurgeCandidates(pool.Take(2), 3, new Rng(1uL)).Length == 2,
            "Daten selection caps at available cards");
    }

    private static void ValidateKillKiSS()
    {
        KillKiSSCard card = ModelDb.Card<KillKiSSCard>();
        Require(card.EnergyCost.Canonical == 1, "Kill KiSS cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Kill KiSS type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Kill KiSS target");
        Require(card.Keywords.Count == 0, "Kill KiSS unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 9m, "Kill KiSS damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 10m, "Kill KiSS upgraded damage");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 1m, "Kill KiSS Desire count");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Kill KiSS upgraded Desire count");

        CardModel drawFirst = ModelDb.Card<DesireCard>().ToMutable();
        CardModel drawSecond = ModelDb.Card<DesireCard>().ToMutable();
        CardModel discardFirst = ModelDb.Card<DesireCard>().ToMutable();
        CardModel discardSecond = ModelDb.Card<DesireCard>().ToMutable();
        CardModel[] selected = KillKiSSCard.SelectDesiresToRetrieve(
            [drawFirst, ModelDb.Card<DefendTogawaSakiko>().ToMutable(), drawSecond],
            [discardFirst, discardSecond],
            3);
        Require(selected.Length == 3, "Kill KiSS retrieval count");
        Require(ReferenceEquals(selected[0], drawFirst), "Kill KiSS first Draw-pile priority");
        Require(ReferenceEquals(selected[1], drawSecond), "Kill KiSS second Draw-pile priority");
        Require(ReferenceEquals(selected[2], discardFirst), "Kill KiSS Discard fallback");
        Require(KillKiSSCard.SelectDesiresToRetrieve(
                [drawFirst, drawSecond],
                [discardFirst, discardSecond],
                10).Length == 4,
            "Kill KiSS retrieval caps at available Desires");
        Require(KillKiSSCard.SelectDesiresToRetrieve([drawFirst], [discardFirst], 0).Length == 0,
            "Kill KiSS non-positive retrieval count");
    }

    private static void ValidateSymbolIVEarth()
    {
        SymbolIVEarthCard card = ModelDb.Card<SymbolIVEarthCard>();
        SymbolIVEarthCard upgraded = Upgraded(card);
        Require(card.EnergyCost.Canonical == 1, "Symbol IV: Earth cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Symbol IV: Earth type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Symbol IV: Earth target");
        Require(card.Keywords.Count == 0, "Symbol IV: Earth unexpected keyword");
        Require(card.GainsBlock, "Symbol IV: Earth block declaration");
        Require(card.DynamicVars.Block.BaseValue == 15m, "Symbol IV: Earth Block");
        Require(upgraded.DynamicVars.Block.BaseValue == 20m, "Symbol IV: Earth upgraded Block");
        Require(card.DynamicVars.CalculationBase.BaseValue == 15m, "Symbol IV: Earth calculation base");
        Require(upgraded.DynamicVars.CalculationBase.BaseValue == 20m,
            "Symbol IV: Earth upgraded calculation base");
        Require(card.DynamicVars.ExtraDamage.BaseValue == 1m, "Symbol IV: Earth Block multiplier");
        Require(card.DynamicVars.CalculatedDamage.Props == ValueProp.Move,
            "Symbol IV: Earth damage properties");
    }

    private static void ValidateQuaerereLumina()
    {
        QuaerereLuminaCard card = ModelDb.Card<QuaerereLuminaCard>();
        Require(card.EnergyCost.Canonical == 1, "Quaerere Lumina cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common,
            "Quaerere Lumina type and rarity");
        Require(card.TargetType == TargetType.Self, "Quaerere Lumina target");
        Require(card.GainsBlock, "Quaerere Lumina block declaration");
        Require(card.Keywords.Count == 0, "Quaerere Lumina unexpected keyword");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 7m, "Quaerere Lumina Scry amount");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 9m,
            "Quaerere Lumina upgraded Scry amount");

        CardModel[] drawPile = Enumerable.Range(0, 5)
            .Select(_ => ModelDb.Card<DefendTogawaSakiko>().ToMutable())
            .ToArray();
        CardModel[] window = QuaerereLuminaCard.SelectScryWindow(drawPile, 3);
        Require(window.Length == 3, "Quaerere Lumina Scry window count");
        Require(window.SequenceEqual(drawPile.Take(3)), "Quaerere Lumina Scry window order");
        Require(QuaerereLuminaCard.SelectScryWindow(drawPile, 0).Length == 0,
            "Quaerere Lumina non-positive Scry window");
        Require(QuaerereLuminaCard.SelectScryWindow(drawPile, 99).SequenceEqual(drawPile),
            "Quaerere Lumina Scry window caps at Draw-pile size");
    }

    private static void ValidateKings()
    {
        KingsCard card = ModelDb.Card<KingsCard>();
        Require(card.EnergyCost.Canonical == 1, "Kings cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Kings type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Kings target");
        Require(card.Keywords.Count == 0, "Kings unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 14m, "Kings damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 20m, "Kings upgraded damage");

        KingsPower power = ModelDb.Power<KingsPower>();
        Require(power.Type == PowerType.Debuff, "Kings power type");
        Require(power.StackType == PowerStackType.Single, "Kings nonstacking policy");

        List<int> options = [1, 2, 3];
        Require(KingsRewardState.RemoveOneOption(options), "Kings reward reduction result");
        Require(options.SequenceEqual([1, 2]), "Kings reward reduction removes exactly one option");
        Require(!KingsRewardState.RemoveOneOption(new List<int>()), "Kings empty reward clamp");

        CardCreationOptions encounterReward = new CardCreationOptions(
                Array.Empty<CardPoolModel>(),
                CardCreationSource.Encounter,
                CardRarityOddsType.RegularEncounter)
            .WithFlags(CardCreationFlags.IsCardReward);
        CardCreationOptions encounterNonReward = new CardCreationOptions(
            Array.Empty<CardPoolModel>(),
            CardCreationSource.Encounter,
            CardRarityOddsType.RegularEncounter);
        CardCreationOptions nonCombatReward = new CardCreationOptions(
                Array.Empty<CardPoolModel>(),
                CardCreationSource.Other,
                CardRarityOddsType.RegularEncounter)
            .WithFlags(CardCreationFlags.IsCardReward);
        Require(KingsRewardState.IsCombatCardReward(encounterReward), "Kings encounter reward eligibility");
        Require(!KingsRewardState.IsCombatCardReward(encounterNonReward), "Kings non-reward exclusion");
        Require(!KingsRewardState.IsCombatCardReward(nonCombatReward), "Kings non-combat reward exclusion");

        KingsCard mutableCard = (KingsCard)card.ToMutable();
        mutableCard.SetKingsRewardPending(true);
        SavedProperties cardSave = SavedProperties.From(mutableCard)
            ?? throw new InvalidOperationException("Kings card pending state did not serialize.");
        Require(cardSave.bools?.Single(property => property.name == nameof(KingsCard.KingsRewardPending)).value == true,
            "Kings card pending SavedProperty");
        KingsCard restoredCard = (KingsCard)card.ToMutable();
        cardSave.Fill(restoredCard);
        Require(restoredCard.KingsRewardPending, "Kings card pending round trip");

        StarterRelicTogawaSakiko relic =
            (StarterRelicTogawaSakiko)ModelDb.Relic<StarterRelicTogawaSakiko>().ToMutable();
        relic.SetKingsRewardPending(true);
        SavedProperties relicSave = SavedProperties.From(relic)
            ?? throw new InvalidOperationException("Kings relic pending state did not serialize.");
        Require(relicSave.bools?.Single(property =>
                property.name == nameof(StarterRelicTogawaSakiko.KingsRewardPending)).value == true,
            "Kings relic pending SavedProperty");
        StarterRelicTogawaSakiko restoredRelic =
            (StarterRelicTogawaSakiko)ModelDb.Relic<StarterRelicTogawaSakiko>().ToMutable();
        relicSave.Fill(restoredRelic);
        Require(restoredRelic.KingsRewardPending, "Kings relic pending round trip");

        KingsRewardCarrierModifier carrier =
            (KingsRewardCarrierModifier)ModelDb.Modifier<KingsRewardCarrierModifier>().ToMutable();
        Require(carrier.PendingPlayerIds.Length == 0, "Kings run carrier initial state");
        Require(KingsRewardCarrierModifier.ParsePendingPlayerIds("42,invalid,7,42")
                .SequenceEqual([7UL, 42UL]),
            "Kings run carrier robust ID parsing");
        carrier.SetPending(42UL, true);
        Require(carrier.IsPending(42UL), "Kings run carrier first player pending state");
        carrier.SetPending(7UL, true);
        Require(carrier.PendingPlayerIds == "7,42", "Kings run carrier deterministic player ordering");
        carrier.SetPending(42UL, true);
        Require(carrier.PendingPlayerIds == "7,42", "Kings run carrier duplicate player suppression");
        carrier.SetPending(42UL, false);
        Require(!carrier.IsPending(42UL) && carrier.IsPending(7UL),
            "Kings run carrier player-scoped clear");
        SavedProperties carrierStateSave = SavedProperties.From(carrier)
            ?? throw new InvalidOperationException("Kings run carrier pending state did not serialize.");
        Require(carrierStateSave.strings?.Single(property =>
                property.name == nameof(KingsRewardCarrierModifier.PendingPlayerIds)).value == "7",
            "Kings run carrier native SavedProperty");
        KingsRewardCarrierModifier restoredRunCarrier =
            (KingsRewardCarrierModifier)ModelDb.Modifier<KingsRewardCarrierModifier>().ToMutable();
        carrierStateSave.Fill(restoredRunCarrier);
        Require(restoredRunCarrier.IsPending(7UL), "Kings run carrier pending round trip");
        Require(!restoredRunCarrier.IsPending(42UL), "Kings run carrier cleared player round trip");
        restoredRunCarrier.SetPending(7UL, false);
        Require(restoredRunCarrier.PendingPlayerIds.Length == 0, "Kings run carrier full clear");
    }

    private static void ValidateAccomplice()
    {
        AccompliceCard card = ModelDb.Card<AccompliceCard>();
        Require(card.EnergyCost.Canonical == 2, "Accomplice cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common,
            "Accomplice type and rarity");
        Require(card.TargetType == TargetType.Self, "Accomplice target");
        Require(card.Keywords.Count == 0, "Accomplice unexpected keyword");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "Accomplice Desire count");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 3m,
            "Accomplice upgraded Desire count");

        DesireCard first = (DesireCard)ModelDb.Card<DesireCard>().ToMutable();
        DesireCard second = (DesireCard)ModelDb.Card<DesireCard>().ToMutable();
        DesireCard[] selected = AccompliceCard.SelectDesiresInHand(
                [first, ModelDb.Card<DefendTogawaSakiko>().ToMutable(), second])
            .ToArray();
        Require(selected.Length == 2, "Accomplice Desire hand filter count");
        Require(ReferenceEquals(selected[0], first) && ReferenceEquals(selected[1], second),
            "Accomplice Desire hand filter identity and order");
    }

    private static void ValidateCarefree()
    {
        CarefreeCard card = ModelDb.Card<CarefreeCard>();
        Require(card.EnergyCost.Canonical == 1, "Carefree cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common,
            "Carefree type and rarity");
        Require(card.TargetType == TargetType.Self, "Carefree target");
        Require(card.Keywords.Count == 0, "Carefree unexpected keyword");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "Carefree draw count");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 3m,
            "Carefree upgraded draw count");
    }

    private static void ValidateDesuWa()
    {
        DesuWaCard card = ModelDb.Card<DesuWaCard>();
        Require(card.EnergyCost.Canonical == 0, "Desu Wa cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common,
            "Desu Wa type and rarity");
        Require(card.TargetType == TargetType.Self, "Desu Wa target");
        Require(card.Keywords.Count == 0, "Desu Wa unexpected keyword");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 1m, "Desu Wa retrieval count");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Desu Wa upgraded retrieval count");

        CardModel drawAttack = ModelDb.Card<StrikeTogawaSakiko>().ToMutable();
        CardModel drawSkill = ModelDb.Card<DefendTogawaSakiko>().ToMutable();
        CardModel discardAttack = ModelDb.Card<MelodyCard>().ToMutable();
        CardModel[] drawPriority = DesuWaCard.SelectCandidatePile(
            [drawSkill, drawAttack],
            [discardAttack],
            CardType.Attack);
        Require(drawPriority.Length == 1 && ReferenceEquals(drawPriority[0], drawAttack),
            "Desu Wa did not prioritize matching Draw-pile cards");
        CardModel[] discardFallback = DesuWaCard.SelectCandidatePile(
            [drawSkill],
            [discardAttack],
            CardType.Attack);
        Require(discardFallback.Length == 1 && ReferenceEquals(discardFallback[0], discardAttack),
            "Desu Wa did not fall back to matching Discard-pile cards");
        Require(DesuWaCard.SelectCandidatePile([drawSkill], [], CardType.Attack).Length == 0,
            "Desu Wa returned a mismatched card type");
    }

    private static void ValidateEdgeOfBreakdown()
    {
        EdgeOfBreakdownCard card = ModelDb.Card<EdgeOfBreakdownCard>();
        Require(card.EnergyCost.Canonical == 1, "Edge of Breakdown cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Common,
            "Edge of Breakdown type and rarity");
        Require(card.TargetType == TargetType.Self, "Edge of Breakdown target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Edge of Breakdown Exhaust");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 2m, "Edge of Breakdown Frail");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 1m,
            "Edge of Breakdown upgraded Frail");
    }

    private static void ValidateMasqueradeRhapsodyRequest()
    {
        MasqueradeRhapsodyRequestCard card = ModelDb.Card<MasqueradeRhapsodyRequestCard>();
        MasqueradeRhapsodyRequestCard upgraded = Upgraded(card);
        Require(card.EnergyCost.Canonical == 1, "Masquerade Rhapsody Request cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Common,
            "Masquerade Rhapsody Request type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Masquerade Rhapsody Request target");
        Require(card.Keywords.Count == 0, "Masquerade Rhapsody Request unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 1m, "Masquerade Rhapsody Request damage");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 1m,
            "Masquerade Rhapsody Request purge growth");
        Require(upgraded.DynamicVars.Damage.BaseValue == 1m,
            "Masquerade Rhapsody Request upgraded base damage");
        Require(upgraded.DynamicVars["MagicNumber"].BaseValue == 2m,
            "Masquerade Rhapsody Request upgraded purge growth");

        MasqueradeRhapsodyRequestCard mutable =
            (MasqueradeRhapsodyRequestCard)card.ToMutable();
        mutable.IncreaseFromPurge(2);
        Require(mutable.PermanentDamageIncrease == 2, "Masquerade permanent increase state");
        Require(mutable.DynamicVars.Damage.BaseValue == 3m, "Masquerade permanent damage projection");
        SavedProperties save = SavedProperties.From(mutable)
            ?? throw new InvalidOperationException("Masquerade permanent damage did not serialize.");
        Require(save.ints?.Single(property =>
                property.name == nameof(MasqueradeRhapsodyRequestCard.PermanentDamageIncrease)).value == 2,
            "Masquerade permanent damage SavedProperty");
        MasqueradeRhapsodyRequestCard restored =
            (MasqueradeRhapsodyRequestCard)card.ToMutable();
        save.Fill(restored);
        Require(restored.PermanentDamageIncrease == 2 && restored.DynamicVars.Damage.BaseValue == 3m,
            "Masquerade permanent damage round trip");
        upgraded.IncreaseFromPurge(upgraded.DynamicVars["MagicNumber"].IntValue);
        Require(upgraded.PermanentDamageIncrease == 2 && upgraded.DynamicVars.Damage.BaseValue == 3m,
            "Masquerade upgraded purge increment");
    }

    private static void ValidateWeakness()
    {
        WeaknessCard card = ModelDb.Card<WeaknessCard>();
        Require(card.EnergyCost.Canonical == -1, "Weakness unplayable cost");
        Require(card.Type == CardType.Curse && card.Rarity == CardRarity.Curse,
            "Weakness type and rarity");
        Require(card.TargetType == TargetType.None, "Weakness target");
        Require(card.Keywords.SetEquals([CardKeyword.Unplayable]), "Weakness keyword");
        Require(card.MaxUpgradeLevel == 0, "Weakness upgrade policy");
    }

    private static void ValidateClockOut()
    {
        ClockOutCard card = ModelDb.Card<ClockOutCard>();
        Require(card.EnergyCost.Canonical == 1, "Clock Out cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon,
            "Clock Out type and rarity");
        Require(card.TargetType == TargetType.Self, "Clock Out target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Clock Out Exhaust");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 15m, "Clock Out gold");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 20m,
            "Clock Out upgraded gold");
    }

    private static void ValidateFallenFlowers()
    {
        FallenFlowersCard card = ModelDb.Card<FallenFlowersCard>();
        Require(card.EnergyCost.Canonical == 2, "Fallen Flowers cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon,
            "Fallen Flowers type and rarity");
        Require(card.TargetType == TargetType.Self, "Fallen Flowers target");
        Require(card.Keywords.SetEquals([CardKeyword.Ethereal]), "Fallen Flowers Ethereal");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 7m, "Fallen Flowers Dazzling");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 9m,
            "Fallen Flowers upgraded Dazzling");
    }

    private static void ValidateHachibouseiDance()
    {
        HachibouseiDanceCard card = ModelDb.Card<HachibouseiDanceCard>();
        HachibouseiDanceCard upgraded = Upgraded(card);
        Require(card.EnergyCost.Canonical == 4, "Hachibousei Dance cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Uncommon,
            "Hachibousei Dance type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Hachibousei Dance target");
        Require(card.Keywords.Count == 0, "Hachibousei Dance unexpected keyword");
        Require(card.DynamicVars.Damage.BaseValue == 8m, "Hachibousei Dance damage");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 8m,
            "Hachibousei Dance Plating");
        Require(upgraded.EnergyCost.GetWithModifiers(CostModifiers.None) == 3,
            "Hachibousei Dance upgraded cost");
        Require(upgraded.DynamicVars.Damage.BaseValue == 8m &&
                upgraded.DynamicVars["MagicNumber"].BaseValue == 8m,
            "Hachibousei Dance upgrade changed values");
    }

    private static void ValidatePhantomOfMutsumi()
    {
        PhantomOfMutsumiCard card = ModelDb.Card<PhantomOfMutsumiCard>();
        PhantomOfMutsumiCard upgraded = Upgraded(card);
        Require(card.EnergyCost.Canonical == 1, "Phantom of Mutsumi cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Uncommon,
            "Phantom of Mutsumi type and rarity");
        Require(card.TargetType == TargetType.AnyEnemy, "Phantom of Mutsumi target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Phantom of Mutsumi Exhaust");
        Require(card.GainsBlock, "Phantom of Mutsumi block flag");
        Require(card.DynamicVars.Damage.BaseValue == 6m, "Phantom of Mutsumi damage");
        Require(card.DynamicVars.Block.BaseValue == 6m, "Phantom of Mutsumi block");
        Require(upgraded.DynamicVars.Damage.BaseValue == 6m && upgraded.DynamicVars.Block.BaseValue == 6m,
            "Phantom of Mutsumi upgrade changed source values");
    }

    private static void ValidatePhantomOfSoyo()
    {
        PhantomOfSoyoCard card = ModelDb.Card<PhantomOfSoyoCard>();
        Require(card.EnergyCost.Canonical == 1, "Phantom of Soyo cost");
        Require(card.Type == CardType.Attack && card.Rarity == CardRarity.Uncommon,
            "Phantom of Soyo type and rarity");
        Require(card.TargetType == TargetType.AllEnemies, "Phantom of Soyo target");
        Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "Phantom of Soyo Exhaust");
        Require(card.DynamicVars.Damage.BaseValue == 7m, "Phantom of Soyo damage");
        Require(Upgraded(card).DynamicVars.Damage.BaseValue == 7m,
            "Phantom of Soyo upgrade changed damage");
    }

    private static void ValidateRhinocerosBeetle()
    {
        RhinocerosBeetleCard card = ModelDb.Card<RhinocerosBeetleCard>();
        Require(card.EnergyCost.Canonical == 1, "Rhinoceros Beetle cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon,
            "Rhinoceros Beetle type and rarity");
        Require(card.TargetType == TargetType.Self, "Rhinoceros Beetle target");
        Require(card.Keywords.Count == 0, "Rhinoceros Beetle unexpected keyword");
        Require(card.GainsBlock, "Rhinoceros Beetle block flag");
        Require(card.DynamicVars.CalculationBase.BaseValue == 4m, "Rhinoceros Beetle base block");
        Require(card.DynamicVars.CalculationExtra.BaseValue == 1m, "Rhinoceros Beetle Dazzling multiplier");
        Require(card.DynamicVars.CalculatedBlock.Props == ValueProp.Unpowered,
            "Rhinoceros Beetle bypasses Dexterity");
        Require(Upgraded(card).DynamicVars.CalculationBase.BaseValue == 7m,
            "Rhinoceros Beetle upgraded base block");
    }

    private static void ValidateCountingStars()
    {
        CountingStarsCard card = ModelDb.Card<CountingStarsCard>();
        Require(card.EnergyCost.Canonical == 1, "Counting Stars cost");
        Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon,
            "Counting Stars type and rarity");
        Require(card.TargetType == TargetType.Self, "Counting Stars target");
        Require(card.Keywords.Count == 0, "Counting Stars unexpected keyword");
        Require(card.DynamicVars.Energy.BaseValue == 1m, "Counting Stars next-turn energy");
        Require(card.DynamicVars["MagicNumber"].BaseValue == 1m, "Counting Stars Dazzling multiplier");
        Require(Upgraded(card).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Counting Stars upgraded Dazzling multiplier");
    }

    private static void ValidateRemainingUncommonCards()
    {
        AleaIactaEstCard alea = ModelDb.Card<AleaIactaEstCard>();
        Require(alea.EnergyCost.Canonical == 1, "Alea Iacta Est cost");
        Require(alea.Type == CardType.Skill && alea.Rarity == CardRarity.Uncommon,
            "Alea Iacta Est type and rarity");
        Require(alea.TargetType == TargetType.Self, "Alea Iacta Est target");
        Require(alea.Keywords.SetEquals([CardKeyword.Exhaust]), "Alea Iacta Est Exhaust");
        Require(Upgraded(alea).Keywords.SetEquals([CardKeyword.Exhaust]),
            "Alea Iacta Est upgraded keywords");

        AnglesCard angles = ModelDb.Card<AnglesCard>();
        Require(angles.EnergyCost.Canonical == 1, "Angles cost");
        Require(angles.Type == CardType.Attack && angles.Rarity == CardRarity.Uncommon,
            "Angles type and rarity");
        Require(angles.TargetType == TargetType.AnyEnemy, "Angles target");
        Require(angles.Keywords.SetEquals([CardKeyword.Exhaust]), "Angles Exhaust");
        Require(angles.DynamicVars.Damage.BaseValue == 9m, "Angles damage");
        Require(angles.DynamicVars["MagicNumber"].BaseValue == 1m, "Angles God's Creation");
        Require(Upgraded(angles).DynamicVars.Damage.BaseValue == 12m, "Angles upgraded damage");

        ChoirSChoirCard choir = ModelDb.Card<ChoirSChoirCard>();
        Require(choir.EnergyCost.Canonical == 3, "Choir 'S' Choir cost");
        Require(choir.Type == CardType.Attack && choir.Rarity == CardRarity.Uncommon,
            "Choir 'S' Choir type and rarity");
        Require(choir.TargetType == TargetType.AnyEnemy, "Choir 'S' Choir target");
        Require(choir.Keywords.SetEquals([CardKeyword.Retain]), "Choir 'S' Choir Retain");
        Require(choir.DynamicVars.Damage.BaseValue == 6m, "Choir 'S' Choir damage");
        Require(Upgraded(choir).DynamicVars.Damage.BaseValue == 7m,
            "Choir 'S' Choir upgraded damage");

        CrucifixXCard crucifix = ModelDb.Card<CrucifixXCard>();
        Require(crucifix.EnergyCost.Canonical == 0 && crucifix.EnergyCost.CostsX,
            "Crucifix X cost");
        Require(crucifix.Type == CardType.Attack && crucifix.Rarity == CardRarity.Uncommon,
            "Crucifix X type and rarity");
        Require(crucifix.TargetType == TargetType.AnyEnemy, "Crucifix X target");
        Require(crucifix.GainsBlock, "Crucifix X block flag");
        Require(crucifix.DynamicVars.Damage.BaseValue == 4m, "Crucifix X damage");
        Require(crucifix.DynamicVars.Block.BaseValue == 4m, "Crucifix X Block");
        CrucifixXCard upgradedCrucifix = Upgraded(crucifix);
        Require(upgradedCrucifix.DynamicVars.Damage.BaseValue == 6m &&
                upgradedCrucifix.DynamicVars.Block.BaseValue == 6m,
            "Crucifix X upgraded values");

        CuriosityCard curiosity = ModelDb.Card<CuriosityCard>();
        Require(curiosity.EnergyCost.Canonical == 2, "Curiosity cost");
        Require(curiosity.Type == CardType.Power && curiosity.Rarity == CardRarity.Uncommon,
            "Curiosity type and rarity");
        Require(curiosity.TargetType == TargetType.Self, "Curiosity target");
        Require(curiosity.Keywords.Count == 0, "Curiosity unexpected keyword");
        Require(Upgraded(curiosity).EnergyCost.GetWithModifiers(CostModifiers.None) == 1,
            "Curiosity upgraded cost");

        EnduranceCard endurance = ModelDb.Card<EnduranceCard>();
        Require(endurance.EnergyCost.Canonical == 1, "Endurance cost");
        Require(endurance.Type == CardType.Power && endurance.Rarity == CardRarity.Uncommon,
            "Endurance type and rarity");
        Require(endurance.TargetType == TargetType.Self, "Endurance target");
        Require(endurance.DynamicVars["MagicNumber"].BaseValue == 5m, "Endurance Block");
        Require(Upgraded(endurance).DynamicVars["MagicNumber"].BaseValue == 7m,
            "Endurance upgraded Block");

        FearlessCard fearless = ModelDb.Card<FearlessCard>();
        Require(fearless.EnergyCost.Canonical == 1, "Fearless cost");
        Require(fearless.Type == CardType.Power && fearless.Rarity == CardRarity.Uncommon,
            "Fearless type and rarity");
        Require(fearless.TargetType == TargetType.Self, "Fearless target");
        Require(fearless.DynamicVars["MagicNumber"].BaseValue == 2m, "Fearless turns");
        Require(Upgraded(fearless).DynamicVars["MagicNumber"].BaseValue == 3m,
            "Fearless upgraded turns");

        KaoCard kao = ModelDb.Card<KaoCard>();
        Require(kao.EnergyCost.Canonical == 1, "Kao cost");
        Require(kao.Type == CardType.Attack && kao.Rarity == CardRarity.Uncommon,
            "Kao type and rarity");
        Require(kao.TargetType == TargetType.AnyEnemy, "Kao target");
        Require(kao.Keywords.SetEquals([CardKeyword.Retain]), "Kao Retain");
        Require(kao.DynamicVars.Damage.BaseValue == 5m, "Kao damage");
        Require(kao.DynamicVars["MagicNumber"].BaseValue == 4m, "Kao growth");
        Require(Upgraded(kao).DynamicVars.Damage.BaseValue == 9m, "Kao upgraded damage");

        OurSongCard ourSong = ModelDb.Card<OurSongCard>();
        Require(ourSong.EnergyCost.Canonical == 1, "Our Song cost");
        Require(ourSong.Type == CardType.Skill && ourSong.Rarity == CardRarity.Uncommon,
            "Our Song type and rarity");
        Require(ourSong.TargetType == TargetType.Self, "Our Song target");
        Require(ourSong.Keywords.Count == 0, "Our Song unexpected keyword");
        Require(Upgraded(ourSong).EnergyCost.GetWithModifiers(CostModifiers.None) == 0,
            "Our Song upgraded cost");

        PerdereOmniaCard perdere = ModelDb.Card<PerdereOmniaCard>();
        Require(perdere.EnergyCost.Canonical == -1, "Perdere Omnia unplayable cost");
        Require(perdere.Type == CardType.Skill && perdere.Rarity == CardRarity.Uncommon,
            "Perdere Omnia type and rarity");
        Require(perdere.TargetType == TargetType.None, "Perdere Omnia target");
        Require(perdere.Keywords.SetEquals([CardKeyword.Unplayable]), "Perdere Omnia Unplayable");
        Require(perdere.DynamicVars["MagicNumber"].BaseValue == 1m, "Perdere Omnia count");
        Require(Upgraded(perdere).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Perdere Omnia upgraded count");

        PrimoDieInScaenaCard primo = ModelDb.Card<PrimoDieInScaenaCard>();
        Require(primo.EnergyCost.Canonical == 0, "Primo Die In Scaena cost");
        Require(primo.Type == CardType.Skill && primo.Rarity == CardRarity.Uncommon,
            "Primo Die In Scaena type and rarity");
        Require(primo.TargetType == TargetType.Self, "Primo Die In Scaena target");
        Require(primo.Keywords.Count == 0, "Primo Die In Scaena base keywords");
        Require(Upgraded(primo).Keywords.SetEquals([CardKeyword.Innate]),
            "Primo Die In Scaena upgraded Innate");

        SeizeTheFateCard seize = ModelDb.Card<SeizeTheFateCard>();
        Require(seize.EnergyCost.Canonical == 1, "Seize the Fate cost");
        Require(seize.Type == CardType.Power && seize.Rarity == CardRarity.Uncommon,
            "Seize the Fate type and rarity");
        Require(seize.TargetType == TargetType.Self, "Seize the Fate target");
        Require(seize.Keywords.Count == 0, "Seize the Fate unexpected keyword");
        Require(seize.DynamicVars["MagicNumber"].BaseValue == 6m, "Seize the Fate initial Hype");
        SeizeTheFateCard mutableSeize = (SeizeTheFateCard)seize.ToMutable();
        mutableSeize.TimesPlayed = 4;
        Require(mutableSeize.TimesPlayed == 4, "Seize the Fate saved play count");
        Require(mutableSeize.DynamicVars["MagicNumber"].BaseValue == 2m,
            "Seize the Fate permanent decay");
        mutableSeize.UpgradeInternal();
        mutableSeize.FinalizeUpgradeInternal();
        Require(mutableSeize.TimesPlayed == 4 && mutableSeize.DynamicVars["MagicNumber"].BaseValue == 2m,
            "Seize the Fate upgrade preserves prior decay");

        SharedDestinyCard shared = ModelDb.Card<SharedDestinyCard>();
        Require(shared.EnergyCost.Canonical == 1, "Shared Destiny cost");
        Require(shared.Type == CardType.Power && shared.Rarity == CardRarity.Uncommon,
            "Shared Destiny type and rarity");
        Require(shared.TargetType == TargetType.Self, "Shared Destiny target");
        Require(shared.Keywords.Count == 0, "Shared Destiny unexpected keyword");
        Require(shared.DynamicVars["MagicNumber"].BaseValue == 1m, "Shared Destiny draw");
        Require(Upgraded(shared).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Shared Destiny upgraded draw");

        SymbolIFireCard fire = ModelDb.Card<SymbolIFireCard>();
        Require(fire.EnergyCost.Canonical == 2, "Symbol I: Fire cost");
        Require(fire.Type == CardType.Attack && fire.Rarity == CardRarity.Uncommon,
            "Symbol I: Fire type and rarity");
        Require(fire.TargetType == TargetType.AllEnemies, "Symbol I: Fire target");
        Require(fire.Keywords.Count == 0, "Symbol I: Fire unexpected keyword");
        Require(fire.DynamicVars.Damage.BaseValue == 20m, "Symbol I: Fire damage");
        Require(fire.DynamicVars["MagicNumber"].BaseValue == 1m, "Symbol I: Fire replay count");
        Require(Upgraded(fire).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Symbol I: Fire upgraded replay count");

        SymbolIIIWaterCard water = ModelDb.Card<SymbolIIIWaterCard>();
        Require(water.EnergyCost.Canonical == 2, "Symbol III: Water cost");
        Require(water.Type == CardType.Attack && water.Rarity == CardRarity.Uncommon,
            "Symbol III: Water type and rarity");
        Require(water.TargetType == TargetType.RandomEnemy, "Symbol III: Water target");
        Require(water.Keywords.Count == 0, "Symbol III: Water unexpected keyword");
        Require(water.DynamicVars.Damage.BaseValue == 16m, "Symbol III: Water damage");
        Require(Upgraded(water).EnergyCost.GetWithModifiers(CostModifiers.None) == 1,
            "Symbol III: Water upgraded cost");

        TheGirlWithFlaxenHairCard girl = ModelDb.Card<TheGirlWithFlaxenHairCard>();
        Require(girl.EnergyCost.Canonical == 1, "The Girl with Flaxen Hair cost");
        Require(girl.Type == CardType.Attack && girl.Rarity == CardRarity.Uncommon,
            "The Girl with Flaxen Hair type and rarity");
        Require(girl.TargetType == TargetType.AnyEnemy, "The Girl with Flaxen Hair target");
        Require(girl.Keywords.Count == 0, "The Girl with Flaxen Hair unexpected keyword");
        Require(girl.DynamicVars.Damage.BaseValue == 7m, "The Girl with Flaxen Hair damage");
        Require(girl.DynamicVars["MagicNumber"].BaseValue == 3m,
            "The Girl with Flaxen Hair Block");
        TheGirlWithFlaxenHairCard upgradedGirl = Upgraded(girl);
        Require(upgradedGirl.DynamicVars.Damage.BaseValue == 7m &&
                upgradedGirl.DynamicVars["MagicNumber"].BaseValue == 5m,
            "The Girl with Flaxen Hair upgraded values");

        UtopiaCard utopia = ModelDb.Card<UtopiaCard>();
        Require(utopia.EnergyCost.Canonical == 2, "Utopia cost");
        Require(utopia.Type == CardType.Skill && utopia.Rarity == CardRarity.Uncommon,
            "Utopia type and rarity");
        Require(utopia.TargetType == TargetType.AnyEnemy, "Utopia target");
        Require(utopia.Keywords.Count == 0, "Utopia unexpected keyword");
        Require(utopia.DynamicVars["MagicNumber"] is HpLossVar, "Utopia HP-loss variable");
        Require(utopia.DynamicVars["MagicNumber"].BaseValue == 15m, "Utopia HP loss");
        Require(Upgraded(utopia).DynamicVars["MagicNumber"].BaseValue == 20m,
            "Utopia upgraded HP loss");

        VeritasCard veritas = ModelDb.Card<VeritasCard>();
        Require(veritas.EnergyCost.Canonical == 1, "Veritas cost");
        Require(veritas.Type == CardType.Skill && veritas.Rarity == CardRarity.Uncommon,
            "Veritas type and rarity");
        Require(veritas.TargetType == TargetType.Self, "Veritas target");
        Require(veritas.Keywords.Count == 0, "Veritas unexpected keyword");
        Require(veritas.DynamicVars["MagicNumber"].BaseValue == 3m, "Veritas draw");
        Require(Upgraded(veritas).DynamicVars["MagicNumber"].BaseValue == 4m,
            "Veritas upgraded draw");

        WishFulfilledCard wishFulfilled = ModelDb.Card<WishFulfilledCard>();
        Require(wishFulfilled.EnergyCost.Canonical == 2, "Wish Fulfilled cost");
        Require(wishFulfilled.Type == CardType.Skill && wishFulfilled.Rarity == CardRarity.Uncommon,
            "Wish Fulfilled type and rarity");
        Require(wishFulfilled.TargetType == TargetType.Self, "Wish Fulfilled target");
        Require(wishFulfilled.Keywords.SetEquals([CardKeyword.Exhaust, CardKeyword.Ethereal]),
            "Wish Fulfilled base keywords");
        WishFulfilledCard upgradedWish = Upgraded(wishFulfilled);
        Require(upgradedWish.Keywords.SetEquals([CardKeyword.Exhaust]),
            "Wish Fulfilled upgraded keywords");
        Require(upgradedWish.EnergyCost.GetWithModifiers(CostModifiers.None) == 2,
            "Wish Fulfilled upgrade changed cost");

        WishToBecomeHumanCard wishHuman = ModelDb.Card<WishToBecomeHumanCard>();
        Require(wishHuman.EnergyCost.Canonical == 2, "Wish to Become Human cost");
        Require(wishHuman.Type == CardType.Attack && wishHuman.Rarity == CardRarity.Uncommon,
            "Wish to Become Human type and rarity");
        Require(wishHuman.TargetType == TargetType.AnyEnemy, "Wish to Become Human target");
        Require(wishHuman.Keywords.Count == 0, "Wish to Become Human unexpected keyword");
        Require(wishHuman.DynamicVars.Damage.BaseValue == 2m, "Wish to Become Human damage");
        Require(Upgraded(wishHuman).DynamicVars.Damage.BaseValue == 3m,
            "Wish to Become Human upgraded damage");
    }

    private static void ValidateRemainingUncommonPowers()
    {
        PowerModel[] powers =
        [
            ModelDb.Power<CuriosityPower>(),
            ModelDb.Power<EndurancePower>(),
            ModelDb.Power<FearlessPower>(),
            ModelDb.Power<GodsCreationPower>(),
            ModelDb.Power<GirlOfSpringPower>(),
            ModelDb.Power<OurSongPower>(),
            ModelDb.Power<PerdereOmniaPower>(),
            ModelDb.Power<PrimoDieInScaenaPower>(),
            ModelDb.Power<SeizeTheFatePower>(),
            ModelDb.Power<SharedDestinyPower>()
        ];
        foreach (PowerModel power in powers)
        {
            Require(power.Type == PowerType.Buff && power.StackType == PowerStackType.Counter,
                $"{power.GetType().Name} type and stack policy");
        }

        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<CuriosityPower>()).Supported,
            "Curiosity reset-safe power-copy policy");
        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<OurSongPower>()).Supported,
            "Our Song reset-safe power-copy policy");
        Require(PowerCopyCommand.GetCompatibility(ModelDb.Power<SharedDestinyPower>()).Supported,
            "Shared Destiny reset-safe power-copy policy");
    }

    private static void ValidateRareCards()
    {
        AsYourHeartDesiresCard asYourHeartDesires = ModelDb.Card<AsYourHeartDesiresCard>();
        Require(asYourHeartDesires.EnergyCost.Canonical == 2, "As Your Heart Desires cost");
        Require(asYourHeartDesires.Type == CardType.Skill && asYourHeartDesires.Rarity == CardRarity.Rare,
            "As Your Heart Desires type and rarity");
        Require(asYourHeartDesires.TargetType == TargetType.Self, "As Your Heart Desires target");
        Require(asYourHeartDesires.Keywords.SetEquals([CardKeyword.Exhaust]),
            "As Your Heart Desires Exhaust");
        Require(asYourHeartDesires.DynamicVars["MagicNumber"].BaseValue == 1m &&
                Upgraded(asYourHeartDesires).DynamicVars["MagicNumber"].BaseValue == 1m,
            "As Your Heart Desires constant copy count");
        Require(Upgraded(asYourHeartDesires).EnergyCost.GetWithModifiers(CostModifiers.None) == 1,
            "As Your Heart Desires upgraded cost");

        AveMujicaCard aveMujica = ModelDb.Card<AveMujicaCard>();
        Require(aveMujica.EnergyCost.Canonical == 2, "Ave Mujica cost");
        Require(aveMujica.Type == CardType.Attack && aveMujica.Rarity == CardRarity.Rare,
            "Ave Mujica type and rarity");
        Require(aveMujica.TargetType == TargetType.AnyEnemy, "Ave Mujica target");
        Require(aveMujica.Keywords.Count == 0, "Ave Mujica Java-faithful non-Exhaust policy");
        Require(aveMujica.DynamicVars.Damage.BaseValue == 7m, "Ave Mujica damage stat");
        Require(Upgraded(aveMujica).DynamicVars.Damage.BaseValue == 10m,
            "Ave Mujica upgraded damage stat");
        Require(AveMujicaCard.ChoiceCount == 3, "Ave Mujica choice count");
        CardModel[] aveOptions = AveMujicaCard.SelectOptionCanonicals(new Rng(711uL));
        CardModel[] repeatedAveOptions = AveMujicaCard.SelectOptionCanonicals(new Rng(711uL));
        HashSet<CardModel> allowedAveOptions =
        [
            ModelDb.Card<SymbolIFireCard>(),
            ModelDb.Card<SymbolIIAirCard>(),
            ModelDb.Card<SymbolIIIWaterCard>(),
            ModelDb.Card<SymbolIVEarthCard>(),
            ModelDb.Card<EtherCard>()
        ];
        Require(aveOptions.Length == 3, "Ave Mujica selected option count");
        Require(aveOptions.Distinct(ReferenceEqualityComparer.Instance).Count() == 3,
            "Ave Mujica option uniqueness");
        Require(aveOptions.All(allowedAveOptions.Contains), "Ave Mujica option boundary");
        Require(aveOptions.SequenceEqual(repeatedAveOptions, ReferenceEqualityComparer.Instance),
            "Ave Mujica deterministic seeded selection");

        BandInvitationCard bandInvitation = ModelDb.Card<BandInvitationCard>();
        Require(bandInvitation.EnergyCost.Canonical == 0, "Band Invitation cost");
        Require(bandInvitation.Type == CardType.Skill && bandInvitation.Rarity == CardRarity.Rare,
            "Band Invitation type and rarity");
        Require(bandInvitation.TargetType == TargetType.Self, "Band Invitation target");
        Require(bandInvitation.Keywords.Count == 0, "Band Invitation unexpected keyword");
        Require(bandInvitation.DynamicVars["MagicNumber"].BaseValue == 4m,
            "Band Invitation energy threshold");
        Require(Upgraded(bandInvitation).DynamicVars["MagicNumber"].BaseValue == 5m,
            "Band Invitation upgraded energy threshold");
        Require(BandInvitationCard.CountPositiveEnergy(
                [ModelDb.Card<StrikeTogawaSakiko>(), ModelDb.Card<DefendTogawaSakiko>(), ModelDb.Card<AmorisCard>()]) == 2,
            "Band Invitation positive-cost sum");
        Require(BandInvitationCard.CountPositiveEnergy([]) == 0,
            "Band Invitation empty-cost sum");

        BlackBirthdayCard blackBirthday = ModelDb.Card<BlackBirthdayCard>();
        Require(blackBirthday.EnergyCost.Canonical == 2, "Black Birthday cost");
        Require(blackBirthday.Type == CardType.Attack && blackBirthday.Rarity == CardRarity.Rare,
            "Black Birthday type and rarity");
        Require(blackBirthday.TargetType == TargetType.AllEnemies, "Black Birthday target");
        Require(blackBirthday.Keywords.SetEquals([CardKeyword.Exhaust]), "Black Birthday Exhaust");
        Require(blackBirthday.DynamicVars.Damage.BaseValue == 12m, "Black Birthday damage");
        Require(Upgraded(blackBirthday).DynamicVars.Damage.BaseValue == 16m,
            "Black Birthday upgraded damage");
        PowerModel strength = ModelDb.Power<StrengthPower>();
        PowerModel surrounded = ModelDb.Power<SurroundedPower>();
        PowerModel[] removablePowers = BlackBirthdayCard.GetRemovablePowers([surrounded, strength]);
        Require(removablePowers.Length == 1 && ReferenceEquals(removablePowers[0], strength),
            "Black Birthday removable-power filter");

        CharismaticFormCard charismatic = ModelDb.Card<CharismaticFormCard>();
        Require(charismatic.EnergyCost.Canonical == 3, "Charismatic Form cost");
        Require(charismatic.Type == CardType.Power && charismatic.Rarity == CardRarity.Rare,
            "Charismatic Form type and rarity");
        Require(charismatic.TargetType == TargetType.Self, "Charismatic Form target");
        Require(charismatic.Keywords.Count == 0, "Charismatic Form base keywords");
        Require(charismatic.DynamicVars["MagicNumber"].BaseValue == 2m,
            "Charismatic Form Hype amount");
        Require(Upgraded(charismatic).Keywords.SetEquals([CardKeyword.Innate]),
            "Charismatic Form upgraded Innate");

        CrueltyCard cruelty = ModelDb.Card<CrueltyCard>();
        Require(cruelty.EnergyCost.Canonical == 0, "Cruelty cost");
        Require(cruelty.Type == CardType.Power && cruelty.Rarity == CardRarity.Rare,
            "Cruelty type and rarity");
        Require(cruelty.TargetType == TargetType.Self, "Cruelty target");
        Require(cruelty.Keywords.Count == 0, "Cruelty unexpected keyword");
        Require(cruelty.DynamicVars["MagicNumber"].BaseValue == 2m, "Cruelty Vulnerable");
        Require(Upgraded(cruelty).DynamicVars["MagicNumber"].BaseValue == 1m,
            "Cruelty upgraded Vulnerable");

        CrychicCard crychic = ModelDb.Card<CrychicCard>();
        Require(crychic.EnergyCost.Canonical == 0 && crychic.EnergyCost.CostsX, "Crychic X cost");
        Require(crychic.Type == CardType.Skill && crychic.Rarity == CardRarity.Rare,
            "Crychic type and rarity");
        Require(crychic.TargetType == TargetType.Self, "Crychic target");
        Require(crychic.Keywords.SetEquals([CardKeyword.Exhaust]), "Crychic Exhaust");
        Require(Upgraded(crychic).Keywords.SetEquals([CardKeyword.Exhaust]),
            "Crychic upgraded keywords");
        Require(CrychicPower.PhantomCanonicals.Count == 5 &&
                CrychicPower.PhantomCanonicals.Distinct(ReferenceEqualityComparer.Instance).Count() == 5,
            "Crychic phantom pool");

        EtherCard ether = ModelDb.Card<EtherCard>();
        Require(ether.EnergyCost.Canonical == 2, "Ether cost");
        Require(ether.Type == CardType.Attack && ether.Rarity == CardRarity.Rare,
            "Ether type and rarity");
        Require(ether.TargetType == TargetType.AnyEnemy, "Ether target");
        Require(ether.Keywords.Count == 0, "Ether unexpected keyword");
        Require(ether.DynamicVars.Damage.BaseValue == 2m, "Ether damage per exhausted card");
        Require(Upgraded(ether).DynamicVars.Damage.BaseValue == 3m,
            "Ether upgraded damage per exhausted card");
        Require(EtherCard.MaximumExhaustCount == 12, "Ether exhaust cap");
        CardModel[] etherCandidates = Enumerable.Range(0, 13)
            .Select(_ => ModelDb.Card<DefendTogawaSakiko>().ToMutable())
            .Append(ModelDb.Card<StrikeTogawaSakiko>().ToMutable())
            .ToArray();
        CardModel[] selectedForEther = EtherCard.SelectCardsToExhaust(etherCandidates);
        Require(selectedForEther.Length == 12, "Ether selected exhaust count");
        Require(selectedForEther.All(card => card.Type != CardType.Attack),
            "Ether selected an Attack");
        Require(selectedForEther.SequenceEqual(etherCandidates.Take(12), ReferenceEqualityComparer.Instance),
            "Ether source-order and cap policy");

        ImprisonedXIICard imprisoned = ModelDb.Card<ImprisonedXIICard>();
        Require(imprisoned.EnergyCost.Canonical == 3, "Imprisoned XII cost");
        Require(imprisoned.Type == CardType.Attack && imprisoned.Rarity == CardRarity.Rare,
            "Imprisoned XII type and rarity");
        Require(imprisoned.TargetType == TargetType.AnyEnemy, "Imprisoned XII target");
        Require(imprisoned.Keywords.Count == 0, "Imprisoned XII base keywords");
        Require(imprisoned.DynamicVars.Damage.BaseValue == 8m, "Imprisoned XII damage");
        Require(Upgraded(imprisoned).Keywords.SetEquals([CardKeyword.Retain]),
            "Imprisoned XII upgraded Retain");

        MasksCard masks = ModelDb.Card<MasksCard>();
        Require(masks.EnergyCost.Canonical == 1, "Masks cost");
        Require(masks.Type == CardType.Power && masks.Rarity == CardRarity.Rare,
            "Masks type and rarity");
        Require(masks.TargetType == TargetType.Self, "Masks target");
        Require(masks.Keywords.Count == 0, "Masks base keywords");
        Require(masks.DynamicVars["MagicNumber"].BaseValue == 2m, "Masks Dazzling and Strength amount");
        Require(Upgraded(masks).Keywords.SetEquals([CardKeyword.Retain]), "Masks upgraded Retain");

        PerfectionCard perfection = ModelDb.Card<PerfectionCard>();
        Require(perfection.EnergyCost.Canonical == 3, "Perfection cost");
        Require(perfection.Type == CardType.Skill && perfection.Rarity == CardRarity.Rare,
            "Perfection type and rarity");
        Require(perfection.TargetType == TargetType.Self, "Perfection target");
        Require(perfection.Keywords.SetEquals([CardKeyword.Exhaust]), "Perfection Exhaust");
        Require(PerfectionCard.CandidateCount == 20, "Perfection candidate count");
        Require(Upgraded(perfection).EnergyCost.GetWithModifiers(CostModifiers.None) == 2,
            "Perfection upgraded cost");

        PrideCard pride = ModelDb.Card<PrideCard>();
        Require(pride.EnergyCost.Canonical == 0, "Pride cost");
        Require(pride.Type == CardType.Skill && pride.Rarity == CardRarity.Rare,
            "Pride type and rarity");
        Require(pride.TargetType == TargetType.Self, "Pride target");
        Require(pride.Keywords.SetEquals([CardKeyword.Innate, CardKeyword.Exhaust]), "Pride keywords");
        Require(pride.DynamicVars["MagicNumber"].BaseValue == 1m, "Pride Attack retrieval count");
        Require(Upgraded(pride).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Pride upgraded Attack retrieval count");
        CardModel drawAttack = ModelDb.Card<StrikeTogawaSakiko>().ToMutable();
        CardModel discardAttack = ModelDb.Card<MelodyCard>().ToMutable();
        CardModel nonAttack = ModelDb.Card<DefendTogawaSakiko>().ToMutable();
        Require(PrideCard.SelectCandidatePile([nonAttack, drawAttack], [discardAttack])
                .SequenceEqual([drawAttack], ReferenceEqualityComparer.Instance),
            "Pride Draw-pile precedence");
        Require(PrideCard.SelectCandidatePile([nonAttack], [nonAttack, discardAttack])
                .SequenceEqual([discardAttack], ReferenceEqualityComparer.Instance),
            "Pride Discard fallback");
        Require(PrideCard.SelectCandidatePile([nonAttack], [nonAttack]).Length == 0,
            "Pride empty candidate result");

        SoraNoMusicaCard sora = ModelDb.Card<SoraNoMusicaCard>();
        Require(sora.EnergyCost.Canonical == 3, "Sora No Musica cost");
        Require(sora.Type == CardType.Attack && sora.Rarity == CardRarity.Rare,
            "Sora No Musica type and rarity");
        Require(sora.TargetType == TargetType.AnyEnemy, "Sora No Musica target");
        Require(sora.Keywords.SetEquals([CardKeyword.Exhaust]), "Sora No Musica Exhaust");
        Require(sora.DynamicVars.Damage.BaseValue == 12m, "Sora No Musica damage");
        Require(sora.DynamicVars["MagicNumber"].BaseValue == 5m, "Sora No Musica selection cap");
        Require(Upgraded(sora).DynamicVars["MagicNumber"].BaseValue == 7m,
            "Sora No Musica upgraded selection cap");

        SpringSunlightCard spring = ModelDb.Card<SpringSunlightCard>();
        Require(spring.EnergyCost.Canonical == 0, "Spring Sunlight canonical cost");
        Require(spring.Type == CardType.Attack && spring.Rarity == CardRarity.Rare,
            "Spring Sunlight type and rarity");
        Require(spring.TargetType == TargetType.AnyEnemy, "Spring Sunlight target");
        Require(spring.Keywords.Count == 0, "Spring Sunlight unexpected keyword");
        Require(spring.DynamicVars.Damage.BaseValue == 30m, "Spring Sunlight damage");
        Require(spring.DynamicVars["MagicNumber"].BaseValue == 6m, "Spring Sunlight deck divisor");
        Require(Upgraded(spring).DynamicVars.Damage.BaseValue == 40m,
            "Spring Sunlight upgraded damage");
        Require(SpringSunlightCard.CalculateCost(0) == 0 && SpringSunlightCard.CalculateCost(5) == 0,
            "Spring Sunlight sub-threshold cost");
        Require(SpringSunlightCard.CalculateCost(6) == 1, "Spring Sunlight first threshold");
        Require(SpringSunlightCard.CalculateCost(11) == 1, "Spring Sunlight floor division");
        Require(SpringSunlightCard.CalculateCost(12) == 2, "Spring Sunlight second threshold");

        StayEleganceCard stayElegance = ModelDb.Card<StayEleganceCard>();
        Require(stayElegance.EnergyCost.Canonical == 1, "Stay Elegance cost");
        Require(stayElegance.Type == CardType.Skill && stayElegance.Rarity == CardRarity.Rare,
            "Stay Elegance type and rarity");
        Require(stayElegance.TargetType == TargetType.Self, "Stay Elegance target");
        Require(stayElegance.Keywords.SetEquals([CardKeyword.Exhaust]), "Stay Elegance Exhaust");
        Require(Upgraded(stayElegance).EnergyCost.GetWithModifiers(CostModifiers.None) == 0,
            "Stay Elegance upgraded cost");

        WishYouGoodLuckCard wishYouGoodLuck = ModelDb.Card<WishYouGoodLuckCard>();
        Require(wishYouGoodLuck.EnergyCost.Canonical == 1, "Wish You Good Luck cost");
        Require(wishYouGoodLuck.Type == CardType.Power && wishYouGoodLuck.Rarity == CardRarity.Rare,
            "Wish You Good Luck type and rarity");
        Require(wishYouGoodLuck.TargetType == TargetType.Self, "Wish You Good Luck target");
        Require(wishYouGoodLuck.Keywords.Count == 0, "Wish You Good Luck unexpected keyword");
        Require(wishYouGoodLuck.DynamicVars["MagicNumber"].BaseValue == 1m,
            "Wish You Good Luck amount");
        Require(Upgraded(wishYouGoodLuck).DynamicVars["MagicNumber"].BaseValue == 2m,
            "Wish You Good Luck upgraded amount");

        WorldviewCard worldview = ModelDb.Card<WorldviewCard>();
        Require(worldview.EnergyCost.Canonical == 1, "Worldview cost");
        Require(worldview.Type == CardType.Power && worldview.Rarity == CardRarity.Rare,
            "Worldview type and rarity");
        Require(worldview.TargetType == TargetType.Self, "Worldview target");
        Require(worldview.Keywords.Count == 0, "Worldview base keywords");
        Require(Upgraded(worldview).Keywords.SetEquals([CardKeyword.Innate]),
            "Worldview upgraded Innate");

        Require(ModelDb.GetId<AsYourHeartDesiresCard>().Entry == "TOGAWASAKIKO-AS_YOUR_HEART_DESIRES_CARD",
            "As Your Heart Desires stable ID");
        Require(ModelDb.GetId<AveMujicaCard>().Entry == "TOGAWASAKIKO-AVE_MUJICA_CARD", "Ave Mujica stable ID");
        Require(ModelDb.GetId<BandInvitationCard>().Entry == "TOGAWASAKIKO-BAND_INVITATION_CARD",
            "Band Invitation stable ID");
        Require(ModelDb.GetId<BlackBirthdayCard>().Entry == "TOGAWASAKIKO-BLACK_BIRTHDAY_CARD",
            "Black Birthday stable ID");
        Require(ModelDb.GetId<CharismaticFormCard>().Entry == "TOGAWASAKIKO-CHARISMATIC_FORM_CARD",
            "Charismatic Form stable ID");
        Require(ModelDb.GetId<CrueltyCard>().Entry == "TOGAWASAKIKO-CRUELTY_CARD", "Cruelty stable ID");
        Require(ModelDb.GetId<CrychicCard>().Entry == "TOGAWASAKIKO-CRYCHIC_CARD", "Crychic stable ID");
        Require(ModelDb.GetId<EtherCard>().Entry == "TOGAWASAKIKO-ETHER_CARD", "Ether stable ID");
        Require(ModelDb.GetId<ImprisonedXIICard>().Entry == "TOGAWASAKIKO-IMPRISONED_XI_I_CARD",
            "Imprisoned XII stable ID");
        Require(ModelDb.GetId<MasksCard>().Entry == "TOGAWASAKIKO-MASKS_CARD", "Masks stable ID");
        Require(ModelDb.GetId<PerfectionCard>().Entry == "TOGAWASAKIKO-PERFECTION_CARD", "Perfection stable ID");
        Require(ModelDb.GetId<PrideCard>().Entry == "TOGAWASAKIKO-PRIDE_CARD", "Pride stable ID");
        Require(ModelDb.GetId<SoraNoMusicaCard>().Entry == "TOGAWASAKIKO-SORA_NO_MUSICA_CARD",
            "Sora No Musica stable ID");
        Require(ModelDb.GetId<SpringSunlightCard>().Entry == "TOGAWASAKIKO-SPRING_SUNLIGHT_CARD",
            "Spring Sunlight stable ID");
        Require(ModelDb.GetId<StayEleganceCard>().Entry == "TOGAWASAKIKO-STAY_ELEGANCE_CARD",
            "Stay Elegance stable ID");
        Require(ModelDb.GetId<WishYouGoodLuckCard>().Entry == "TOGAWASAKIKO-WISH_YOU_GOOD_LUCK_CARD",
            "Wish You Good Luck stable ID");
        Require(ModelDb.GetId<WorldviewCard>().Entry == "TOGAWASAKIKO-WORLDVIEW_CARD", "Worldview stable ID");
    }

    private static void ValidateRarePowers()
    {
        CharismaticFormPower charismatic = ModelDb.Power<CharismaticFormPower>();
        SakikoCrueltyPower cruelty = ModelDb.Power<SakikoCrueltyPower>();
        CrychicPower crychic = ModelDb.Power<CrychicPower>();
        PridePower pride = ModelDb.Power<PridePower>();
        WishYouGoodLuckPower wish = ModelDb.Power<WishYouGoodLuckPower>();
        WorldviewPower worldview = ModelDb.Power<WorldviewPower>();

        Require(charismatic.Type == PowerType.Buff && charismatic.StackType == PowerStackType.Counter,
            "Charismatic Form power metadata");
        Require(cruelty.Type == PowerType.Buff && cruelty.StackType == PowerStackType.Counter,
            "Cruelty power metadata");
        Require(crychic.Type == PowerType.Buff && crychic.StackType == PowerStackType.Counter,
            "Crychic power metadata");
        Require(pride.Type == PowerType.Buff && pride.StackType == PowerStackType.Counter,
            "Pride power metadata");
        Require(wish.Type == PowerType.Buff && wish.StackType == PowerStackType.Counter,
            "Wish You Good Luck power metadata");
        Require(worldview.Type == PowerType.Buff && worldview.StackType == PowerStackType.Single,
            "Worldview power metadata");
        Require(PowerCopyCommand.GetCompatibility(charismatic).Supported,
            "Charismatic Form power-copy policy");
        Require(PowerCopyCommand.GetCompatibility(cruelty).Supported, "Cruelty power-copy policy");
        Require(PowerCopyCommand.GetCompatibility(crychic).Supported, "Crychic power-copy policy");
        Require(PowerCopyCommand.GetCompatibility(wish).Supported, "Wish You Good Luck power-copy policy");
        Require(PowerCopyCommand.GetCompatibility(worldview).Supported, "Worldview power-copy policy");
        PowerCopyCompatibility prideCompatibility = PowerCopyCommand.GetCompatibility(pride);
        Require(!prideCompatibility.Supported, "Pride internal-state power-copy rejection");
        Require(!string.IsNullOrWhiteSpace(prideCompatibility.Reason),
            "Pride power-copy rejection reason");

        Require(ModelDb.GetId<CharismaticFormPower>().Entry == "TOGAWASAKIKO-CHARISMATIC_FORM_POWER",
            "Charismatic Form power stable ID");
        Require(ModelDb.GetId<SakikoCrueltyPower>().Entry == "TOGAWASAKIKO-CRUELTY_POWER",
            "Cruelty power stable ID");
        Require(ModelDb.GetId<CrychicPower>().Entry == "TOGAWASAKIKO-CRYCHIC_POWER",
            "Crychic power stable ID");
        Require(ModelDb.GetId<PridePower>().Entry == "TOGAWASAKIKO-PRIDE_POWER",
            "Pride power stable ID");
        Require(ModelDb.GetId<WishYouGoodLuckPower>().Entry == "TOGAWASAKIKO-WISH_YOU_GOOD_LUCK_POWER",
            "Wish You Good Luck power stable ID");
        Require(ModelDb.GetId<WorldviewPower>().Entry == "TOGAWASAKIKO-WORLDVIEW_POWER",
            "Worldview power stable ID");
    }

    private static void ValidateWishFulfilledSelection()
    {
        CardModel removable = ModelDb.Card<StrikeTogawaSakiko>();
        CardModel protectedCard = ModelDb.Card<BadLuck>();
        CardModel[] candidates = WishFulfilledCard.GetPurgeCandidates([removable, protectedCard]);
        Require(candidates.Length == 1, "Wish Fulfilled filters non-removable cards");
        Require(ReferenceEquals(candidates[0], removable), "Wish Fulfilled preserves candidate order");
    }

    private static PowerChangeEvent LossEvent(
        int round,
        CreatureIdentity target,
        ModelId powerId,
        PowerType declaredType,
        decimal delta)
    {
        return new PowerChangeEvent(
            round,
            target,
            powerId,
            powerId.Entry,
            declaredType,
            declaredType,
            delta,
            decimal.ToInt32(decimal.Negate(delta)),
            0,
            PowerChangeKind.Reduced,
            null,
            null);
    }

    private static void ValidateVoiceRoutes()
    {
        string splitMoment = SakikoAudioCmd.GetCardVoicePath("ASplitMoment");
        string greetings = SakikoAudioCmd.GetCardVoicePath("Greetings");
        string tiredness = SakikoAudioCmd.GetCardVoicePath("Tiredness");
        string innerCry = SakikoAudioCmd.GetCardVoicePath("InnerCry");
        string phantomOfTaki = SakikoAudioCmd.GetCardVoicePath("PhantomOfTaki");
        string phantomOfTomori = SakikoAudioCmd.GetCardVoicePath("PhantomOfTomori");
        string heartsBarrier = SakikoAudioCmd.GetCardVoicePath("HeartsBarrier");
        string accomplice = SakikoAudioCmd.GetCardVoicePath("Accomplice");
        string carefree = SakikoAudioCmd.GetCardVoicePath("Carefree");
        string desuWa = SakikoAudioCmd.GetCardVoicePath("DesuWa");
        string edgeOfBreakdown = SakikoAudioCmd.GetCardVoicePath("EdgeOfBreakdown");
        string clockOut = SakikoAudioCmd.GetCardVoicePath("ClockOut");
        string fallenFlowers = SakikoAudioCmd.GetCardVoicePath("FallenFlowers");
        string phantomOfMutsumi = SakikoAudioCmd.GetCardVoicePath("PhantomOfMutsumi");
        string phantomOfSoyo = SakikoAudioCmd.GetCardVoicePath("PhantomOfSoyo");
        string rhinocerosBeetle = SakikoAudioCmd.GetCardVoicePath("RhinocerosBeetle");
        string countingStars = SakikoAudioCmd.GetCardVoicePath("CountingStars");
        string endurance = SakikoAudioCmd.GetCardVoicePath("Endurance");
        string fearless = SakikoAudioCmd.GetCardVoicePath("Fearless");
        string ourSong = SakikoAudioCmd.GetCardVoicePath("OurSong");
        string perdereOmnia = SakikoAudioCmd.GetCardVoicePath("PerdereOmnia");
        string primoDieInScaena = SakikoAudioCmd.GetCardVoicePath("PrimoDieInScaena");
        string sharedDestiny = SakikoAudioCmd.GetCardVoicePath("SharedDestiny");
        string theGirlWithFlaxenHair = SakikoAudioCmd.GetCardVoicePath("TheGirlWithFlaxenHair");
        string wishToBecomeHuman = SakikoAudioCmd.GetCardVoicePath("WishToBecomeHuman");
        (string Path, string Suffix)[] rareRoutes =
        [
            (SakikoAudioCmd.GetCardVoicePath("AsYourHeartDesires"), "/asyourheartdesires.wav"),
            (SakikoAudioCmd.GetCardVoicePath("AveMujica"), "/avemujica.wav"),
            (SakikoAudioCmd.GetCardVoicePath("BandInvitation"), "/bandinvitation.wav"),
            (SakikoAudioCmd.GetCardVoicePath("Cruelty"), "/cruelty.wav"),
            (SakikoAudioCmd.GetCardVoicePath("Masks"), "/masks.wav"),
            (SakikoAudioCmd.GetCardVoicePath("Perfection"), "/perfection.wav"),
            (SakikoAudioCmd.GetCardVoicePath("Pride"), "/pride.wav"),
            (SakikoAudioCmd.GetCardVoicePath("StayElegance"), "/stayelegance.wav"),
            (SakikoAudioCmd.GetCardVoicePath("WishYouGoodLuck"), "/wishyougoodluck.wav"),
            (SakikoAudioCmd.GetCardVoicePath("Worldview"), "/worldview.wav")
        ];
        Require(splitMoment.EndsWith("/asplitmoment.wav", StringComparison.Ordinal), "A Split Moment voice route");
        Require(greetings.EndsWith("/greetings.wav", StringComparison.Ordinal), "Greetings voice route");
        Require(tiredness.EndsWith("/tiredness.wav", StringComparison.Ordinal), "Tiredness voice route");
        Require(innerCry.EndsWith("/innercry.wav", StringComparison.Ordinal), "Inner Cry voice route");
        Require(phantomOfTaki.EndsWith("/phantomoftaki.wav", StringComparison.Ordinal),
            "Phantom of Taki voice route");
        Require(phantomOfTomori.EndsWith("/phantomoftomori.wav", StringComparison.Ordinal),
            "Phantom of Tomori voice route");
        Require(heartsBarrier.EndsWith("/heartsbarrier.wav", StringComparison.Ordinal),
            "Heart's Barrier voice route");
        Require(accomplice.EndsWith("/accomplice.wav", StringComparison.Ordinal), "Accomplice voice route");
        Require(carefree.EndsWith("/carefree.wav", StringComparison.Ordinal), "Carefree voice route");
        Require(desuWa.EndsWith("/desuwa.wav", StringComparison.Ordinal), "Desu Wa voice route");
        Require(edgeOfBreakdown.EndsWith("/edgeofbreakdown.wav", StringComparison.Ordinal),
            "Edge of Breakdown voice route");
        Require(clockOut.EndsWith("/clockout.wav", StringComparison.Ordinal), "Clock Out voice route");
        Require(fallenFlowers.EndsWith("/fallenflowers.wav", StringComparison.Ordinal),
            "Fallen Flowers voice route");
        Require(phantomOfMutsumi.EndsWith("/phantomofmutsumi.wav", StringComparison.Ordinal),
            "Phantom of Mutsumi voice route");
        Require(phantomOfSoyo.EndsWith("/phantomofsoyo.wav", StringComparison.Ordinal),
            "Phantom of Soyo voice route");
        Require(rhinocerosBeetle.EndsWith("/rhinocerosbeetle.wav", StringComparison.Ordinal),
            "Rhinoceros Beetle voice route");
        Require(countingStars.EndsWith("/countingstars.wav", StringComparison.Ordinal),
            "Counting Stars voice route");
        Require(endurance.EndsWith("/endurance.wav", StringComparison.Ordinal), "Endurance voice route");
        Require(fearless.EndsWith("/fearless.wav", StringComparison.Ordinal), "Fearless voice route");
        Require(ourSong.EndsWith("/oursong.wav", StringComparison.Ordinal), "Our Song voice route");
        Require(perdereOmnia.EndsWith("/perdereomnia.wav", StringComparison.Ordinal),
            "Perdere Omnia voice route");
        Require(primoDieInScaena.EndsWith("/primodieinscaena.wav", StringComparison.Ordinal),
            "Primo Die In Scaena voice route");
        Require(sharedDestiny.EndsWith("/shareddestiny.wav", StringComparison.Ordinal),
            "Shared Destiny voice route");
        Require(theGirlWithFlaxenHair.EndsWith("/thegirlwithflaxenhair.wav", StringComparison.Ordinal),
            "The Girl with Flaxen Hair voice route");
        Require(wishToBecomeHuman.EndsWith("/wishtobecomehuman.wav", StringComparison.Ordinal),
            "Wish to Become Human voice route");
        Require(ResourceLoader.Exists(splitMoment, "AudioStream"), "A Split Moment voice resource");
        Require(ResourceLoader.Exists(greetings, "AudioStream"), "Greetings voice resource");
        Require(ResourceLoader.Exists(tiredness, "AudioStream"), "Tiredness voice resource");
        Require(ResourceLoader.Exists(innerCry, "AudioStream"), "Inner Cry voice resource");
        Require(ResourceLoader.Exists(phantomOfTaki, "AudioStream"), "Phantom of Taki voice resource");
        Require(ResourceLoader.Exists(phantomOfTomori, "AudioStream"), "Phantom of Tomori voice resource");
        Require(ResourceLoader.Exists(heartsBarrier, "AudioStream"), "Heart's Barrier voice resource");
        Require(ResourceLoader.Exists(accomplice, "AudioStream"), "Accomplice voice resource");
        Require(ResourceLoader.Exists(carefree, "AudioStream"), "Carefree voice resource");
        Require(ResourceLoader.Exists(desuWa, "AudioStream"), "Desu Wa voice resource");
        Require(ResourceLoader.Exists(edgeOfBreakdown, "AudioStream"),
            "Edge of Breakdown voice resource");
        Require(ResourceLoader.Exists(clockOut, "AudioStream"), "Clock Out voice resource");
        Require(ResourceLoader.Exists(fallenFlowers, "AudioStream"), "Fallen Flowers voice resource");
        Require(ResourceLoader.Exists(phantomOfMutsumi, "AudioStream"),
            "Phantom of Mutsumi voice resource");
        Require(ResourceLoader.Exists(phantomOfSoyo, "AudioStream"), "Phantom of Soyo voice resource");
        Require(ResourceLoader.Exists(rhinocerosBeetle, "AudioStream"),
            "Rhinoceros Beetle voice resource");
        Require(ResourceLoader.Exists(countingStars, "AudioStream"), "Counting Stars voice resource");
        Require(ResourceLoader.Exists(endurance, "AudioStream"), "Endurance voice resource");
        Require(ResourceLoader.Exists(fearless, "AudioStream"), "Fearless voice resource");
        Require(ResourceLoader.Exists(ourSong, "AudioStream"), "Our Song voice resource");
        Require(ResourceLoader.Exists(perdereOmnia, "AudioStream"), "Perdere Omnia voice resource");
        Require(ResourceLoader.Exists(primoDieInScaena, "AudioStream"),
            "Primo Die In Scaena voice resource");
        Require(ResourceLoader.Exists(sharedDestiny, "AudioStream"), "Shared Destiny voice resource");
        Require(ResourceLoader.Exists(theGirlWithFlaxenHair, "AudioStream"),
            "The Girl with Flaxen Hair voice resource");
        Require(ResourceLoader.Exists(wishToBecomeHuman, "AudioStream"),
            "Wish to Become Human voice resource");
        foreach ((string path, string suffix) in rareRoutes)
        {
            Require(path.EndsWith(suffix, StringComparison.Ordinal), $"rare voice route {suffix}");
            Require(ResourceLoader.Exists(path, "AudioStream"), $"rare voice resource {suffix}");
        }
    }

    private static T Upgraded<T>(T canonical) where T : CardModel
    {
        T card = (T)canonical.ToMutable();
        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
        return card;
    }

    private static void Require(bool condition, string label)
    {
        _assertions++;
        if (!condition)
        {
            throw new InvalidOperationException($"Phase N5 pure contract failed: {label}.");
        }
    }
}
