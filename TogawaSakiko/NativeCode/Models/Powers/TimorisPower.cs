using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class TimorisPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return;
        }

        Creature owner = Owner;
        int amount = Amount;
        Flash();
        await PowerCmd.Remove(this);
        if (amount <= 0)
        {
            return;
        }

        VulnerablePower? vulnerable = await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            owner,
            amount,
            owner,
            null);
        if (vulnerable is not null)
        {
            vulnerable.SkipNextDurationTick = false;
        }
    }
}
