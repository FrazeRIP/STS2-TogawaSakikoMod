using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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
    string? FailureReason = null);

public static class SakikoPurgeCommand
{
    public static async Task<SakikoPurgeResult> RemoveAsync(
        CardModel selectedCard,
        bool showPersistentPreview = true,
        bool skipCombatVisuals = false)
    {
        ArgumentNullException.ThrowIfNull(selectedCard);
        selectedCard.AssertMutable();

        CardModel? persistentCard = selectedCard.Pile?.Type == PileType.Deck
            ? selectedCard
            : selectedCard.DeckVersion;
        if (persistentCard is not null && IsEligible(persistentCard))
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

        return new SakikoPurgeResult(
            SelectedCard: selectedCard,
            Success: false,
            Prevented: true,
            PersistentRemoval: null,
            MasqueradeCardsIncreased: 0,
            FailureReason: "Only removable cards still in the permanent deck can be purged.");
    }

    public static bool IsEligible(CardModel card)
    {
        return !card.HasBeenRemovedFromState && card.Pile?.Type == PileType.Deck &&
            card.IsRemovable && card.Owner.Deck.Cards.Contains(card);
    }

    public static CardModel[] GetCandidates(Player owner)
    {
        return owner.Deck.Cards.Where(IsEligible).ToArray();
    }

    private static int NotifyMasqueradeMask(Player owner)
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
