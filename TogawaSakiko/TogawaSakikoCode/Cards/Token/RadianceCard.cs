using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class RadianceCard : TogawaSakikoCard
{
    public RadianceCard() : base(1, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<DazzlingPower>(4, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DazzlingPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Radiance", "Gain !DazzlingPower! Dazzling.");
}
