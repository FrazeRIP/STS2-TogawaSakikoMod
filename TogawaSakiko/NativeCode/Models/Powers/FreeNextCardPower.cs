using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class FreeNextCardPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private bool Applies(CardModel card) =>
        Amount > 0 && card.Owner.Creature == Owner && card.Pile?.Type is PileType.Hand or PileType.Play;

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal currentCost, out decimal newCost)
    {
        newCost = currentCost;
        if (!Applies(card))
        {
            return false;
        }

        newCost = 0;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Applies(cardPlay.Card))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
