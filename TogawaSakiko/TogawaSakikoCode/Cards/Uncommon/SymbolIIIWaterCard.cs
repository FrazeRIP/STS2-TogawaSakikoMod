using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Curse;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class SymbolIIIWaterCard : TogawaSakikoCard
{
    public SymbolIIIWaterCard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
        WithDamage(16, upgrade: 4);
        // In STS1 ends turn after play — not available in STS2
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var doloris = new DolorisCard();
        await CardPileCmd.AddGeneratedCardToCombat(doloris, PileType.Discard, addedByPlayer: false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 2 → 1
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Symbol III: Water",
            "Deal !Damage! damage to a random enemy. Add Doloris (curse) to discard. (TODO: end turn after play)");
}
