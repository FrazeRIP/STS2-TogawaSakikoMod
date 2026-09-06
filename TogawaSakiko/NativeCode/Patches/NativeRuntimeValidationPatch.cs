using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
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
        NativeAssetPaths.CharacterPortrait,
        NativeAssetPaths.CharacterIcon,
        NativeAssetPaths.CharacterMapMarker,
        NativeAssetPaths.StrikePortrait,
        NativeAssetPaths.DefendPortrait,
        NativeAssetPaths.MoonlightSonataPortrait,
        NativeAssetPaths.SplitMomentPortrait,
        NativeAssetPaths.DesirePortrait,
        NativeAssetPaths.TwoMoonsPortrait,
        NativeAssetPaths.SilentFarewellPortrait,
        NativeAssetPaths.MonochromeHairbandIcon,
        NativeAssetPaths.MonochromeHairbandOutline,
        NativeAssetPaths.MonochromeHairbandBigIcon,
        NativeAssetPaths.DazzlingIcon,
        NativeAssetPaths.DazzlingBigIcon
    ];

    private static void Postfix()
    {
        ValidateStableIds();
        ValidateCharacterRegistration();
        ValidateAssets();
        ValidateLocalization();
        NativeSmokeTrace.MarkModelDbInitialized();

        Logger.Info(
            $"Native vertical slice ready. Language={LocManager.Instance.Language}, Models={NativeModelCatalog.GameplayModelCount}, ExternalModDependencies=0");
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
            typeof(SilentFarewellCard)
        ];
        if (rewardCardTypes.Length != expectedRewardCardTypes.Length ||
            rewardCardTypes.Except(expectedRewardCardTypes).Any())
        {
            throw new InvalidOperationException(
                "The Phase N2 reward pool must contain exactly A Split Moment, Two Moons, and Silent Farewell.");
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
            ModelDb.Card<SilentFarewellCard>()
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

        StarterRelicTogawaSakiko relic = ModelDb.Relic<StarterRelicTogawaSakiko>();
        _ = relic.Title.GetFormattedText();
        _ = relic.DynamicDescription.GetFormattedText();
    }
}
