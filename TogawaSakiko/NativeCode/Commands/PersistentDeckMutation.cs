using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TogawaSakiko.NativeCode.Models.Cards;

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

public sealed record PersistentDeckAndCombatAddResult(
    CardPileAddResult PersistentResult,
    CardPileAddResult? CombatResult)
{
    public bool Success => PersistentResult.success && CombatResult is { success: true };

    public CardModel? PersistentCard => PersistentResult.success ? PersistentResult.cardAdded : null;

    public CardModel? CombatCard => CombatResult is { } result && result.success ? result.cardAdded : null;
}

/// <summary>
/// Deterministic commands executed on every peer by their enclosing native card, hook, reward, or room flow.
/// Callers already inside those flows must not submit another action, which would duplicate mutations.
/// A peer-local combat UI request uses PersistentDeckRemovalGameAction.Request before entering these commands.
/// Native CardPileCmd and CardCmd own local preview filtering; state mutations must never be local-only.
/// </summary>
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
        bool skipVisuals = false,
        int upgradeLevel = 0)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(canonicalCard);
        canonicalCard.AssertCanonical();

        CardModel runCard = owner.RunState.CreateCard(canonicalCard, owner);
        UpgradeToLevel(runCard, upgradeLevel);
        return (await AddToDeckAndHandAsync(runCard, position, skipVisuals)).PersistentResult;
    }

    public static Task<CardPileAddResult> AddCanonicalAsync<TCard>(
        Player owner,
        CardPilePosition position = CardPilePosition.Bottom,
        bool skipVisuals = false,
        int upgradeLevel = 0)
        where TCard : CardModel
    {
        return AddCanonicalAsync(owner, ModelDb.Card<TCard>(), position, skipVisuals, upgradeLevel);
    }

    public static Task<PersistentDeckAndCombatAddResult> AddCanonicalWithCombatCopyAsync<TCard>(
        Player owner,
        CardPilePosition persistentPosition = CardPilePosition.Bottom,
        bool skipPersistentVisuals = false,
        int upgradeLevel = 0)
        where TCard : CardModel
    {
        ArgumentNullException.ThrowIfNull(owner);
        CardModel runCard = owner.RunState.CreateCard(ModelDb.Card<TCard>(), owner);
        UpgradeToLevel(runCard, upgradeLevel);
        return AddToDeckAndHandAsync(runCard, persistentPosition, skipPersistentVisuals);
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
        return (await AddToDeckAndHandAsync(runCard, position, skipVisuals)).PersistentResult;
    }

    public static async Task<CardPileAddResult> AddStatEquivalentAsync(
        Player owner,
        CardModel sourceCard,
        CardPilePosition position = CardPilePosition.Bottom,
        bool skipVisuals = false)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(sourceCard);
        sourceCard.AssertMutable();

        if (sourceCard.Owner != owner || sourceCard.RunState != owner.RunState)
        {
            throw new InvalidOperationException(
                $"Cannot copy card {sourceCard.Id} into a different player's persistent deck.");
        }

        CardModel runCard = owner.RunState.LoadCard(sourceCard.ToSerializable(), owner);
        return (await AddToDeckAndHandAsync(runCard, position, skipVisuals)).PersistentResult;
    }

    // During an active combat, every permanent addition also gets one linked copy in Hand.
    // Outside combat, or while combat is ending, only the permanent deck changes.
    private static async Task<PersistentDeckAndCombatAddResult> AddToDeckAndHandAsync(
        CardModel runCard,
        CardPilePosition position,
        bool skipVisuals)
    {
        Player owner = runCard.Owner;
        CardPileAddResult persistent = await CardPileCmd.Add(
            runCard,
            PileType.Deck,
            position,
            skipVisuals: skipVisuals);
        if (!persistent.success)
        {
            return new PersistentDeckAndCombatAddResult(persistent, null);
        }

        CardPileAddResult? combat = null;
        if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
            owner.PlayerCombatState is not null && owner.Creature.CombatState is { } combatState)
        {
            CardModel combatCard = combatState.CloneCard(persistent.cardAdded);
            combatCard.DeckVersion = persistent.cardAdded;
            combat = await CardPileCmd.AddGeneratedCardToCombat(combatCard, PileType.Hand, owner);
        }

        SpringSunlightCard.RefreshCombatCosts(owner);
        return new PersistentDeckAndCombatAddResult(persistent, combat);
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
            SpringSunlightCard.RefreshCombatCosts(persistentCard.Owner);

            PersistentDeckRemovalResult result = new(
                PersistentDeckRemovalStatus.Removed,
                persistentCard,
                combatCopies);
            if (persistentCard is MementoMoriCard memento)
            {
                await memento.AfterPermanentRemovalAsync();
            }

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

    private static void UpgradeToLevel(CardModel card, int upgradeLevel)
    {
        if (upgradeLevel < 0 || upgradeLevel > card.MaxUpgradeLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(upgradeLevel),
                upgradeLevel,
                $"Card {card.Id} supports upgrade levels 0 through {card.MaxUpgradeLevel}.");
        }

        while (card.CurrentUpgradeLevel < upgradeLevel)
        {
            int previousUpgradeLevel = card.CurrentUpgradeLevel;
            CardCmd.Upgrade(card, CardPreviewStyle.None);
            if (card.CurrentUpgradeLevel <= previousUpgradeLevel)
            {
                throw new InvalidOperationException(
                    $"Card {card.Id} failed to advance from upgrade level {previousUpgradeLevel} " +
                    $"toward requested level {upgradeLevel}; stopping to prevent an infinite upgrade loop.");
            }
        }
    }
}
