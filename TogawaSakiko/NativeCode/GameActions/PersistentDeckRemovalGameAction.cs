using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Multiplayer;

namespace TogawaSakiko.NativeCode.GameActions;

public sealed class PersistentDeckRemovalGameAction : GameAction
{
    private readonly Player _owner;
    private readonly NetDeckCard _persistentCard;
    private readonly NetCombatCard? _linkedCombatCard;
    private readonly bool _skipCombatVisuals;

    public override ulong OwnerId => _owner.NetId;

    public override GameActionType ActionType => GameActionType.Combat;

    public PersistentDeckRemovalResult? Result { get; private set; }

    public PersistentDeckRemovalGameAction(CardModel persistentCard, bool skipCombatVisuals = false)
        : this(persistentCard.Owner, NetDeckCard.FromModel(persistentCard), skipCombatVisuals,
            FindLinkedCombatCard(persistentCard))
    {
    }

    public PersistentDeckRemovalGameAction(
        Player owner,
        NetDeckCard persistentCard,
        bool skipCombatVisuals,
        NetCombatCard? linkedCombatCard = null)
    {
        _owner = owner;
        _persistentCard = persistentCard;
        _skipCombatVisuals = skipCombatVisuals;
        _linkedCombatCard = linkedCombatCard;
    }

    public static PersistentDeckRemovalGameAction Request(
        CardModel persistentCard,
        bool skipCombatVisuals = false)
    {
        ArgumentNullException.ThrowIfNull(persistentCard);
        persistentCard.AssertMutable();
        if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress)
        {
            throw new InvalidOperationException(
                "Persistent deck removal only needs a synchronized game action during active combat.");
        }

        // The native action transport uses the sender as its owner; it cannot request another player's deck.
        if (!SakikoMultiplayerAuthority.CanSubmitPlayerAction(persistentCard.Owner))
        {
            throw new InvalidOperationException(
                "Only the owning local player can request persistent deck removal, and replay cannot submit new actions.");
        }

        PersistentDeckRemovalGameAction action = new(persistentCard, skipCombatVisuals);
        if (persistentCard.Owner.RunState.Players.Count > 1 && action._linkedCombatCard is null)
        {
            throw new InvalidOperationException(
                "A multiplayer removal request requires a registered linked combat copy so a queued deck-index shift cannot select another card.");
        }

        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
        return action;
    }

    protected override async Task ExecuteAction()
    {
        CardModel persistentCard = ResolvePersistentCard();
        Result = await PersistentDeckMutation.RemoveAsync(
            persistentCard,
            skipCombatVisuals: _skipCombatVisuals);
    }

    public override INetAction ToNetAction()
    {
        return new NetPersistentDeckRemovalAction
        {
            PersistentCard = _persistentCard,
            SkipCombatVisuals = _skipCombatVisuals,
            LinkedCombatCard = _linkedCombatCard
        };
    }

    public override string ToString()
    {
        return $"PersistentDeckRemovalGameAction owner={OwnerId} card={_persistentCard}";
    }

    internal CardModel ResolvePersistentCard()
    {
        if (_linkedCombatCard is { } linkedCard)
        {
            CardModel combatCard = linkedCard.ToCardModel();
            CardModel persistentCard = combatCard.DeckVersion ?? throw new InvalidOperationException(
                "The synchronized combat card no longer identifies a persistent deck card.");
            if (combatCard.Owner != _owner || persistentCard.Owner != _owner)
            {
                throw new InvalidOperationException("A synchronized removal cannot target another player's card.");
            }

            return persistentCard;
        }

        if (_owner.RunState.Players.Count > 1)
        {
            throw new InvalidOperationException("A multiplayer removal packet is missing its exact linked combat identity.");
        }

        return _persistentCard.ToCardModel(_owner);
    }

    private static NetCombatCard? FindLinkedCombatCard(CardModel persistentCard)
    {
        foreach (CardModel copy in PersistentDeckMutation.SnapshotLinkedCombatCopies(persistentCard))
        {
            if (NetCombatCardDb.Instance.TryGetCardId(copy, out _))
            {
                return NetCombatCard.FromModel(copy);
            }
        }

        return null;
    }
}

public struct NetPersistentDeckRemovalAction : INetAction, IPacketSerializable
{
    public NetDeckCard PersistentCard;
    public bool SkipCombatVisuals;
    public NetCombatCard? LinkedCombatCard;

    public readonly GameAction ToGameAction(Player player)
    {
        return new PersistentDeckRemovalGameAction(player, PersistentCard, SkipCombatVisuals, LinkedCombatCard);
    }

    public readonly void Serialize(PacketWriter writer)
    {
        writer.Write(PersistentCard);
        writer.WriteBool(SkipCombatVisuals);
        writer.WriteBool(LinkedCombatCard.HasValue);
        if (LinkedCombatCard is { } linkedCard)
        {
            writer.Write(linkedCard);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        PersistentCard = reader.Read<NetDeckCard>();
        SkipCombatVisuals = reader.ReadBool();
        LinkedCombatCard = reader.ReadBool() ? reader.Read<NetCombatCard>() : null;
    }

    public override readonly string ToString()
    {
        return $"NetPersistentDeckRemovalAction card={PersistentCard}";
    }
}
