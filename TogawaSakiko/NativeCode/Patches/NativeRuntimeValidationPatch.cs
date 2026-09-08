using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Potions;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;
using GameLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]
internal static class NativeRuntimeValidationPatch
{
    private static readonly GameLogger Logger = new(Bootstrap.ModEntryPoint.ModId, LogType.Generic);

    private static readonly string[] RequiredAssets =
    [
        NativeAssetPaths.CharacterIconScene,
        NativeAssetPaths.CharacterSelectBackground,
        NativeAssetPaths.CharacterVisualsPreload,
        NativeAssetPaths.CharacterPortrait,
        NativeAssetPaths.CharacterRestSitePortrait,
        NativeAssetPaths.CharacterIcon,
        NativeAssetPaths.CharacterIconOutline,
        NativeAssetPaths.CharacterSelectIcon,
        NativeAssetPaths.CharacterSelectLockedIcon,
        NativeAssetPaths.CharacterMapMarker,
        NativeAssetPaths.CharacterEnergyCounter,
        NativeAssetPaths.CharacterMerchant,
        NativeAssetPaths.CharacterRestSite,
        NativeAssetPaths.CharacterTrail,
        NativeAssetPaths.CharacterTransitionMaterial,
        NativeAssetPaths.CharacterTransitionTexture,
        NativeAssetPaths.CharacterArmPoint,
        NativeAssetPaths.CharacterArmRock,
        NativeAssetPaths.CharacterArmPaper,
        NativeAssetPaths.CharacterArmScissors,
        NativeAssetPaths.EnergyIcon,
        NativeAssetPaths.RichTextEnergyIcon,
        NativeAssetPaths.CardFrameMaterial,
        NativeAssetPaths.AttackCardFrame,
        NativeAssetPaths.SkillCardFrame,
        NativeAssetPaths.PowerCardFrame,
        NativeAssetPaths.StrikePortrait,
        NativeAssetPaths.DefendPortrait,
        NativeAssetPaths.MoonlightSonataPortrait,
        NativeAssetPaths.SplitMomentPortrait,
        NativeAssetPaths.DesirePortrait,
        NativeAssetPaths.TwoMoonsPortrait,
        NativeAssetPaths.SilentFarewellPortrait,
        NativeAssetPaths.GreetingsPortrait,
        NativeAssetPaths.TirednessPortrait,
        NativeAssetPaths.MelodyPortrait,
        NativeAssetPaths.IdealPortrait,
        NativeAssetPaths.ProtectionPortrait,
        NativeAssetPaths.RadiancePortrait,
        NativeAssetPaths.KindnessPortrait,
        NativeAssetPaths.AmorisPortrait,
        NativeAssetPaths.DolorisPortrait,
        NativeAssetPaths.MortisPortrait,
        NativeAssetPaths.OblivionisPortrait,
        NativeAssetPaths.TimorisPortrait,
        NativeAssetPaths.BlackKeysPortrait,
        NativeAssetPaths.WhiteKeysPortrait,
        NativeAssetPaths.BlackAndWhiteKeysPortrait,
        NativeAssetPaths.VoicePortrait,
        NativeAssetPaths.InnerCryPortrait,
        NativeAssetPaths.MementoMoriPortrait,
        NativeAssetPaths.BudgetBentoPortrait,
        NativeAssetPaths.PhantomOfSakikoPortrait,
        NativeAssetPaths.PhantomOfTakiPortrait,
        NativeAssetPaths.PhantomOfTomoriPortrait,
        NativeAssetPaths.SymbolIIAirPortrait,
        NativeAssetPaths.DarkHeavenPortrait,
        NativeAssetPaths.GeorgetteMeGeorgetteYouPortrait,
        NativeAssetPaths.HeartsBarrierPortrait,
        NativeAssetPaths.DatenPortrait,
        NativeAssetPaths.KillKiSSPortrait,
        NativeAssetPaths.SymbolIVEarthPortrait,
        NativeAssetPaths.QuaerereLuminaPortrait,
        NativeAssetPaths.KingsPortrait,
        NativeAssetPaths.AccomplicePortrait,
        NativeAssetPaths.CarefreePortrait,
        NativeAssetPaths.DesuWaPortrait,
        NativeAssetPaths.EdgeOfBreakdownPortrait,
        NativeAssetPaths.MasqueradeRhapsodyRequestPortrait,
        NativeAssetPaths.WeaknessPortrait,
        NativeAssetPaths.ClockOutPortrait,
        NativeAssetPaths.FallenFlowersPortrait,
        NativeAssetPaths.HachibouseiDancePortrait,
        NativeAssetPaths.PhantomOfMutsumiPortrait,
        NativeAssetPaths.PhantomOfSoyoPortrait,
        NativeAssetPaths.RhinocerosBeetlePortrait,
        NativeAssetPaths.CountingStarsPortrait,
        NativeAssetPaths.AleaIactaEstPortrait,
        NativeAssetPaths.AnglesPortrait,
        NativeAssetPaths.ChoirSChoirPortrait,
        NativeAssetPaths.CrucifixXPortrait,
        NativeAssetPaths.CuriosityPortrait,
        NativeAssetPaths.EndurancePortrait,
        NativeAssetPaths.FearlessPortrait,
        NativeAssetPaths.KaoPortrait,
        NativeAssetPaths.OurSongPortrait,
        NativeAssetPaths.PerdereOmniaPortrait,
        NativeAssetPaths.PrimoDieInScaenaPortrait,
        NativeAssetPaths.SeizeTheFatePortrait,
        NativeAssetPaths.SharedDestinyPortrait,
        NativeAssetPaths.SymbolIFirePortrait,
        NativeAssetPaths.SymbolIIIWaterPortrait,
        NativeAssetPaths.TheGirlWithFlaxenHairPortrait,
        NativeAssetPaths.UtopiaPortrait,
        NativeAssetPaths.VeritasPortrait,
        NativeAssetPaths.WishFulfilledPortrait,
        NativeAssetPaths.WishToBecomeHumanPortrait,
        NativeAssetPaths.MonochromeHairbandIcon,
        NativeAssetPaths.MonochromeHairbandOutline,
        NativeAssetPaths.MonochromeHairbandBigIcon,
        NativeAssetPaths.DazzlingIcon,
        NativeAssetPaths.DazzlingBigIcon,
        NativeAssetPaths.HypeIcon,
        NativeAssetPaths.HypeBigIcon,
        NativeAssetPaths.DolorisIcon,
        NativeAssetPaths.DolorisBigIcon,
        NativeAssetPaths.MortisIcon,
        NativeAssetPaths.MortisBigIcon,
        NativeAssetPaths.OblivionisIcon,
        NativeAssetPaths.OblivionisBigIcon,
        NativeAssetPaths.TimorisIcon,
        NativeAssetPaths.TimorisBigIcon,
        NativeAssetPaths.MonsterDivinityIcon,
        NativeAssetPaths.MonsterDivinityBigIcon,
        NativeAssetPaths.KingsIcon,
        NativeAssetPaths.KingsBigIcon,
        NativeAssetPaths.EnduranceIcon,
        NativeAssetPaths.EnduranceBigIcon,
        NativeAssetPaths.FearlessIcon,
        NativeAssetPaths.FearlessBigIcon,
        NativeAssetPaths.GodsCreationIcon,
        NativeAssetPaths.GodsCreationBigIcon,
        NativeAssetPaths.GirlOfSpringIcon,
        NativeAssetPaths.GirlOfSpringBigIcon,
        NativeAssetPaths.OurSongIcon,
        NativeAssetPaths.OurSongBigIcon,
        NativeAssetPaths.PerdereOmniaIcon,
        NativeAssetPaths.PerdereOmniaBigIcon,
        NativeAssetPaths.PrimoDieInScaenaIcon,
        NativeAssetPaths.PrimoDieInScaenaBigIcon,
        NativeAssetPaths.SeizeTheFateIcon,
        NativeAssetPaths.SeizeTheFateBigIcon,
        NativeAssetPaths.SharedDestinyIcon,
        NativeAssetPaths.SharedDestinyBigIcon,
        NativeAssetPaths.AsYourHeartDesiresPortrait,
        NativeAssetPaths.AveMujicaPortrait,
        NativeAssetPaths.BandInvitationPortrait,
        NativeAssetPaths.BlackBirthdayPortrait,
        NativeAssetPaths.CharismaticFormPortrait,
        NativeAssetPaths.CrueltyPortrait,
        NativeAssetPaths.CrychicPortrait,
        NativeAssetPaths.EtherPortrait,
        NativeAssetPaths.ImprisonedXIIPortrait,
        NativeAssetPaths.MasksPortrait,
        NativeAssetPaths.PerfectionPortrait,
        NativeAssetPaths.PridePortrait,
        NativeAssetPaths.SoraNoMusicaPortrait,
        NativeAssetPaths.SpringSunlightPortrait,
        NativeAssetPaths.StayElegancePortrait,
        NativeAssetPaths.WishYouGoodLuckPortrait,
        NativeAssetPaths.WorldviewPortrait,
        NativeAssetPaths.CharismaticFormIcon,
        NativeAssetPaths.CharismaticFormBigIcon,
        NativeAssetPaths.CrueltyIcon,
        NativeAssetPaths.CrueltyBigIcon,
        NativeAssetPaths.CrychicIcon,
        NativeAssetPaths.CrychicBigIcon,
        NativeAssetPaths.PrideIcon,
        NativeAssetPaths.PrideBigIcon,
        NativeAssetPaths.WishYouGoodLuckIcon,
        NativeAssetPaths.WishYouGoodLuckBigIcon,
        NativeAssetPaths.WorldviewIcon,
        NativeAssetPaths.WorldviewBigIcon,
        NativeAssetPaths.DazzlingDownIcon,
        NativeAssetPaths.DazzlingDownBigIcon,
        NativeAssetPaths.FreshlySqueezedCucumberIcon,
        NativeAssetPaths.FreshlySqueezedCucumberBigIcon,
        NativeAssetPaths.RelicIcon("blazinghairband"),
        NativeAssetPaths.RelicOutline("blazinghairband"),
        NativeAssetPaths.RelicBigIcon("blazinghairband"),
        NativeAssetPaths.RelicIcon("colorfulnotebook"),
        NativeAssetPaths.RelicOutline("colorfulnotebook"),
        NativeAssetPaths.RelicBigIcon("colorfulnotebook"),
        NativeAssetPaths.RelicIcon("cuteanimalbandaid"),
        NativeAssetPaths.RelicOutline("cuteanimalbandaid"),
        NativeAssetPaths.RelicBigIcon("cuteanimalbandaid"),
        NativeAssetPaths.RelicIcon("fountaindrink"),
        NativeAssetPaths.RelicOutline("fountaindrink"),
        NativeAssetPaths.RelicBigIcon("fountaindrink"),
        NativeAssetPaths.RelicIcon("goldenpocketwatch"),
        NativeAssetPaths.RelicOutline("goldenpocketwatch"),
        NativeAssetPaths.RelicBigIcon("goldenpocketwatch"),
        NativeAssetPaths.RelicIcon("masquerademask"),
        NativeAssetPaths.RelicOutline("masquerademask"),
        NativeAssetPaths.RelicBigIcon("masquerademask"),
        NativeAssetPaths.RelicIcon("thecompass"),
        NativeAssetPaths.RelicOutline("thecompass"),
        NativeAssetPaths.RelicBigIcon("thecompass"),
        NativeAssetPaths.RelicIcon("thedoll"),
        NativeAssetPaths.RelicOutline("thedoll"),
        NativeAssetPaths.RelicBigIcon("thedoll"),
        NativeAssetPaths.RelicIcon("thethirdmovement"),
        NativeAssetPaths.RelicOutline("thethirdmovement"),
        NativeAssetPaths.RelicBigIcon("thethirdmovement"),
        NativeAssetPaths.RelicIcon("warmthinfusedporcelaincup"),
        NativeAssetPaths.RelicOutline("warmthinfusedporcelaincup"),
        NativeAssetPaths.RelicBigIcon("warmthinfusedporcelaincup"),
        NativeAssetPaths.PotionContainer("chocolatemilkjelly"),
        NativeAssetPaths.PotionOutline("chocolatemilkjelly"),
        NativeAssetPaths.PotionContainer("earlgreytea"),
        NativeAssetPaths.PotionOutline("earlgreytea"),
        NativeAssetPaths.PotionContainer("freshlysqueezedcucumber"),
        NativeAssetPaths.PotionOutline("freshlysqueezedcucumber"),
        NativeAssetPaths.PotionContainer("hallucinationpotion"),
        NativeAssetPaths.PotionOutline("hallucinationpotion"),
        NativeAssetPaths.PotionLiquid("hallucinationpotion"),
        NativeAssetPaths.PotionContainer("matchaparfait"),
        NativeAssetPaths.PotionOutline("matchaparfait"),
        NativeAssetPaths.PotionContainer("orangemilkjelly"),
        NativeAssetPaths.PotionOutline("orangemilkjelly")
    ];

    private static void Postfix()
    {
        ValidateStableIds();
        ValidateCharacterRegistration();
        ValidateAssets();
        ValidateLocalization();
        N3PureContractTests.Run();
        N5PureContractTests.Run();
        N6PureContractTests.Run();
        N7AudioPureContractTests.Run();
        N4LocalizationDiagnostics.RunIfRequested();
        N4PresentationDiagnostics.RunIfRequested();
        NativeSmokeTrace.MarkModelDbInitialized();

        Logger.Info(
            $"Native vertical slice ready. Language={LocManager.Instance.Language}, Models={NativeModelCatalog.GameplayModelCount}, ExternalModDependencies=0");
        Logger.Info($"Phase N3 pure contract tests passed ({N3PureContractTests.AssertionCount} assertions).");
        Logger.Info($"Phase N5 pure contract tests passed ({N5PureContractTests.AssertionCount} assertions).");
        Logger.Info($"Phase N6 pure contract tests passed ({N6PureContractTests.AssertionCount} assertions).");
        Logger.Info($"Phase N7 audio pure contract tests passed ({N7AudioPureContractTests.AssertionCount} assertions).");
    }

    private static void ValidateStableIds()
    {
        foreach ((Type type, string expectedEntry) in NativeStableIds.Entries)
        {
            ModelId expected = new(ModelDb.GetCategory(type), expectedEntry);
            ModelId actual = ModelDb.GetId(type);
            if (actual != expected)
            {
                throw new InvalidOperationException($"Native model ID mismatch for {type.FullName}: expected {expected}, found {actual}.");
            }

            AbstractModel? canonical = ModelDb.All.SingleOrDefault(model => model.GetType() == type);
            if (canonical is null || canonical.Id != expected)
            {
                throw new InvalidOperationException($"Canonical model was not registered with stable ID {expected}.");
            }
        }
    }

    private static void ValidateCharacterRegistration()
    {
        SakikoCharacter character = ModelDb.Character<SakikoCharacter>();
        if (!ModelDb.AllCharacters.Contains(character))
        {
            throw new InvalidOperationException("Togawa Sakiko is missing from ModelDb.AllCharacters.");
        }

        CardModel[] startingDeck = character.StartingDeck.ToArray();
        if (startingDeck.Length != 9 ||
            startingDeck.Count(card => card is StrikeTogawaSakiko) != 4 ||
            startingDeck.Count(card => card is DefendTogawaSakiko) != 4 ||
            startingDeck.Count(card => card is TheMoonlightSonataCard) != 1)
        {
            throw new InvalidOperationException("Togawa Sakiko's native starting deck does not match the STS1 source of truth.");
        }

        if (character.StartingRelics.Count != 1 || character.StartingRelics[0] is not StarterRelicTogawaSakiko)
        {
            throw new InvalidOperationException("Togawa Sakiko's native starter relic is not registered correctly.");
        }

        Type[] rewardCardTypes = character.CardPool
            .GetUnlockedCards(MegaCrit.Sts2.Core.Unlocks.UnlockState.none, MegaCrit.Sts2.Core.Entities.Cards.CardMultiplayerConstraint.None)
            .Select(card => card.GetType())
            .ToArray();
        Type[] expectedRewardCardTypes =
        [
            typeof(ASplitMomentCard),
            typeof(TwoMoonsCard),
            typeof(SilentFarewellCard),
            typeof(GreetingsCard),
            typeof(BlackAndWhiteKeysCard),
            typeof(InnerCryCard),
            typeof(MementoMoriCard),
            typeof(BudgetBentoCard),
            typeof(PhantomOfSakikoCard),
            typeof(PhantomOfTakiCard),
            typeof(PhantomOfTomoriCard),
            typeof(SymbolIIAirCard),
            typeof(DarkHeavenCard),
            typeof(GeorgetteMeGeorgetteYouCard),
            typeof(HeartsBarrierCard),
            typeof(DatenCard),
            typeof(KillKiSSCard),
            typeof(SymbolIVEarthCard),
            typeof(QuaerereLuminaCard),
            typeof(KingsCard),
            typeof(AccompliceCard),
            typeof(DesuWaCard),
            typeof(EdgeOfBreakdownCard),
            typeof(MasqueradeRhapsodyRequestCard),
            typeof(ClockOutCard),
            typeof(FallenFlowersCard),
            typeof(HachibouseiDanceCard),
            typeof(PhantomOfMutsumiCard),
            typeof(PhantomOfSoyoCard),
            typeof(RhinocerosBeetleCard),
            typeof(CountingStarsCard),
            typeof(AleaIactaEstCard),
            typeof(AnglesCard),
            typeof(ChoirSChoirCard),
            typeof(CrucifixXCard),
            typeof(CuriosityCard),
            typeof(EnduranceCard),
            typeof(FearlessCard),
            typeof(KaoCard),
            typeof(OurSongCard),
            typeof(PerdereOmniaCard),
            typeof(PrimoDieInScaenaCard),
            typeof(SeizeTheFateCard),
            typeof(SharedDestinyCard),
            typeof(SymbolIFireCard),
            typeof(SymbolIIIWaterCard),
            typeof(TheGirlWithFlaxenHairCard),
            typeof(UtopiaCard),
            typeof(VeritasCard),
            typeof(WishFulfilledCard),
            typeof(WishToBecomeHumanCard),
            typeof(AsYourHeartDesiresCard),
            typeof(AveMujicaCard),
            typeof(BandInvitationCard),
            typeof(BlackBirthdayCard),
            typeof(CharismaticFormCard),
            typeof(CrueltyCard),
            typeof(CrychicCard),
            typeof(EtherCard),
            typeof(ImprisonedXIICard),
            typeof(MasksCard),
            typeof(PerfectionCard),
            typeof(PrideCard),
            typeof(SoraNoMusicaCard),
            typeof(SpringSunlightCard),
            typeof(StayEleganceCard),
            typeof(WishYouGoodLuckCard),
            typeof(WorldviewCard)
        ];
        if (rewardCardTypes.Length != expectedRewardCardTypes.Length ||
            rewardCardTypes.Except(expectedRewardCardTypes).Any())
        {
            throw new InvalidOperationException(
                "The enabled reward pool contains an unfinished or missing native card.");
        }
    }

    private static void ValidateAssets()
    {
        string[] missing = RequiredAssets.Where(path => !ResourceLoader.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException("Native vertical-slice assets are missing: " + string.Join(", ", missing));
        }
    }

    private static void ValidateLocalization()
    {
        SakikoCharacter character = ModelDb.Character<SakikoCharacter>();
        CardModel[] cards =
        [
            ModelDb.Card<StrikeTogawaSakiko>(),
            ModelDb.Card<DefendTogawaSakiko>(),
            ModelDb.Card<TheMoonlightSonataCard>(),
            ModelDb.Card<ASplitMomentCard>(),
            ModelDb.Card<DesireCard>(),
            ModelDb.Card<TwoMoonsCard>(),
            ModelDb.Card<SilentFarewellCard>(),
            ModelDb.Card<GreetingsCard>(),
            ModelDb.Card<TirednessCard>(),
            ModelDb.Card<MelodyCard>(),
            ModelDb.Card<IdealCard>(),
            ModelDb.Card<ProtectionCard>(),
            ModelDb.Card<RadianceCard>(),
            ModelDb.Card<KindnessCard>(),
            ModelDb.Card<AmorisCard>(),
            ModelDb.Card<DolorisCard>(),
            ModelDb.Card<MortisCard>(),
            ModelDb.Card<OblivionisCard>(),
            ModelDb.Card<TimorisCard>(),
            ModelDb.Card<BlackKeysCard>(),
            ModelDb.Card<WhiteKeysCard>(),
            ModelDb.Card<BlackAndWhiteKeysCard>(),
            ModelDb.Card<VoiceCard>(),
            ModelDb.Card<InnerCryCard>(),
            ModelDb.Card<MementoMoriCard>(),
            ModelDb.Card<BudgetBentoCard>(),
            ModelDb.Card<PhantomOfSakikoCard>(),
            ModelDb.Card<PhantomOfTakiCard>(),
            ModelDb.Card<PhantomOfTomoriCard>(),
            ModelDb.Card<SymbolIIAirCard>(),
            ModelDb.Card<DarkHeavenCard>(),
            ModelDb.Card<GeorgetteMeGeorgetteYouCard>(),
            ModelDb.Card<HeartsBarrierCard>(),
            ModelDb.Card<DatenCard>(),
            ModelDb.Card<KillKiSSCard>(),
            ModelDb.Card<SymbolIVEarthCard>(),
            ModelDb.Card<QuaerereLuminaCard>(),
            ModelDb.Card<KingsCard>(),
            ModelDb.Card<AccompliceCard>(),
            ModelDb.Card<CarefreeCard>(),
            ModelDb.Card<DesuWaCard>(),
            ModelDb.Card<EdgeOfBreakdownCard>(),
            ModelDb.Card<MasqueradeRhapsodyRequestCard>(),
            ModelDb.Card<WeaknessCard>(),
            ModelDb.Card<ClockOutCard>(),
            ModelDb.Card<FallenFlowersCard>(),
            ModelDb.Card<HachibouseiDanceCard>(),
            ModelDb.Card<PhantomOfMutsumiCard>(),
            ModelDb.Card<PhantomOfSoyoCard>(),
            ModelDb.Card<RhinocerosBeetleCard>(),
            ModelDb.Card<CountingStarsCard>(),
            ModelDb.Card<AleaIactaEstCard>(),
            ModelDb.Card<AnglesCard>(),
            ModelDb.Card<ChoirSChoirCard>(),
            ModelDb.Card<CrucifixXCard>(),
            ModelDb.Card<CuriosityCard>(),
            ModelDb.Card<EnduranceCard>(),
            ModelDb.Card<FearlessCard>(),
            ModelDb.Card<KaoCard>(),
            ModelDb.Card<OurSongCard>(),
            ModelDb.Card<PerdereOmniaCard>(),
            ModelDb.Card<PrimoDieInScaenaCard>(),
            ModelDb.Card<SeizeTheFateCard>(),
            ModelDb.Card<SharedDestinyCard>(),
            ModelDb.Card<SymbolIFireCard>(),
            ModelDb.Card<SymbolIIIWaterCard>(),
            ModelDb.Card<TheGirlWithFlaxenHairCard>(),
            ModelDb.Card<UtopiaCard>(),
            ModelDb.Card<VeritasCard>(),
            ModelDb.Card<WishFulfilledCard>(),
            ModelDb.Card<WishToBecomeHumanCard>(),
            ModelDb.Card<AsYourHeartDesiresCard>(),
            ModelDb.Card<AveMujicaCard>(),
            ModelDb.Card<BandInvitationCard>(),
            ModelDb.Card<BlackBirthdayCard>(),
            ModelDb.Card<CharismaticFormCard>(),
            ModelDb.Card<CrueltyCard>(),
            ModelDb.Card<CrychicCard>(),
            ModelDb.Card<EtherCard>(),
            ModelDb.Card<ImprisonedXIICard>(),
            ModelDb.Card<MasksCard>(),
            ModelDb.Card<PerfectionCard>(),
            ModelDb.Card<PrideCard>(),
            ModelDb.Card<SoraNoMusicaCard>(),
            ModelDb.Card<SpringSunlightCard>(),
            ModelDb.Card<StayEleganceCard>(),
            ModelDb.Card<WishYouGoodLuckCard>(),
            ModelDb.Card<WorldviewCard>()
        ];

        _ = character.Title.GetFormattedText();
        _ = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
        foreach (CardModel card in cards)
        {
            _ = card.Title;
            _ = card.GetDescriptionForPile(PileType.None);
        }

        DazzlingPower power = ModelDb.Power<DazzlingPower>();
        _ = power.Title.GetFormattedText();
        _ = power.GetDumbHoverTip();

        HypePower hype = ModelDb.Power<HypePower>();
        _ = hype.Title.GetFormattedText();
        _ = hype.GetDumbHoverTip();

        PowerModel[] cursePowers =
        [
            ModelDb.Power<DolorisPower>(),
            ModelDb.Power<MortisPower>(),
            ModelDb.Power<OblivionisPower>(),
            ModelDb.Power<TimorisPower>(),
            ModelDb.Power<KingsPower>()
        ];
        foreach (PowerModel cursePower in cursePowers)
        {
            _ = cursePower.Title.GetFormattedText();
            _ = cursePower.GetDumbHoverTip();
        }

        PowerModel[] stancePowers =
        [
            ModelDb.Power<MelodiaPower>(),
            ModelDb.Power<MonsterDivinityPower>()
        ];
        foreach (PowerModel stancePower in stancePowers)
        {
            _ = stancePower.Title.GetFormattedText();
            _ = stancePower.GetDumbHoverTip();
        }

        PowerModel[] uncommonPowers =
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
        foreach (PowerModel uncommonPower in uncommonPowers)
        {
            _ = uncommonPower.Title.GetFormattedText();
            _ = uncommonPower.GetDumbHoverTip();
        }

        PowerModel[] rarePowers =
        [
            ModelDb.Power<CharismaticFormPower>(),
            ModelDb.Power<TogawaSakiko.NativeCode.Models.Powers.CrueltyPower>(),
            ModelDb.Power<CrychicPower>(),
            ModelDb.Power<PridePower>(),
            ModelDb.Power<WishYouGoodLuckPower>(),
            ModelDb.Power<WorldviewPower>()
        ];
        foreach (PowerModel rarePower in rarePowers)
        {
            _ = rarePower.Title.GetFormattedText();
            _ = rarePower.GetDumbHoverTip();
        }

        StarterRelicTogawaSakiko relic = ModelDb.Relic<StarterRelicTogawaSakiko>();
        _ = relic.Title.GetFormattedText();
        _ = relic.DynamicDescription.GetFormattedText();

        PowerModel[] phaseN6Powers =
        [
            ModelDb.Power<DazzlingDownPower>(),
            ModelDb.Power<FreshlySqueezedCucumberPower>()
        ];
        foreach (PowerModel phaseN6Power in phaseN6Powers)
        {
            _ = phaseN6Power.Title.GetFormattedText();
            _ = phaseN6Power.GetDumbHoverTip();
        }

        RelicModel[] phaseN6Relics =
        [
            ModelDb.Relic<BlazingHairband>(),
            ModelDb.Relic<ColorfulNotebook>(),
            ModelDb.Relic<CuteAnimalBandAid>(),
            ModelDb.Relic<FountainDrink>(),
            ModelDb.Relic<GoldenPocketWatch>(),
            ModelDb.Relic<MasqueradeMask>(),
            ModelDb.Relic<TheCompass>(),
            ModelDb.Relic<TheDoll>(),
            ModelDb.Relic<TheThirdMovement>(),
            ModelDb.Relic<WarmthInfusedPorcelainCup>()
        ];
        foreach (RelicModel phaseN6Relic in phaseN6Relics)
        {
            _ = phaseN6Relic.Title.GetFormattedText();
            _ = phaseN6Relic.DynamicDescription.GetFormattedText();
        }

        PotionModel[] phaseN6Potions =
        [
            ModelDb.Potion<ChocolateMilkJelly>(),
            ModelDb.Potion<EarlGreyTea>(),
            ModelDb.Potion<FreshlySqueezedCucumber>(),
            ModelDb.Potion<HallucinationPotion>(),
            ModelDb.Potion<MatchaParfait>(),
            ModelDb.Potion<OrangeMilkJelly>()
        ];
        foreach (PotionModel phaseN6Potion in phaseN6Potions)
        {
            _ = phaseN6Potion.Title.GetFormattedText();
            _ = phaseN6Potion.DynamicDescription.GetFormattedText();
        }
    }
}
