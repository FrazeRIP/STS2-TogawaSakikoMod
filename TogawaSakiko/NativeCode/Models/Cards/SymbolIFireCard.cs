using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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

public sealed class SymbolIFireCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(20m, ValueProp.Move), new DynamicVar("MagicNumber", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TimorisCard>()];

    public override string PortraitPath => NativeAssetPaths.SymbolIFirePortrait;

    public SymbolIFireCard()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Symbol I: Fire requires an active combat.");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_fire_burst")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);

        if (!CombatManager.Instance.IsOverOrEnding)
        {
            await PowerCmd.Apply<FireEchoPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["MagicNumber"].BaseValue,
                Owner.Creature,
                this);
        }

        await CombatRewardCardCommand.AddAsync<TimorisCard>(Owner, upgraded: false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
