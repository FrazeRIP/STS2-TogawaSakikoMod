using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: Gain 1 extra energy next turn; draw 1 card.
/// TODO: Energy next turn not available. Simplified: draw 2 cards.
/// </summary>
public class CountingStarsCard : TogawaSakikoCard
{
    public CountingStarsCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Counting Stars", "Draw !Cards! cards. (TODO: gain 1 energy next turn)");
}
