using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

/// <summary>
/// In STS1 restores lost powers. Approximated as gaining Dazzling.
/// </summary>
public class KindnessCard : TogawaSakikoCard
{
    public KindnessCard() : base(1, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<DazzlingPower>(3, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DazzlingPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Kindness", "Gain !DazzlingPower! Dazzling. (TODO: restore lost powers)");
}
