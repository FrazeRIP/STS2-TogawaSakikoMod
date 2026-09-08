using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Presentation.Godot;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class EtherCard : CardModel
{
    public const int MaximumExhaustCount = 12;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<OblivionisCard>()];

    public override string PortraitPath => NativeAssetPaths.EtherPortrait;

    public EtherCard()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    internal static CardModel[] SelectCardsToExhaust(IEnumerable<CardModel> drawPile)
    {
        return drawPile
            .Where(card => card.Type != CardType.Attack)
            .Take(MaximumExhaustCount)
            .ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var combatState = CombatState
            ?? throw new InvalidOperationException("Ether requires an active combat.");
        int exhaustedCount = 0;
        foreach (CardModel card in SelectCardsToExhaust(PileType.Draw.GetPile(Owner).Cards))
        {
            CardPileAddResult? result = await CardCmd.Exhaust(choiceContext, card);
            if (result is { success: true })
            {
                exhaustedCount++;
            }
        }

        if (exhaustedCount > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(exhaustedCount)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitVfxNode(target => SakikoDazzlingImpactVfxNode.Create(target))
                .Execute(choiceContext);
        }

        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<OblivionisCard>(
                Owner,
                combatState,
                PileType.Discard);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
