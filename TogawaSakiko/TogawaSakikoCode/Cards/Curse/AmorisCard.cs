using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Curse;

public class AmorisCard : TogawaSakikoCard
{
    public AmorisCard() : base(0, CardType.Curse, CardRarity.Curse, TargetType.Self, autoAdd: false)
    {
        WithKeywords(CardKeyword.Unplayable, CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Unplayable; no effect.
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Amoris", "Unplayable. Retain. At the end of your turn, this card flashes harmlessly.");
}
