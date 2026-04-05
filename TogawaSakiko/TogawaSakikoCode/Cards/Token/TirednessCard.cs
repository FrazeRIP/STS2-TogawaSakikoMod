using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class TirednessCard : TogawaSakikoCard
{
    public TirednessCard() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithCards(1, upgrade: 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Tiredness", "Draw !Cards! card(s). Exhaust.");
}
