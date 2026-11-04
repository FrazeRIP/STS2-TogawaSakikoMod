using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.NativeCode.Models.Powers;

/// <summary>Combat-long attack damage bonus and vulnerability, evaluated once per hit.</summary>
public sealed class ArdorPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (Amount <= 0 || !props.IsPoweredAttack())
        {
            return 0m;
        }

        decimal bonus = 0m;
        if (dealer == Owner)
        {
            bonus += Amount;
        }

        if (target == Owner)
        {
            bonus += Amount;
        }

        return bonus;
    }
}
