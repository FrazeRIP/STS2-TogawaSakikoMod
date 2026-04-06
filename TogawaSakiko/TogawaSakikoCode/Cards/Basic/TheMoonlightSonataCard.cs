using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Basic;

public class TheMoonlightSonataCard : TogawaSakikoCard
{
    public TheMoonlightSonataCard() : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(15, upgrade: 5);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: 2).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("The Moonlight Sonata", "Hit twice for !Damage! damage. Exhaust.");
}
