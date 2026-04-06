using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.TogawaSakikoCode.Cards.Curse;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class SymbolIVEarthCard : TogawaSakikoCard
{
    public SymbolIVEarthCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithBlock(15, upgrade: 5);
        WithCalculatedDamage(0,
            (card, target) => card.Owner?.Creature.Block ?? 0m,
            DamageProps.card, upgrade: 0, bonusUpgrade: 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var mortis = new MortisCard();
        await CardPileCmd.AddGeneratedCardToCombat(mortis, PileType.Discard, addedByPlayer: false);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Symbol IV: Earth",
            "Gain !Block! Block. Deal damage equal to your total Block. Add Mortis (curse) to discard.");
}
