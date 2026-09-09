using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N6PureContractTests
{
    private static int _assertionCount;

    public static int AssertionCount => _assertionCount;

    public static void Run()
    {
        _assertionCount = 0;
        ValidateRelicModels();
        ValidateRandomCardEligibility();
        ValidatePersistentCounters();
        ValidateSelectionLogic();
        ValidatePotionModels();
        ValidatePools();
    }

    private static void ValidateRelicModels()
    {
        Require(ModelDb.Relic<BlazingHairband>().Rarity == RelicRarity.Ancient, "Blazing Hairband rarity");
        Require(ModelDb.Relic<ColorfulNotebook>().Rarity == RelicRarity.Common, "Colorful Notebook rarity");
        Require(ModelDb.Relic<CuteAnimalBandAid>().Rarity == RelicRarity.Common, "Cute Animal Band-Aid rarity");
        Require(ModelDb.Relic<FountainDrink>().Rarity == RelicRarity.Common, "Fountain Drink rarity");
        Require(ModelDb.Relic<GoldenPocketWatch>().Rarity == RelicRarity.Uncommon, "Golden Pocket Watch rarity");
        Require(ModelDb.Relic<MasqueradeMask>().Rarity == RelicRarity.Rare, "Masquerade Mask rarity");
        Require(ModelDb.Relic<TheCompass>().Rarity == RelicRarity.Shop, "The Compass rarity");
        Require(ModelDb.Relic<TheDoll>().Rarity == RelicRarity.Uncommon, "The Doll rarity");
        Require(ModelDb.Relic<TheThirdMovement>().Rarity == RelicRarity.Event, "The Third Movement rarity");
        Require(ModelDb.Relic<WarmthInfusedPorcelainCup>().Rarity == RelicRarity.Ancient, "Porcelain Cup rarity");

        Require(BlazingHairband.IsEligibleRandomCard(ModelDb.Card<StrikeTogawaSakiko>()),
            "Blazing Hairband accepts enabled cards");
        Require(!BlazingHairband.IsEligibleRandomCard(ModelDb.Card<CarefreeCard>()),
            "Blazing Hairband excludes disabled Carefree");
        Require(!BlazingHairband.IsEligibleRandomCard(ModelDb.Card<WeaknessCard>()),
            "Blazing Hairband excludes disabled Weakness");

        Require(FountainDrink.ShouldForceFor(true, MegaCrit.Sts2.Core.Rooms.RoomType.Monster, false),
            "Fountain Drink forces a full-slot combat reward");
        Require(!FountainDrink.ShouldForceFor(true, MegaCrit.Sts2.Core.Rooms.RoomType.Monster, true),
            "Fountain Drink does not force with an open slot");
        Require(!FountainDrink.ShouldForceFor(false, MegaCrit.Sts2.Core.Rooms.RoomType.Monster, false),
            "Fountain Drink rejects another owner");
        Require(!FountainDrink.ShouldForceFor(true, MegaCrit.Sts2.Core.Rooms.RoomType.Shop, false),
            "Fountain Drink rejects noncombat rewards");
    }

    private static void ValidateRandomCardEligibility()
    {
        CardModel[] characterCards = ModelDb.CardPool<TogawaSakikoCardPool>().AllCards.ToArray();
        CardModel[] curses = characterCards.Where(card => card.Type == CardType.Curse).ToArray();
        CardModel[] statuses = ModelDb.CardPool<StatusCardPool>().AllCards.ToArray();
        Require(curses.Length == 6, "random generation probes cover all six Sakiko curses");
        Require(statuses.Length > 0 && statuses.All(card => card.Type == CardType.Status),
            "random generation probes cover native statuses");

        foreach (CardModel card in curses.Concat(statuses))
        {
            Require(!BlazingHairband.IsEligibleRandomCard(card), $"Hairband excludes {card.Id}");
            Require(!PerfectionCard.IsEligibleRandomCard(card), $"Perfection excludes {card.Id}");
        }

        CardModel[] generatedOnlyCards =
        [
            ModelDb.Card<DesireCard>(), ModelDb.Card<TirednessCard>(), ModelDb.Card<MelodyCard>(),
            ModelDb.Card<IdealCard>(), ModelDb.Card<ProtectionCard>(), ModelDb.Card<RadianceCard>(),
            ModelDb.Card<KindnessCard>(), ModelDb.Card<VoiceCard>(),
            ModelDb.Card<BlackKeysCard>(), ModelDb.Card<WhiteKeysCard>()
        ];
        foreach (CardModel card in generatedOnlyCards)
        {
            Require(card.Rarity == CardRarity.Token && characterCards.Contains(card),
                $"generated-only {card.Id} remains registered as a token");
            Require(!card.CanBeGeneratedInCombat && !card.CanBeGeneratedByModifiers,
                $"generated-only {card.Id} opts out of generic combat and modifier generation");
            Require(!CardFactory.FilterForCombat([card]).Any(),
                $"native combat generation excludes {card.Id}");
        }

        foreach ((string name, Func<CardModel, bool> filter) in new (string, Func<CardModel, bool>)[]
        {
            ("Hairband", BlazingHairband.IsEligibleRandomCard),
            ("Perfection", PerfectionCard.IsEligibleRandomCard)
        })
        {
            CardModel[] previousPool = CardFactory.FilterForCombat(characterCards)
                .Where(card => name != "Hairband" || card is not CarefreeCard and not WeaknessCard)
                .ToArray();
            HashSet<CardModel> expected = previousPool
                .Where(card => card.Type is not CardType.Curse and not CardType.Status).ToHashSet();
            HashSet<CardModel> actual = CardFactory.FilterForCombat(characterCards.Where(filter)).ToHashSet();
            Require(actual.Count > 0 && actual.SetEquals(expected),
                $"{name} removes only curses and statuses from its complete previous candidate pool");
            foreach (CardType type in new[] { CardType.Attack, CardType.Skill, CardType.Power })
            {
                Require(actual.Any(card => card.Type == type), $"{name} retains {type} candidates");
            }
            foreach (CardModel token in generatedOnlyCards)
            {
                Require(!actual.Contains(token), $"{name} excludes generated-only token {token.Id}");
            }
        }
    }

    private static void ValidatePersistentCounters()
    {
        GoldenPocketWatch watch = (GoldenPocketWatch)ModelDb.Relic<GoldenPocketWatch>().ToMutable();
        Require(watch.CardsPlayed == 0 && watch.DisplayAmount == 0, "Golden Pocket Watch initial counter");
        watch.CardsPlayed = 11;
        Require(watch.DisplayAmount == 11 && watch.Status == RelicStatus.Active,
            "Golden Pocket Watch pre-trigger display");
        Require(HasSavedProperty<GoldenPocketWatch>(nameof(GoldenPocketWatch.CardsPlayed)),
            "Golden Pocket Watch counter persistence");

        MasqueradeMask mask = (MasqueradeMask)ModelDb.Relic<MasqueradeMask>().ToMutable();
        (int firstRemaining, int firstRewards) = MasqueradeMask.AdvanceCounter(1, 1);
        (int secondRemaining, int secondRewards) = MasqueradeMask.AdvanceCounter(2, 1);
        (int multiRemaining, int multiRewards) = MasqueradeMask.AdvanceCounter(2, 7);
        Require(firstRemaining == 2 && firstRewards == 0, "Masquerade Mask partial counter");
        Require(secondRemaining == 0 && secondRewards == 1, "Masquerade Mask threshold");
        Require(multiRemaining == 0 && multiRewards == 3, "Masquerade Mask recursive thresholds");
        Require(mask.PurgedCards == 0 && HasSavedProperty<MasqueradeMask>(nameof(MasqueradeMask.PurgedCards)),
            "Masquerade Mask counter persistence");

        TheDoll doll = (TheDoll)ModelDb.Relic<TheDoll>().ToMutable();
        Require(doll.TurnsElapsed == 0 && HasSavedProperty<TheDoll>(nameof(TheDoll.TurnsElapsed)),
            "The Doll combat counter persistence");

        TheThirdMovement movement = (TheThirdMovement)ModelDb.Relic<TheThirdMovement>().ToMutable();
        Require(movement.RemainingUses == TheThirdMovement.StartingUses && !movement.IsUsedUp,
            "The Third Movement initial uses");
        Require(TheThirdMovement.IsEligibleDamage(true, ValueProp.Move, true, ModelDb.Card<TheMoonlightSonataCard>()),
            "The Third Movement accepts owner Moonlight Sonata attack damage");
        Require(!TheThirdMovement.IsEligibleDamage(false, ValueProp.Move, true, ModelDb.Card<TheMoonlightSonataCard>()),
            "The Third Movement stops after three uses");
        Require(!TheThirdMovement.IsEligibleDamage(true, ValueProp.Unpowered, true, ModelDb.Card<TheMoonlightSonataCard>()),
            "The Third Movement rejects unpowered damage");
        Require(!TheThirdMovement.IsEligibleDamage(true, ValueProp.Move, false, ModelDb.Card<TheMoonlightSonataCard>()),
            "The Third Movement rejects another dealer");
        Require(!TheThirdMovement.IsEligibleDamage(true, ValueProp.Move, true, ModelDb.Card<StrikeTogawaSakiko>()),
            "The Third Movement rejects other cards");
        movement.RemainingUses = 0;
        Require(movement.IsUsedUp && movement.DisplayAmount == 0 && movement.Status == RelicStatus.Disabled,
            "The Third Movement used-up state");
        Require(HasSavedProperty<TheThirdMovement>(nameof(TheThirdMovement.RemainingUses)),
            "The Third Movement use persistence");
    }

    private static void ValidateSelectionLogic()
    {
        Require(!WarmthInfusedPorcelainCup.ShouldForceOption(false, 3, false, 1f),
            "Porcelain Cup owner isolation");
        Require(!WarmthInfusedPorcelainCup.ShouldForceOption(true, 0, false, 1f),
            "Porcelain Cup zero-option guard");
        Require(!WarmthInfusedPorcelainCup.ShouldForceOption(true, 3, true, 1f),
            "Porcelain Cup duplicate guard");
        Require(!WarmthInfusedPorcelainCup.ShouldForceOption(true, 3, false, 0.499f),
            "Porcelain Cup failed roll");
        Require(WarmthInfusedPorcelainCup.ShouldForceOption(true, 3, false, 0.5f),
            "Porcelain Cup threshold roll");
        Require(WarmthInfusedPorcelainCup.ShouldForceOption(true, 2, false, 1f),
            "Porcelain Cup preserves a Kings-reduced nonempty reward");
    }

    private static void ValidatePotionModels()
    {
        ValidatePotion<ChocolateMilkJelly>(PotionRarity.Uncommon, 1);
        ValidatePotion<EarlGreyTea>(PotionRarity.Common, 2);
        ValidatePotion<FreshlySqueezedCucumber>(PotionRarity.Uncommon, 20);
        ValidatePotion<HallucinationPotion>(PotionRarity.Common, 5);
        ValidatePotion<MatchaParfait>(PotionRarity.Uncommon, 1);
        ValidatePotion<OrangeMilkJelly>(PotionRarity.Uncommon, 1);
        Require(MatchaParfait.IsDamageInRange(6m) && MatchaParfait.IsDamageInRange(30m),
            "Matcha Parfait inclusive endpoints");
        Require(!MatchaParfait.IsDamageInRange(5m) && !MatchaParfait.IsDamageInRange(31m),
            "Matcha Parfait range rejection");

        DazzlingDownPower dazzlingDown = ModelDb.Power<DazzlingDownPower>();
        FreshlySqueezedCucumberPower cucumber = ModelDb.Power<FreshlySqueezedCucumberPower>();
        Require(dazzlingDown.Type == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff &&
                dazzlingDown.StackType == MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter,
            "Dazzling Down power contract");
        Require(cucumber.Type == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff &&
                cucumber.StackType == MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter,
            "Freshly Squeezed Cucumber power contract");
    }

    private static void ValidatePools()
    {
        Type[] unlockedRelics = ModelDb.RelicPool<TogawaSakikoRelicPool>()
            .GetUnlockedRelics(UnlockState.none)
            .Select(relic => relic.GetType())
            .ToArray();
        Require(unlockedRelics.Length == 9, "Sakiko unlocked relic count");
        Require(!unlockedRelics.Contains(typeof(StarterRelicTogawaSakiko)),
            "starter relic excluded from rewards");
        Require(!unlockedRelics.Contains(typeof(TheThirdMovement)),
            "event relic excluded from random rewards");
        Require(unlockedRelics.Contains(typeof(BlazingHairband)) &&
                unlockedRelics.Contains(typeof(WarmthInfusedPorcelainCup)),
            "Sakiko Ancient relic registration");

        Type[] characterPotions = ModelDb.PotionPool<TogawaSakikoPotionPool>()
            .GetUnlockedPotions(UnlockState.none)
            .Select(potion => potion.GetType())
            .ToArray();
        Require(characterPotions.SequenceEqual(
                new[] { typeof(EarlGreyTea), typeof(HallucinationPotion), typeof(MatchaParfait) }),
            "Sakiko-only potion pool");

        Type[] sharedPotions = ModelDb.PotionPool<SharedPotionPool>()
            .GetUnlockedPotions(UnlockState.none)
            .Select(potion => potion.GetType())
            .ToArray();
        Require(sharedPotions.Contains(typeof(ChocolateMilkJelly)) &&
                sharedPotions.Contains(typeof(FreshlySqueezedCucumber)) &&
                sharedPotions.Contains(typeof(OrangeMilkJelly)),
            "originally unrestricted potions remain shared");
    }

    private static void ValidatePotion<TPotion>(PotionRarity rarity, int potency)
        where TPotion : PotionModel
    {
        PotionModel potion = ModelDb.Potion<TPotion>();
        Require(potion.Rarity == rarity, typeof(TPotion).Name + " rarity");
        Require(potion.Usage == PotionUsage.CombatOnly, typeof(TPotion).Name + " combat-only usage");
        Require(potion.TargetType == TargetType.Self, typeof(TPotion).Name + " self target");
        Require(potion.DynamicVars["Potency"].IntValue == potency, typeof(TPotion).Name + " potency");
    }

    private static bool HasSavedProperty<TModel>(string propertyName)
    {
        return typeof(TModel).GetProperty(propertyName)?.GetCustomAttribute<SavedPropertyAttribute>() is not null;
    }

    private static void Require(bool condition, string contract)
    {
        _assertionCount++;
        if (!condition)
        {
            throw new InvalidOperationException("Phase N6 pure contract failed: " + contract + ".");
        }
    }
}
