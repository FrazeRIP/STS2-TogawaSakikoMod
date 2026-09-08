using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.PotionPools;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Content;

internal static class NativeModelCatalog
{
    public const int GameplayModelCount = 139;

    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        ModHelper.AddModelToPool<TogawaSakikoCardPool, StrikeTogawaSakiko>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DefendTogawaSakiko>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TheMoonlightSonataCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, ASplitMomentCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DesireCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TwoMoonsCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SilentFarewellCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, GreetingsCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TirednessCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, MelodyCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, IdealCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, ProtectionCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, RadianceCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, KindnessCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, AmorisCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DolorisCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, MortisCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, OblivionisCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TimorisCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, BlackKeysCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, WhiteKeysCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, BlackAndWhiteKeysCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, VoiceCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, InnerCryCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, MementoMoriCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, BudgetBentoCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PhantomOfSakikoCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PhantomOfTakiCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PhantomOfTomoriCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SymbolIIAirCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DarkHeavenCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, GeorgetteMeGeorgetteYouCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, HeartsBarrierCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DatenCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, KillKiSSCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SymbolIVEarthCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, QuaerereLuminaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, KingsCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, AccompliceCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CarefreeCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, DesuWaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, EdgeOfBreakdownCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, MasqueradeRhapsodyRequestCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, WeaknessCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, ClockOutCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, FallenFlowersCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, HachibouseiDanceCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PhantomOfMutsumiCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PhantomOfSoyoCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, RhinocerosBeetleCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CountingStarsCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, AleaIactaEstCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, AnglesCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, ChoirSChoirCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CrucifixXCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CuriosityCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, EnduranceCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, FearlessCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, KaoCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, OurSongCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PerdereOmniaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PrimoDieInScaenaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SeizeTheFateCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SharedDestinyCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SymbolIFireCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SymbolIIIWaterCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, TheGirlWithFlaxenHairCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, UtopiaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, VeritasCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, WishFulfilledCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, WishToBecomeHumanCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, AsYourHeartDesiresCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, AveMujicaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, BandInvitationCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, BlackBirthdayCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CharismaticFormCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CrueltyCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, CrychicCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, EtherCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, ImprisonedXIICard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, MasksCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PerfectionCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, PrideCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SoraNoMusicaCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, SpringSunlightCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, StayEleganceCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, WishYouGoodLuckCard>();
        ModHelper.AddModelToPool<TogawaSakikoCardPool, WorldviewCard>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, StarterRelicTogawaSakiko>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, BlazingHairband>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, AnotherMask>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, ColorfulNotebook>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, CuteAnimalBandAid>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, FountainDrink>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, GoldenPocketWatch>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, MasqueradeMask>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, TheCompass>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, TheDoll>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, TheThirdMovement>();
        ModHelper.AddModelToPool<TogawaSakikoRelicPool, WarmthInfusedPorcelainCup>();
        ModHelper.AddModelToPool<SharedPotionPool, ChocolateMilkJelly>();
        ModHelper.AddModelToPool<SharedPotionPool, FreshlySqueezedCucumber>();
        ModHelper.AddModelToPool<SharedPotionPool, OrangeMilkJelly>();
        ModHelper.AddModelToPool<TogawaSakikoPotionPool, EarlGreyTea>();
        ModHelper.AddModelToPool<TogawaSakikoPotionPool, HallucinationPotion>();
        ModHelper.AddModelToPool<TogawaSakikoPotionPool, MatchaParfait>();

        _registered = true;
    }
}
