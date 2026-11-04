using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class BlackBirthdayCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.BlackBirthdayPortrait;

    public BlackBirthdayCard()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    internal static PowerModel[] GetRemovablePowers(IEnumerable<PowerModel> powers)
    {
        return powers.Where(power => power is not SurroundedPower).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Black Birthday requires an active combat.");
        PowerModel[] powers = GetRemovablePowers(Owner.Creature.Powers);
        Creature[] targets = combatState.GetOpponentsOf(Owner.Creature)
            .Where(creature => creature.IsHittable)
            .ToArray();
        Dictionary<Creature, decimal> capturedDamage = targets.ToDictionary(
            target => target,
            target => Hook.ModifyDamage(
                Owner.RunState,
                combatState,
                target,
                Owner.Creature,
                DynamicVars.Damage.BaseValue,
                DynamicVars.Damage.Props,
                this,
                cardPlay,
                ModifyDamageHookType.All,
                CardPreviewMode.None,
                out _));

        // Snapshot both damage and distinct effect count before any protection/clearing runs.
        int hitCount = powers.Select(power => power.Id).Distinct().Count();
        IEnumerable<PowerModel> removalOrder = powers.OrderBy(power => power switch
        {
            KindnessPower => 3,
            ForeverDreamPower => 1,
            HypePower => 2,
            _ => 0
        });
        foreach (PowerModel power in removalOrder)
        {
            if (Owner.Creature.Powers.Contains(power))
            {
                await PowerCmd.Remove(power);
            }
        }

        for (int hit = 0; hit < hitCount; hit++)
        {
            foreach (Creature target in targets.Where(target => target.IsHittable))
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    target,
                    capturedDamage[target],
                    ValueProp.Move | ValueProp.Unpowered,
                    Owner.Creature,
                    this,
                    cardPlay);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
