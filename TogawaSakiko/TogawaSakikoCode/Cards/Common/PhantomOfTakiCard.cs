using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class PhantomOfTakiCard : TogawaSakikoCard
{
    public PhantomOfTakiCard() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(12, upgrade: 4);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var ideal = new IdealCard();
        await CardPileCmd.AddGeneratedCardToCombat(ideal, PileType.Discard, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Phantom of Taki", "Deal !Damage! damage. Add Ideal to discard. Exhaust.");
}
