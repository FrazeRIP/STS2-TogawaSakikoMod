using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SpringSunlightCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(30m, ValueProp.Move),
        new DynamicVar("MagicNumber", 6m)
    ];

    public override string PortraitPath => NativeAssetPaths.SpringSunlightPortrait;

    public SpringSunlightCard()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    internal static int CalculateCost(int persistentDeckSize)
    {
        return persistentDeckSize < 6 ? 0 : persistentDeckSize / 6;
    }

    internal static void RefreshCombatCosts(Player player)
    {
        if (player.PlayerCombatState is null)
        {
            return;
        }

        int cost = CalculateCost(player.Deck.Cards.Count);
        foreach (SpringSunlightCard card in player.PlayerCombatState.AllCards.OfType<SpringSunlightCard>())
        {
            // Only change the base: native free-attack and cost modifiers still apply afterward.
            card.EnergyCost.SetCustomBaseCost(cost);
        }
    }

    public override Task BeforeCombatStart()
    {
        // Initial deck copies enter Draw directly, without AfterCardEnteredCombat.
        RefreshCombatCosts(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner == Owner && card is SpringSunlightCard)
        {
            RefreshCombatCosts(Owner);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (card.Owner == Owner &&
            (oldPileType == PileType.Deck || card.Pile?.Type == PileType.Deck ||
             (card == this && card.Pile?.Type == PileType.Hand)))
        {
            RefreshCombatCosts(Owner);
        }
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Spring Sunlight requires an active combat.");
        RefreshCombatCosts(Owner);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}
