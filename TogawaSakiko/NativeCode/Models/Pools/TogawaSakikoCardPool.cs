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

    internal static bool IsEnabledCard(CardModel card)
    {
        return
            card is TheThirdMovementCard or ASplitMomentCard or TwoMoonsCard or SilentFarewellCard or GreetingsCard or
                BlackAndWhiteKeysCard or InnerCryCard or MementoMoriCard or BudgetBentoCard or
                PhantomOfSakikoCard or PhantomOfTakiCard or PhantomOfTomoriCard or SymbolIIAirCard or
                DarkHeavenCard or GeorgetteMeGeorgetteYouCard or HeartsBarrierCard or DatenCard or
                KillKiSSCard or SymbolIVEarthCard or QuaerereLuminaCard or KingsCard or
                AccompliceCard or DesuWaCard or EdgeOfBreakdownCard or MasqueradeRhapsodyRequestCard or
                ClockOutCard or FallenFlowersCard or HachibouseiDanceCard or PhantomOfMutsumiCard or
                PhantomOfSoyoCard or RhinocerosBeetleCard or CountingStarsCard or AleaIactaEstCard or
                AnglesCard or ChoirSChoirCard or CrucifixXCard or CuriosityCard or EnduranceCard or
                FearlessCard or KaoCard or OurSongCard or PerdereOmniaCard or PrimoDieInScaenaCard or
                SeizeTheFateCard or SharedDestinyCard or SymbolIFireCard or SymbolIIIWaterCard or
                TheGirlWithFlaxenHairCard or UtopiaCard or VeritasCard or WishFulfilledCard or
                WishToBecomeHumanCard or AsYourHeartDesiresCard or AveMujicaCard or BandInvitationCard or
                BlackBirthdayCard or CharismaticFormCard or CrueltyCard or CrychicCard or EtherCard or
                ImprisonedXIICard or MasksCard or PerfectionCard or PrideCard or SoraNoMusicaCard or
                SpringSunlightCard or StayEleganceCard or WishYouGoodLuckCard or WorldviewCard;
    }
}
