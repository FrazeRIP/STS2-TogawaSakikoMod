using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class NextTurnArdorPower : PowerModel
{
    // Pending tracks Ardor queued through AddAsync and can diverge from Amount: power-amount hooks
    // change what Apply adds, and Kindness or Forever Dream can block the removal below.
    private sealed class Data
    {
        public int Pending;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal static async Task AddAsync(PlayerChoiceContext context, Creature owner, int amount, CardModel source)
    {
        NextTurnArdorPower? power = await PowerCmd.Apply<NextTurnArdorPower>(context, owner, amount, owner, source);
        if (power is not null)
        {
            power.GetInternalData<Data>().Pending += amount;
        }
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        int amount = data.Pending;
        data.Pending = 0;
        if (amount <= 0)
        {
            return;
        }

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<ArdorPower>(choiceContext, Owner, amount, Owner, null);
    }
}
