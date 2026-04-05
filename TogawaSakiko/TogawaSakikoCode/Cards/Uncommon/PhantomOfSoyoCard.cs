using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class PhantomOfSoyoCard : TogawaSakikoCard
{
    public PhantomOfSoyoCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(7, upgrade: 3);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var kindness = new KindnessCard();
        await CardPileCmd.AddGeneratedCardToCombat(kindness, PileType.Discard, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Phantom of Soyo", "Deal !Damage! damage to ALL enemies. Add Kindness to discard. Exhaust.");
}
