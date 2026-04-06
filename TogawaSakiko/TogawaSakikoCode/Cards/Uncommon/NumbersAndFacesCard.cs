using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// @CardEnable=false in STS1. Skeletal: apply 1 Strength.
/// </summary>
public class NumbersAndFacesCard : TogawaSakikoCard
{
    public NumbersAndFacesCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<StrengthPower>(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<StrengthPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Numbers and Faces",
            "Gain !StrengthPower! Strength. (TODO: complex power tracker from STS1)");
}
