using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class DazzlingPower : PowerModel
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
        if (amount <= 0m || power.Owner != Owner || power.Type != PowerType.Buff)
        {
            return;
        }

        List<Creature> opponents = CombatState.GetOpponentsOf(Owner)
            .Where(creature => creature.IsHittable)
            .ToList();
        if (opponents.Count == 0)
        {
            return;
        }

        Creature? target = CombatState.RunState.Rng.CombatTargets.NextItem(opponents);
        if (target is null)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner);
        NativeSmokeTrace.Info($"Dazzling dealt {Amount} damage after {power.Id} increased.");
    }
}
