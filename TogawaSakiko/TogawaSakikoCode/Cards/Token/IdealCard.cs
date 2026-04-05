using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

/// <summary>
/// In STS1, the next attacks cost 0. Approximated as gaining Strength.
/// </summary>
public class IdealCard : TogawaSakikoCard
{
    public IdealCard() : base(1, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<StrengthPower>(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<StrengthPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Ideal", "Gain !StrengthPower! Strength.");
}
