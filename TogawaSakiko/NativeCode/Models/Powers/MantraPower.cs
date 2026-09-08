using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using TogawaSakiko.NativeCode.Commands;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class MantraPower : PowerModel
{
    private bool UsesPlayerDescription => !IsMutable || Owner?.IsPlayer != false;

    public override LocString Description => UsesPlayerDescription
        ? new LocString("powers", Id.Entry + ".playerDescription")
        : base.Description;

    protected override string SmartDescriptionLocKey => UsesPlayerDescription
        ? Id.Entry + ".playerSmartDescription"
        : base.SmartDescriptionLocKey;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [UsesPlayerDescription
            ? HoverTipFactory.FromPower<MonsterDivinityPower>()
            : new HoverTip(
                new LocString("powers", ModelDb.Power<MonsterDivinityPower>().Id.Entry + ".title"),
                ModelDb.Power<MonsterDivinityPower>().Description)];

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
