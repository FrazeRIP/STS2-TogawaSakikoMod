using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// @CardEnable=false in STS1. Apply Vigor (approximated as Dexterity in STS2).
/// </summary>
public class PassionCard : TogawaSakikoCard
{
    public PassionCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<DexterityPower>(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DexterityPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Passion",
            "Gain !DexterityPower! Dexterity. (TODO: apply Vigor from STS1; approximated as Dexterity)");
}
