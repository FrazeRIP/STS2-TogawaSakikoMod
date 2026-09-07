using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N5PureContractTests
{
    public const int AssertionCount = 28;
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

    private static void ValidateVoiceRoutes()
    {
        string splitMoment = SakikoAudioCmd.GetCardVoicePath("ASplitMoment");
        string greetings = SakikoAudioCmd.GetCardVoicePath("Greetings");
        string tiredness = SakikoAudioCmd.GetCardVoicePath("Tiredness");
        Require(splitMoment.EndsWith("/asplitmoment.wav", StringComparison.Ordinal), "A Split Moment voice route");
        Require(greetings.EndsWith("/greetings.wav", StringComparison.Ordinal), "Greetings voice route");
        Require(tiredness.EndsWith("/tiredness.wav", StringComparison.Ordinal), "Tiredness voice route");
        Require(ResourceLoader.Exists(splitMoment, "AudioStream"), "A Split Moment voice resource");
        Require(ResourceLoader.Exists(greetings, "AudioStream"), "Greetings voice resource");
        Require(ResourceLoader.Exists(tiredness, "AudioStream"), "Tiredness voice resource");
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
