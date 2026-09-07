using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Presentation.Godot;

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
        if (amount <= 0m ||
            power.Owner != Owner ||
            power.TypeForCurrentAmount != PowerType.Buff ||
            power is MonsterDivinityPower or AmbergrisPower)
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
        target.GetVfxContainer()?.AddChildSafely(SakikoDazzlingImpactVfxNode.Create(target));
        await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner);
        GirlOfSpringPower? girlOfSpring = Owner.Powers.OfType<GirlOfSpringPower>().FirstOrDefault();
        if (girlOfSpring is not null)
        {
            await CreatureCmd.GainBlock(Owner, girlOfSpring.Amount, ValueProp.Unpowered, null);
        }

        NativeSmokeTrace.Info($"Dazzling dealt {Amount} damage after {power.Id} increased.");
    }
}
