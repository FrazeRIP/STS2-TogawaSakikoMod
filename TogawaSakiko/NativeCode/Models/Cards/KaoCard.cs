using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class KaoCard : CardModel
{
    private static readonly Action<CombatStateTracker, string> NotifyCombatStateChanged =
        (AccessTools.Method(typeof(CombatStateTracker), "NotifyCombatStateChanged") ??
            throw new MissingMethodException(typeof(CombatStateTracker).FullName, "NotifyCombatStateChanged"))
        .CreateDelegate<Action<CombatStateTracker, string>>();

    private int _combatDamageIncrease;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override bool ShouldGlowGoldInternal => CombatState?.HittableEnemies.Any(enemy =>
        enemy.Side != Owner.Creature.Side && enemy.Monster?.NextMove.Intents.Any(intent => intent is BuffIntent) == true) ?? false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move), new DynamicVar("MagicNumber", 4m)];

    public override string PortraitPath => NativeAssetPaths.KaoPortrait;

    public KaoCard()
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
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Pile?.Type == PileType.Hand &&
            amount > 0m &&
            power.Owner.Side != Owner.Creature.Side &&
            power.TypeForCurrentAmount == PowerType.Buff)
        {
            int increase = DynamicVars["MagicNumber"].IntValue;
            _combatDamageIncrease += increase;
            // Applying a new power may refresh the hand before this hook runs; refresh after our own growth too.
            NotifyCombatStateChanged(CombatManager.Instance.StateTracker, nameof(KaoCard));
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        ReferenceEquals(cardSource, this) && props.IsPoweredAttack() ? _combatDamageIncrease : 0m;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        // Combat growth remains separate from the base value so native previews color modified damage.
    }
}
