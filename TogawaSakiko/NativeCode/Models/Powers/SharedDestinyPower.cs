using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class SharedDestinyPower : PowerModel
{
    private sealed class Data
    {
        public readonly HashSet<PowerModel> NewlyAppliedBuffs = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforePowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature target,
        Creature? applier,
        CardModel? cardSource)
    {
        if (target == Owner &&
            amount > 0m &&
            power.Type == PowerType.Buff &&
            power is not AmbergrisPower &&
            !target.HasPower(power.Id))
        {
            GetInternalData<Data>().NewlyAppliedBuffs.Add(power);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount > 0m &&
            power.Owner == Owner &&
            power.TypeForCurrentAmount == PowerType.Buff &&
            GetInternalData<Data>().NewlyAppliedBuffs.Remove(power) &&
            Owner.Player is not null)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
        }
    }
}
