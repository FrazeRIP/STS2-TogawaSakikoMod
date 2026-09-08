using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class BlackBirthdayCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.BlackBirthdayPortrait;

    public BlackBirthdayCard()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
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

        foreach (PowerModel power in powers.Reverse())
        {
            await PowerCmd.Remove(power);
            foreach (Creature target in targets)
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
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
