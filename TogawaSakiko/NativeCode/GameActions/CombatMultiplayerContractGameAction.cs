using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.GameActions;

/// <summary>Only the explicitly enabled installed-game multiplayer harness submits this action.</summary>
public sealed class CombatMultiplayerContractGameAction(Player owner) : GameAction
{
    public override ulong OwnerId => owner.NetId;

    public override GameActionType ActionType => GameActionType.Combat;

    protected override Task ExecuteAction()
    {
        if (!MultiplayerContractDiagnostics.Enabled)
        {
            throw new InvalidOperationException("Multiplayer contract actions require the opt-in test harness.");
        }

        return CombatMultiplayerContractDiagnostics.RunAsync(new GameActionPlayerChoiceContext(this), owner.Deck.Cards[0]);
    }

    public override INetAction ToNetAction() => new NetCombatMultiplayerContractAction();
}

public struct NetCombatMultiplayerContractAction : INetAction, IPacketSerializable
{
    public readonly GameAction ToGameAction(Player player) => new CombatMultiplayerContractGameAction(player);

    public readonly void Serialize(PacketWriter writer)
    {
    }

    public void Deserialize(PacketReader reader)
    {
    }
}
