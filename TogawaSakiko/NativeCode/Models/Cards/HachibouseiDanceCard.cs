using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class HachibouseiDanceCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("SelfDamage", 8m),
        new BlockVar(8m, ValueProp.Move)
    ];

    public override string PortraitPath => NativeAssetPaths.HachibouseiDancePortrait;

    public HachibouseiDanceCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["SelfDamage"].BaseValue,
            ValueProp.Unpowered,
            Owner.Creature);
        if (Owner.Creature.IsDead)
        {
            return;
        }

        if (cardPlay.Target.IsHittable)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_heavy_blunt")
                .Execute(choiceContext);
        }

        if (!CombatManager.Instance.IsOverOrEnding)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["SelfDamage"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
