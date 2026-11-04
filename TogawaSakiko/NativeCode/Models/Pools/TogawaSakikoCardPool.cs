using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Pools;

public sealed class TogawaSakikoCardPool : CardPoolModel
{
    public override string Title => "TogawaSakiko";

    public override string EnergyColorName => "togawa_sakiko";

    public override string CardFrameMaterialPath => "card_frame_togawa_sakiko";

    public override Color DeckEntryCardColor => new("8295A8");

    public override Color EnergyOutlineColor => new("1D5673");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return Array.Empty<CardModel>();
    }

    protected override IEnumerable<CardModel> FilterThroughEpochs(
        UnlockState unlockState,
        IEnumerable<CardModel> cards)
    {
        return cards.Where(IsEnabledCard);
    }

    // Starter and generated cards stay inspectable without entering reward or generation pools.
    internal static bool IsVisibleInLibrary(CardModel card)
    {
        return IsEnabledCard(card) ||
            card is StrikeTogawaSakiko or DefendTogawaSakiko or TheMoonlightSonataCard or
                DesireCard or TirednessCard or MelodyCard or IdealCard or ProtectionCard or
                RadianceCard or KindnessCard or VoiceCard or AmorisCard or DolorisCard or
                MortisCard or OblivionisCard or TimorisCard;
    }

    internal static bool IsEnabledCard(CardModel card)
    {
        return
            card is TheThirdMovementCard or ASplitMomentCard or TwoMoonsCard or GreetingsCard or
                InnerCryCard or MementoMoriCard or PhantomOfSakikoCard or PhantomOfTakiCard or
                PhantomOfTomoriCard or SymbolIIAirCard or DarkHeavenCard or GeorgetteMeGeorgetteYouCard or
                HeartsBarrierCard or DatenCard or KillKiSSCard or SymbolIVEarthCard or KingsCard or
                AccompliceCard or DesuWaCard or MasqueradeRhapsodyRequestCard or FallenFlowersCard or
                HachibouseiDanceCard or PhantomOfMutsumiCard or PhantomOfSoyoCard or RhinocerosBeetleCard or
                CountingStarsCard or AnglesCard or ChoirSChoirCard or CrucifixXCard or CuriosityCard or
                ForeverDreamCard or DivineCard or InYourBlueEyesCard or ASongOfRomanceCard or
                TheWholeBlueWorldCard or SophieCard or GodsBloodCard or UnnamedOneCard or UnnamedTwoCard or
                UnnamedThreeCard or UnnamedFourCard or UnnamedFiveCard or UnnamedHypeCard or
                SlashTheWayCard or UnrestrainedCard or SweetDreamsCard or AllWillsBecomeOneCard or
                BaddyBaddyCard or SoftnessCard or EncoreCard or ImprovisationCard or BandAffairsCard or
                UnceasingMelodyCard or FearlessCard or KaoCard or OurSongCard or PerdereOmniaCard or
                PrimoDieInScaenaCard or SeizeTheFateCard or SharedDestinyCard or SymbolIFireCard or
                SymbolIIIWaterCard or TheGirlWithFlaxenHairCard or UtopiaCard or WishFulfilledCard or
                WishToBecomeHumanCard or AsYourHeartDesiresCard or AveMujicaCard or BandInvitationCard or
                BlackBirthdayCard or SuccubusFormCard or CharismaticFormCard or CrueltyCard or CrychicCard or
                EtherCard or ImprisonedXIICard or MasksCard or PerfectionCard or PrideCard or
                SoraNoMusicaCard or SpringSunlightCard or StayEleganceCard or WishYouGoodLuckCard or
                WorldviewCard;
    }
}
