using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class MementoMoriCard : CardModel
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(30m, ValueProp.Move), new CardsVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<MonsterDivinityPower>()];

    public override string PortraitPath => NativeAssetPaths.MementoMoriPortrait;

    public MementoMoriCard()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
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
    }

    internal async Task AfterPermanentRemovalAsync()
    {
        if (!CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsOverOrEnding ||
            Owner.Creature.IsDead)
        {
            return;
        }

        var context = new HookPlayerChoiceContext(
            Owner,
            LocalContext.NetId ?? Owner.NetId,
            GameActionType.CombatPlayPhaseOnly);
        context.PushModel(this);
        await context.AssignTaskAndWaitForPauseOrCompletion(ResolveRemovalAsync(context));
    }

    private async Task ResolveRemovalAsync(PlayerChoiceContext context)
    {
        await SakikoStanceCmd.EnterDivinityAsync(context, Owner.Creature, Owner.Creature, this);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2m);
    }
}
