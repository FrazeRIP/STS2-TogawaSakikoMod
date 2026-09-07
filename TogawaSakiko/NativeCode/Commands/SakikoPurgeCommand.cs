using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Commands;

public sealed record SakikoPurgeResult(
    CardModel SelectedCard,
    bool Success,
    bool Prevented,
    PersistentDeckRemovalResult? PersistentRemoval,
    int MasqueradeCardsIncreased,
    int MasqueradeRewardsAdded = 0,
    string? FailureReason = null,
    bool ReplacedByPlay = false);

public static class SakikoPurgeCommand
{
    public static async Task<SakikoPurgeResult> RemoveAsync(
        CardModel selectedCard,
        bool showPersistentPreview = true,
        bool skipCombatVisuals = false,
        PlayerChoiceContext? choiceContext = null)
    {
        ArgumentNullException.ThrowIfNull(selectedCard);
        selectedCard.AssertMutable();

        if (selectedCard is UtopiaCard && selectedCard.Pile is { IsCombatPile: true })
        {
            if (choiceContext is null)
            {
                return new SakikoPurgeResult(
                    SelectedCard: selectedCard,
                    Success: false,
                    Prevented: false,
                    PersistentRemoval: null,
                    MasqueradeCardsIncreased: 0,
                    FailureReason: "Utopia requires the active player-choice context to replace a combat purge with play.");
            }

            if (CombatManager.Instance.IsOverOrEnding)
            {
                return new SakikoPurgeResult(
                    SelectedCard: selectedCard,
                    Success: false,
                    Prevented: false,
                    PersistentRemoval: null,
                    MasqueradeCardsIncreased: 0,
                    FailureReason: "Utopia cannot replace a purge while combat is ending.");
            }

            try
            {
                await CardCmd.AutoPlay(choiceContext, selectedCard, null);
                return new SakikoPurgeResult(
                    SelectedCard: selectedCard,
                    Success: true,
                    Prevented: false,
                    PersistentRemoval: null,
                    MasqueradeCardsIncreased: 0,
                    ReplacedByPlay: true);
            }
            catch (Exception exception)
            {
                return new SakikoPurgeResult(
                    SelectedCard: selectedCard,
                    Success: false,
                    Prevented: false,
                    PersistentRemoval: null,
                    MasqueradeCardsIncreased: 0,
                    FailureReason: $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        CardModel? persistentCard = selectedCard.Pile?.Type == PileType.Deck
            ? selectedCard
            : selectedCard.DeckVersion;
        if (persistentCard is not null)
        {
            PersistentDeckRemovalResult removal = await PersistentDeckMutation.RemoveAsync(
                persistentCard,
                showPersistentPreview,
                skipCombatVisuals);
            if (!removal.Success)
            {
                return new SakikoPurgeResult(
                    SelectedCard: selectedCard,
                    Success: false,
                    Prevented: removal.Prevented,
                    PersistentRemoval: removal,
                    MasqueradeCardsIncreased: 0,
                    FailureReason: removal.FailureReason);
            }

            int increased = MasqueradeRhapsodyRequestCard.ApplyPurgeGrowth(persistentCard.Owner);
            int rewardsAdded = NotifyMasqueradeMask(persistentCard.Owner);
            return new SakikoPurgeResult(
                SelectedCard: selectedCard,
                Success: true,
                Prevented: false,
                PersistentRemoval: removal,
                MasqueradeCardsIncreased: increased,
                MasqueradeRewardsAdded: rewardsAdded);
        }

        if (!selectedCard.IsRemovable)
        {
            return new SakikoPurgeResult(
                SelectedCard: selectedCard,
                Success: false,
                Prevented: true,
                PersistentRemoval: null,
                MasqueradeCardsIncreased: 0,
                FailureReason: $"Card {selectedCard.Id} has a keyword that prevents removal.");
        }

        if (selectedCard.Pile is not { IsCombatPile: true })
        {
            return new SakikoPurgeResult(
                SelectedCard: selectedCard,
                Success: false,
                Prevented: false,
                PersistentRemoval: null,
                MasqueradeCardsIncreased: 0,
                FailureReason: $"Card {selectedCard.Id} is not in a combat pile or the persistent deck.");
        }

        try
        {
            await CardPileCmd.RemoveFromCombat(selectedCard, skipCombatVisuals);
            int increased = MasqueradeRhapsodyRequestCard.ApplyPurgeGrowth(selectedCard.Owner);
            int rewardsAdded = NotifyMasqueradeMask(selectedCard.Owner);
            return new SakikoPurgeResult(
                SelectedCard: selectedCard,
                Success: true,
                Prevented: false,
                PersistentRemoval: null,
                MasqueradeCardsIncreased: increased,
                MasqueradeRewardsAdded: rewardsAdded);
        }
        catch (Exception exception)
        {
            return new SakikoPurgeResult(
                SelectedCard: selectedCard,
                Success: false,
                Prevented: false,
                PersistentRemoval: null,
                MasqueradeCardsIncreased: 0,
                FailureReason: $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static int NotifyMasqueradeMask(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        if (CombatManager.Instance.IsOverOrEnding ||
            owner.RunState.CurrentRoom is not CombatRoom room)
        {
            return 0;
        }

        int rewardsAdded = 0;
        foreach (MasqueradeMask mask in owner.Relics.OfType<MasqueradeMask>().Where(relic => !relic.IsMelted))
        {
            rewardsAdded += mask.OnCardPurged(room);
        }

        return rewardsAdded;
    }
}
