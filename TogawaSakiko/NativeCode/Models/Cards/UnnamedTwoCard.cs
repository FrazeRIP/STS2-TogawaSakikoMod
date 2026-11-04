using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UnnamedTwoCard : CardModel
{
    public override bool GainsBlock => true;

    public override string PortraitPath => NativeAssetPaths.BudgetBentoPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7m, ValueProp.Move), new RepeatVar(2)];

    public UnnamedTwoCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await PowerCmd.Apply<ArdorPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1);
    }
}
