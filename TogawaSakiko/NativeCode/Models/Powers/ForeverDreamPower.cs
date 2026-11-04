using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class ForeverDreamPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    internal static HypePower? GetPayment(PowerModel power)
    {
        var owner = power.Owner;
        if (!CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsEnding ||
            owner.IsDead ||
            power.Amount == 0 ||
            power is HypePower or KindnessPower ||
            power.TypeForCurrentAmount != PowerType.Buff ||
            !owner.Powers.Contains(power) ||
            !owner.HasPower<ForeverDreamPower>())
        {
            return null;
        }

        return owner.GetPower<HypePower>() is { Amount: > 0 } hype && !KindnessPower.Protects(hype) ? hype : null;
    }

    internal static async Task<bool> TryPayAsync(HypePower payment)
    {
        var owner = payment.Owner;
        int before = payment.Amount;
        await payment.ConsumeOneAsync();
        return !owner.Powers.Contains(payment) || payment.Amount < before;
    }
}
