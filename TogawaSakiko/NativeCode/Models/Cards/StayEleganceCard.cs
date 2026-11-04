using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class StayEleganceCard : CardModel
{
    public override bool CanBeGeneratedInCombat => false;

    public override string PortraitPath => NativeAssetPaths.StayElegancePortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public StayEleganceCard()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "StayElegance");
        await PowerCmd.Apply<StayElegancePower>(
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
