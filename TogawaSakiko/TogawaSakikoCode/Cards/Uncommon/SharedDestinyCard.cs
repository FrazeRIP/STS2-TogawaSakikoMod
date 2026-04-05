using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class SharedDestinyCard : TogawaSakikoCard
{
    public SharedDestinyCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<SharedDestinyPower>(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<SharedDestinyPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Shared Destiny",
            "Apply !SharedDestinyPower! Shared Destiny: whenever a buff is applied to you, draw a card.");
}
