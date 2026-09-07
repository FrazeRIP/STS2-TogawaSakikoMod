using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class WishToBecomeHumanCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DazzlingPower>()];

    public override string PortraitPath => NativeAssetPaths.WishToBecomeHumanPortrait;

    public WishToBecomeHumanCard()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        SakikoAudioCmd.TryPlayCardVoice(Owner, "WishToBecomeHuman");
        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(2)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        int unblockedDamage = attack.Results
            .SelectMany(results => results)
            .Sum(result => result.UnblockedDamage);
        if (unblockedDamage > 0)
        {
            await PowerCmd.Apply<DazzlingPower>(
                choiceContext,
                Owner.Creature,
                unblockedDamage,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
