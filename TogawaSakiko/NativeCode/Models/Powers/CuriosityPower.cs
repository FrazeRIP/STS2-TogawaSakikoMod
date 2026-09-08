using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class CuriosityPower : PowerModel
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForPowerCards = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Power)
        {
            GetInternalData<Data>().AmountsForPowerCards[cardPlay.Card] = Amount;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner &&
            GetInternalData<Data>().AmountsForPowerCards.Remove(cardPlay.Card, out int amount) &&
            amount > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                Owner,
                amount,
                Owner,
                cardPlay.Card);
        }
    }
}
