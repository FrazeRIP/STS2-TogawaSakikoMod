using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SymbolIVEarthCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(15m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier(static (CardModel card, Creature? _) =>
                card.Owner.Creature.Block + PreviewBlockGain(card))
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<MortisCard>()];

    public override string PortraitPath => NativeAssetPaths.SymbolIVEarthPortrait;

    public SymbolIVEarthCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await DamageCmd.Attack(Owner.Creature.Block)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);

        await CombatRewardCardCommand.AddAsync<MortisCard>(Owner, upgraded: false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }

    private static decimal PreviewBlockGain(CardModel card)
    {
        if (card.CombatState is null)
        {
            return card.DynamicVars.Block.BaseValue;
        }

        decimal block = Hook.ModifyBlock(
            card.CombatState,
            card.Owner.Creature,
            card.DynamicVars.Block.BaseValue,
            ValueProp.Move,
            card,
            null,
            out _);
        return Math.Max(0m, decimal.Floor(block));
    }
}
