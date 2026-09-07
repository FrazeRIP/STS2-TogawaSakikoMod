using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class RhinocerosBeetleCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(4m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Unpowered)
            .WithMultiplier(static (CardModel card, Creature? _) =>
                decimal.Floor((card.Owner.Creature.GetPower<DazzlingPower>()?.Amount ?? 0m) / 2m))
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DazzlingPower>()];

    public override string PortraitPath => NativeAssetPaths.RhinocerosBeetlePortrait;

    public RhinocerosBeetleCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "RhinocerosBeetle");
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(null),
            DynamicVars.CalculatedBlock.Props,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
    }
}
