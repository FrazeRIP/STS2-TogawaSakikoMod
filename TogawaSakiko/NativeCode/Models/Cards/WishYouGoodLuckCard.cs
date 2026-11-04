using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class WishYouGoodLuckCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(16m, ValueProp.Move), new RepeatVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ThornsPower>()];

    public override string PortraitPath => NativeAssetPaths.WishYouGoodLuckPortrait;

    public WishYouGoodLuckCard()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "WishYouGoodLuck");
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars.Repeat.UpgradeValueBy(2m);
    }
}
