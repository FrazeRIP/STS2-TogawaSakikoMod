using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class EncorePower : ConsecutiveCardPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override async Task OnConsecutiveCardAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(Amount, cardPlay.Card.Owner);
    }
}
