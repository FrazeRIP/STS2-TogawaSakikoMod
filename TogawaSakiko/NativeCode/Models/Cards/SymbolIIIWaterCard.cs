using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SymbolIIIWaterCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [StunIntent.GetStaticHoverTip(), HoverTipFactory.FromCard<DolorisCard>()];

    public override string PortraitPath => NativeAssetPaths.SymbolIIIWaterPortrait;

    public SymbolIIIWaterCard()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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

        if (!cardPlay.Target.IsDead && !cardPlay.Target.IsStunned)
        {
            await CreatureCmd.Stun(cardPlay.Target, null);
        }

        await CombatRewardCardCommand.AddAsync<DolorisCard>(Owner, upgraded: false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
