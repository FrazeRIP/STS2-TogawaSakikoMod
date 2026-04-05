using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// @CardEnable=false in STS1. Simplified: draw 2 cards.
/// (In STS1: draw 2 then choose 1 to retain — no retain-on-choice API in STS2.)
/// </summary>
public class CarefreeCard : TogawaSakikoCard
{
    public CarefreeCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Carefree", "Draw !Cards! cards. (TODO: choose 1 to retain)");
}
