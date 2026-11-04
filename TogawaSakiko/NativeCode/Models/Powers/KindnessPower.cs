using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class KindnessPower : PowerModel
{
    private sealed class Data
    {
        public bool UntilNextTurn;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    internal bool UntilNextTurn => IsMutable && GetInternalData<Data>().UntilNextTurn;

    public override LocString Description =>
        new("powers", Id.Entry + (UntilNextTurn ? ".extendedDescription" : ".description"));

    protected override string SmartDescriptionLocKey =>
        Id.Entry + (UntilNextTurn ? ".extendedDescription" : ".smartDescription");

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal static async Task AddAsync(PlayerChoiceContext context, CardModel source)
    {
        KindnessPower? power = await PowerCmd.Apply<KindnessPower>(
            context,
            source.Owner.Creature,
            1,
            source.Owner.Creature,
            source);
        if (power is not null)
        {
            power.GetInternalData<Data>().UntilNextTurn |= source.IsUpgraded;
        }
    }

    internal static bool Protects(PowerModel power) =>
        power is not KindnessPower &&
        power.Amount != 0 &&
        CombatManager.Instance.IsInProgress &&
        !CombatManager.Instance.IsEnding &&
        !power.Owner.IsDead &&
        power.TypeForCurrentAmount == PowerType.Buff &&
        power.Owner.Powers.Contains(power) &&
        power.Owner.HasPower<KindnessPower>();

    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!UntilNextTurn && participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
