using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class FireEchoPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner.Creature == Owner && card.Type == CardType.Attack ? playCount + Amount : playCount;

    // No end-of-turn cleanup: all charges persist until the next Attack.
    public override Task AfterModifyingCardPlayCount(CardModel card) => PowerCmd.Remove(this);
}
