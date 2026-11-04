using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class BandAffairsCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(9m, ValueProp.Move), new CardsVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<TirednessCard>()];

    public override string PortraitPath => NativeAssetPaths.ClockOutPortrait;

    public BandAffairsCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        var addition = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(Owner);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
