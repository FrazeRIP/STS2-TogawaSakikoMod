using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class DesireCard : TogawaSakikoCard
{
    public DesireCard() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithBlock(8, upgrade: 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Desire", "Gain !Block! Block.");
}
