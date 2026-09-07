using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Commands;

public enum PersistentDeckRemovalStatus
{
    Removed,
    PreventedNotRemovable,
    NotInPersistentDeck,
    AlreadyRemoved,
    Failed
}

public sealed record PersistentDeckRemovalResult(
    PersistentDeckRemovalStatus Status,
    CardModel PersistentCard,
    IReadOnlyList<CardModel> RemovedCombatCopies,
    string? FailureReason = null)
{
    public bool Success => Status == PersistentDeckRemovalStatus.Removed;

    public bool Prevented => Status == PersistentDeckRemovalStatus.PreventedNotRemovable;
}

public static class PersistentDeckMutation
{
    private static readonly PileType[] LinkedCombatPileTypes =
    [
        PileType.Hand,
        PileType.Draw,
        PileType.Discard,
        PileType.Exhaust,
        PileType.Play
    ];

    public static event Action<PersistentDeckRemovalResult>? PersistentCardRemoved;

    internal static bool IsExactLinkedCopy(CardModel candidate, CardModel persistentCard)
    {
        return ReferenceEquals(candidate.DeckVersion, persistentCard);
    }

    public static async Task<CardPileAddResult> AddCanonicalAsync(
        Player owner,
        CardModel canonicalCard,
        CardPilePosition position = CardPilePosition.Bottom,
        bool skipVisuals = false)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(canonicalCard);
        canonicalCard.AssertCanonical();

        CardModel runCard = owner.RunState.CreateCard(canonicalCard, owner);
        return await CardPileCmd.Add(runCard, PileType.Deck, position, skipVisuals: skipVisuals);
    }

    public static Task<CardPileAddResult> AddCanonicalAsync<TCard>(
        Player owner,
        CardPilePosition position = CardPilePosition.Bottom,
        bool skipVisuals = false)
        where TCard : CardModel
    {
        return AddCanonicalAsync(owner, ModelDb.Card<TCard>(), position, skipVisuals);
    }

    public static async Task<CardPileAddResult> AddRunCardCloneAsync(
        Player owner,
        CardModel sourceRunCard,
        CardPilePosition position = CardPilePosition.Bottom,
        bool skipVisuals = false)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(sourceRunCard);
        sourceRunCard.AssertMutable();

        if (sourceRunCard.Owner != owner || sourceRunCard.RunState != owner.RunState)
        {
            throw new InvalidOperationException(
                $"Cannot clone persistent card {sourceRunCard.Id} into a different player's run state.");
        }

        CardModel runCard = owner.RunState.CloneCard(sourceRunCard);
        return await CardPileCmd.Add(runCard, PileType.Deck, position, skipVisuals: skipVisuals);
    }

    public static IReadOnlyList<CardModel> SnapshotLinkedCombatCopies(CardModel persistentCard)
    {
        ArgumentNullException.ThrowIfNull(persistentCard);
        persistentCard.AssertMutable();

        Player owner = persistentCard.Owner;
        if (owner?.PlayerCombatState is null)
        {
            return Array.Empty<CardModel>();
        }

        HashSet<PileType> allowedPiles = LinkedCombatPileTypes.ToHashSet();
        return owner.PlayerCombatState.AllPiles
            .Where(pile => allowedPiles.Contains(pile.Type))
            .SelectMany(pile => pile.Cards)
            .Where(card => IsExactLinkedCopy(card, persistentCard))
            .ToArray();
    }

    public static async Task<PersistentDeckRemovalResult> RemoveAsync(
        CardModel persistentCard,
        bool showPersistentPreview = true,
        bool skipCombatVisuals = false)
    {
        ArgumentNullException.ThrowIfNull(persistentCard);
        persistentCard.AssertMutable();

        if (persistentCard.HasBeenRemovedFromState)
        {
            return new PersistentDeckRemovalResult(
                PersistentDeckRemovalStatus.AlreadyRemoved,
                persistentCard,
                Array.Empty<CardModel>(),
                "The persistent card was already removed from its run state.");
        }

        if (persistentCard.Pile?.Type != PileType.Deck)
        {
            return new PersistentDeckRemovalResult(
                PersistentDeckRemovalStatus.NotInPersistentDeck,
                persistentCard,
                Array.Empty<CardModel>(),
                $"The requested card is in {persistentCard.Pile?.Type.ToString() ?? "no pile"}, not the persistent deck.");
        }

        if (!persistentCard.IsRemovable)
        {
            return new PersistentDeckRemovalResult(
                PersistentDeckRemovalStatus.PreventedNotRemovable,
                persistentCard,
                Array.Empty<CardModel>(),
                $"Card {persistentCard.Id} has a keyword that prevents removal.");
        }

        IReadOnlyList<CardModel> combatCopies = SnapshotLinkedCombatCopies(persistentCard);
        try
        {
            if (combatCopies.Count > 0)
            {
                await CardPileCmd.RemoveFromCombat(combatCopies, skipCombatVisuals);
            }

            await CardPileCmd.RemoveFromDeck(persistentCard, showPersistentPreview);

            PersistentDeckRemovalResult result = new(
                PersistentDeckRemovalStatus.Removed,
                persistentCard,
                combatCopies);
            PersistentCardRemoved?.Invoke(result);
            return result;
        }
        catch (Exception exception)
        {
            return new PersistentDeckRemovalResult(
                PersistentDeckRemovalStatus.Failed,
                persistentCard,
                combatCopies.Where(card => card.HasBeenRemovedFromState).ToArray(),
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }
}
