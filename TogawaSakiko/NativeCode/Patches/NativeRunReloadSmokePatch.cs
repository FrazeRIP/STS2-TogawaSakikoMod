using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadRunSave))]
internal static class NativeRunReloadSmokePatch
{
    private static int _validationStarted;

    private static void Postfix(ReadSaveResult<SerializableRun> __result)
    {
        if ((!NativeSmokeTrace.ReloadEnabled &&
             !NativeSmokeTrace.ContractReloadEnabled &&
             !NativeSmokeTrace.KingsReloadEnabled &&
             !NativeSmokeTrace.N6ReloadEnabled &&
             !NativeSmokeTrace.N7ReloadEnabled) ||
            Interlocked.CompareExchange(ref _validationStarted, 1, 0) != 0)
        {
            return;
        }

        if (!__result.Success || __result.SaveData is null)
        {
            throw new InvalidOperationException(
                $"The native reload smoke could not read the run save. Status={__result.Status}, Error={__result.ErrorMessage}");
        }

        RunState runState = RunState.FromSerializable(__result.SaveData);
        var player = runState.Players.SingleOrDefault(candidate => candidate.Character is SakikoCharacter)
            ?? throw new InvalidOperationException("The reloaded run does not contain Togawa Sakiko.");

        if (NativeSmokeTrace.N7ReloadEnabled)
        {
            if (player.Deck.Cards.Count == 0 || player.Deck.Cards.Any(card => card.Id == ModelId.none))
            {
                throw new InvalidOperationException("The Phase N7 reloaded Sakiko deck is empty or contains an unresolved model ID.");
            }

            int disabledCompatibilityCards = player.Deck.Cards.Count(card => card is CarefreeCard or WeaknessCard);
            if (disabledCompatibilityCards != 0)
            {
                throw new InvalidOperationException(
                    $"The Phase N7 natural run generated {disabledCompatibilityCards} disabled compatibility card(s).");
            }

            NativeSmokeTrace.N7Info(
                $"reload deserialized Togawa Sakiko. Act={runState.CurrentActIndex + 1}, " +
                $"Visited={runState.VisitedMapCoords.Count}, Deck={player.Deck.Cards.Count}, " +
                $"Relics={player.Relics.Count}, Potions={player.PotionSlots.Count}.");
            return;
        }

        if (NativeSmokeTrace.KingsReloadEnabled)
        {
            TaskHelper.RunSafely(KingsLifecycleDiagnostics.VerifyReloadedRewardAsync(
                player,
                __result.SaveData.PreFinishedRoom));
            return;
        }

        if (!player.Relics.Any(relic => relic is StarterRelicTogawaSakiko))
        {
            throw new InvalidOperationException("The reloaded run does not contain Monochrome Hairband.");
        }

        int desireCount = player.Deck.Cards.Count(card => card is DesireCard);
        if (desireCount == 0)
        {
            throw new InvalidOperationException("The reloaded run does not contain the generated Desire card.");
        }

        NativeSmokeTrace.ReloadInfo(
            $"deserialized Togawa Sakiko with Monochrome Hairband and {desireCount} Desire card(s).");

        if (NativeSmokeTrace.ContractReloadEnabled)
        {
            int silentFarewellCount = player.Deck.Cards.Count(card => card is SilentFarewellCard);
            int twoMoonsCount = player.Deck.Cards.Count(card => card is TwoMoonsCard);
            if (desireCount != 1 || silentFarewellCount != 1 || twoMoonsCount != 0)
            {
                throw new InvalidOperationException(
                    "The Phase N3 reloaded deck did not preserve exact add/removal results: " +
                    $"Desire={desireCount}, SilentFarewell={silentFarewellCount}, TwoMoons={twoMoonsCount}.");
            }

            NativeSmokeTrace.ContractInfo(
                "reload preserved exactly one Hairband Desire, one added Silent Farewell, and no removed Two Moons.");
        }

        if (NativeSmokeTrace.N6ReloadEnabled)
        {
            GoldenPocketWatch watch = player.Relics.OfType<GoldenPocketWatch>().SingleOrDefault()
                ?? throw new InvalidOperationException("The N6 reload is missing Golden Pocket Watch.");
            MasqueradeMask mask = player.Relics.OfType<MasqueradeMask>().SingleOrDefault()
                ?? throw new InvalidOperationException("The N6 reload is missing Masquerade Mask.");
            TheDoll doll = player.Relics.OfType<TheDoll>().SingleOrDefault()
                ?? throw new InvalidOperationException("The N6 reload is missing The Doll.");
            TheThirdMovement movement = player.Relics.OfType<TheThirdMovement>().SingleOrDefault()
                ?? throw new InvalidOperationException("The N6 reload is missing The Third Movement.");

            if (watch.CardsPlayed != 7 || mask.PurgedCards != 2 ||
                doll.TurnsElapsed != 1 || movement.RemainingUses != 1)
            {
                throw new InvalidOperationException(
                    "The N6 reloaded counters were not exact: " +
                    $"Watch={watch.CardsPlayed}, Mask={mask.PurgedCards}, " +
                    $"Doll={doll.TurnsElapsed}, ThirdMovement={movement.RemainingUses}.");
            }

            int earlGreyCount = player.PotionSlots.OfType<TogawaSakiko.NativeCode.Models.Potions.EarlGreyTea>().Count();
            int purgeRewardCount = __result.SaveData.PreFinishedRoom?.ExtraRewards.Values
                .SelectMany(rewards => rewards)
                .Count(reward => reward.RewardType == MegaCrit.Sts2.Core.Rewards.RewardType.RemoveCard) ?? 0;
            if (earlGreyCount != 1 || purgeRewardCount < 5)
            {
                throw new InvalidOperationException(
                    $"The N6 reload did not preserve its potion/rewards: EarlGrey={earlGreyCount}, PurgeRewards={purgeRewardCount}.");
            }

            NativeSmokeTrace.N6Info(
                $"reload restored Watch=7, Mask=2, Doll=1, ThirdMovement=1, EarlGrey=1, PurgeRewards={purgeRewardCount}.");
        }
    }
}
