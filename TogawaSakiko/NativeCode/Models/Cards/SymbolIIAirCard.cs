using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SymbolIIAirCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16m, ValueProp.Move),
        new DynamicVar("MagicNumber", 3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<AmorisCard>()];

    public override string PortraitPath => NativeAssetPaths.SymbolIIAirPortrait;

    public SymbolIIAirCard()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Symbol II: Air requires an active combat.");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(combatState)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars["MagicNumber"].BaseValue, Owner);
        await CombatRewardCardCommand.AddAsync<AmorisCard>(Owner, upgraded: false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
