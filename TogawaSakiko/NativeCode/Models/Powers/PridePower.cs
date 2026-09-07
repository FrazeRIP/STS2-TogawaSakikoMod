using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class PridePower : PowerModel
{
    private sealed class Data
    {
        public readonly List<CardModel> Copies = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal void Register(PrideCard source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.AssertMutable();
        GetInternalData<Data>().Copies.Add((CardModel)source.ClonePreservingMutability());
    }

    internal int RegisteredCopyCount => GetInternalData<Data>().Copies.Count;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player is null)
        {
            return;
        }

        Flash();
        foreach (CardModel snapshot in GetInternalData<Data>().Copies.AsEnumerable().Reverse())
        {
            CardModel copy = CombatState.CloneCard(snapshot);
            await CardPileCmd.AddGeneratedCardToCombat(
                copy,
                PileType.Draw,
                Owner.Player,
                CardPilePosition.Top);
        }

        await PowerCmd.Remove(this);
    }
}
