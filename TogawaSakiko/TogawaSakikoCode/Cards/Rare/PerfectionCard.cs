using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class PerfectionCard : TogawaSakikoCard
{
    public PerfectionCard() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var hand = Owner.Hand.ToList();
        foreach (var card in hand)
        {
            if (!card.IsUpgraded)
                card.Upgrade();
        }
    }
    // TODO: OnUpgrade not supported in STS2. Upgrade: 3 → 2

    public override List<(string, string)>? Localization =>
        new CardLoc("Perfection", "Upgrade all cards in your hand. Exhaust.");
}
