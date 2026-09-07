using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class MantraPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<MonsterDivinityPower>()];

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount <= 0m || Amount < SakikoStanceCmd.MantraThreshold)
        {
            return;
        }

        Flash();
        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -SakikoStanceCmd.MantraThreshold,
            Owner,
            cardSource,
            silent: true);
        await SakikoStanceCmd.EnterDivinityAsync(
            choiceContext,
            Owner,
            applier ?? Owner,
            cardSource);
    }
}
