using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UnnamedFourCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.MomentMemorySmallPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move), new PowerVar<DazzlingPower>(3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DazzlingPower>()];

    public UnnamedFourCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(DazzlingPower)].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[nameof(DazzlingPower)].UpgradeValueBy(1m);
    }
}
