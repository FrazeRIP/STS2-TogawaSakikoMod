using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Commands;

namespace TogawaSakiko.NativeCode.GameActions;

public sealed class PersistentDeckRemovalGameAction : GameAction
{
    private readonly Player _owner;
    private readonly NetDeckCard _persistentCard;
    private readonly bool _skipCombatVisuals;

    public override ulong OwnerId => _owner.NetId;

    public override GameActionType ActionType => GameActionType.Combat;

    public PersistentDeckRemovalResult? Result { get; private set; }

    public PersistentDeckRemovalGameAction(CardModel persistentCard, bool skipCombatVisuals = false)
        : this(persistentCard.Owner, NetDeckCard.FromModel(persistentCard), skipCombatVisuals)
    {
    }

    public PersistentDeckRemovalGameAction(Player owner, NetDeckCard persistentCard, bool skipCombatVisuals)
    {
        _owner = owner;
        _persistentCard = persistentCard;
        _skipCombatVisuals = skipCombatVisuals;
    }

    public static PersistentDeckRemovalGameAction Request(
        CardModel persistentCard,
        bool skipCombatVisuals = false)
    {
        if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress)
        {
            throw new InvalidOperationException(
                "Persistent deck removal only needs a synchronized game action during active combat.");
        }

        PersistentDeckRemovalGameAction action = new(persistentCard, skipCombatVisuals);
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
        return action;
    }

    protected override async Task ExecuteAction()
    {
        CardModel persistentCard = _persistentCard.ToCardModel(_owner);
        Result = await PersistentDeckMutation.RemoveAsync(
            persistentCard,
            skipCombatVisuals: _skipCombatVisuals);
    }

    public override INetAction ToNetAction()
    {
        return new NetPersistentDeckRemovalAction
        {
            PersistentCard = _persistentCard,
            SkipCombatVisuals = _skipCombatVisuals
        };
    }

    public override string ToString()
    {
        return $"PersistentDeckRemovalGameAction owner={OwnerId} card={_persistentCard}";
    }
}

public struct NetPersistentDeckRemovalAction : INetAction, IPacketSerializable
{
    public NetDeckCard PersistentCard;
    public bool SkipCombatVisuals;

    public readonly GameAction ToGameAction(Player player)
    {
        return new PersistentDeckRemovalGameAction(player, PersistentCard, SkipCombatVisuals);
    }

    public readonly void Serialize(PacketWriter writer)
    {
        writer.Write(PersistentCard);
        writer.WriteBool(SkipCombatVisuals);
    }

    public void Deserialize(PacketReader reader)
    {
        PersistentCard = reader.Read<NetDeckCard>();
        SkipCombatVisuals = reader.ReadBool();
    }

    public override readonly string ToString()
    {
        return $"NetPersistentDeckRemovalAction card={PersistentCard}";
    }
}
