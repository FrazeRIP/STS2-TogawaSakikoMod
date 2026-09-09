using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>Records diagnostic fixture setup so native replays include every gameplay mutation.</summary>
public sealed class MultiplayerFixtureGameAction(Player owner, ModelId cardId, string phase, bool finishCombat) : GameAction
{
    public override ulong OwnerId => owner.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    protected override async Task ExecuteAction()
    {
        if (!MultiplayerContractDiagnostics.Enabled)
        {
            throw new InvalidOperationException("Multiplayer fixture action requires the opt-in diagnostic harness.");
        }
        var combat = owner.Creature.CombatState
            ?? throw new InvalidOperationException("Multiplayer fixture requires active combat.");
        if (finishCombat)
        {
            foreach (var enemy in combat.Enemies)
            {
                enemy.SetCurrentHpInternal(1);
            }
        }
        CardModel card = combat.CreateCard(ModelDb.GetById<CardModel>(cardId), owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        await PlayerCmd.GainEnergy(10, owner);
        MultiplayerContractDiagnostics.FixtureCards.Add(phase, card);
    }

    public override INetAction ToNetAction() => new NetMultiplayerFixtureAction
    {
        CardId = cardId, Phase = phase, FinishCombat = finishCombat
    };
}

public struct NetMultiplayerFixtureAction : INetAction, IPacketSerializable
{
    public ModelId CardId;
    public string Phase;
    public bool FinishCombat;

    public readonly GameAction ToGameAction(Player player) => new MultiplayerFixtureGameAction(player, CardId, Phase, FinishCombat);
    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteModelEntry(CardId);
        writer.WriteString(Phase);
        writer.WriteBool(FinishCombat);
    }

    public void Deserialize(PacketReader reader)
    {
        CardId = reader.ReadModelIdAssumingType<CardModel>();
        Phase = reader.ReadString();
        FinishCombat = reader.ReadBool();
    }
}
