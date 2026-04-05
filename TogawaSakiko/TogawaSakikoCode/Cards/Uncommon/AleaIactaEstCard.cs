using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class AleaIactaEstCard : TogawaSakikoCard
{
    public AleaIactaEstCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Upgrade all cards in hand
        var hand = Owner.Hand.ToList();
        foreach (var card in hand)
        {
            if (!card.IsUpgraded)
                card.Upgrade();
        }
        if (IsUpgraded)
        {
            var drawPile = Owner.DrawPile.ToList();
            foreach (var card in drawPile)
            {
                if (!card.IsUpgraded)
                    card.Upgrade();
            }
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Alea Iacta Est",
            "Upgrade all cards in your hand. Upgraded: also upgrade draw pile. Exhaust.");
}
