using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UnnamedHypeCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.NumbersAndFacesPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HypePower>()];

    public UnnamedHypeCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        var normal = base.GetResultLocationForCardPlay();
        return normal.pileType == PileType.Discard
            ? new CardLocation(Owner, PileType.Draw, CardPilePosition.Top)
            : normal;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HypePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
