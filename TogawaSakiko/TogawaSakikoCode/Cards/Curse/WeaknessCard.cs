using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Curse;

/// <summary>
/// @CardEnable=false in STS1. Unplayable curse, skeletal.
/// </summary>
public class WeaknessCard : TogawaSakikoCard
{
    public WeaknessCard() : base(0, CardType.Curse, CardRarity.Curse, TargetType.Self, autoAdd: false)
    {
        WithKeywords(CardKeyword.Unplayable);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Weakness", "Unplayable.");
}
