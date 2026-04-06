using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class WishYouGoodLuckCard : TogawaSakikoCard
{
    public WishYouGoodLuckCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<WishYouGoodLuckPower>(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<WishYouGoodLuckPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Wish You Good Luck",
            "Apply !WishYouGoodLuckPower! Wish You Good Luck: when you take 0 damage, gain Thorns.");
}
