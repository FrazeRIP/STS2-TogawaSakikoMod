using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class PhantomOfSakikoCard : TogawaSakikoCard
{
    public PhantomOfSakikoCard() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, upgrade: 2);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: 3).Execute(ctx);
        var radiance = new RadianceCard();
        await CardPileCmd.AddGeneratedCardToCombat(radiance, PileType.Discard, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Phantom of Sakiko", "Hit 3 times for !Damage! damage. Add Radiance to discard. Exhaust.");
}
