using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class FearlessCard : TogawaSakikoCard
{
    public FearlessCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<FearlessPower>(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<FearlessPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Fearless",
            "Apply !FearlessPower! Fearless: at end of each turn, exhaust a card from hand.");
}
