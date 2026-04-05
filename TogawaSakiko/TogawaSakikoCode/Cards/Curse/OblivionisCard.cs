using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Curse;

public class OblivionisCard : TogawaSakikoCard
{
    public OblivionisCard() : base(0, CardType.Curse, CardRarity.Curse, TargetType.Self, autoAdd: false)
    {
        WithKeywords(CardKeyword.Unplayable, CardKeyword.Ethereal);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Oblivionis", "Unplayable. Ethereal. When drawn, exhausts without effect.");
}
