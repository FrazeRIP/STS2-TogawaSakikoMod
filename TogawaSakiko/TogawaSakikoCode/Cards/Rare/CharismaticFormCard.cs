using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class CharismaticFormCard : TogawaSakikoCard
{
    public CharismaticFormCard() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
    }

    protected override void OnUpgrade()
    {
        WithKeywords(CardKeyword.Innate);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Charismatic Form",
            "Apply !VulnerablePower! Vulnerable to ALL enemies. Upgraded: Innate.");
}
