using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class StayElegancePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0 || power.Owner != Owner || power.TypeForCurrentAmount != PowerType.Buff)
        {
            return;
        }

        int block = ReferenceEquals(power, this) ? Amount - (int)amount : Amount;
        if (block <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}
