using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class FallenFlowersCard : TogawaSakikoCard
{
    public FallenFlowersCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<DazzlingPower>(7, upgrade: 2);
        WithKeywords(CardKeyword.Ethereal);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DazzlingPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Fallen Flowers", "Gain !DazzlingPower! Dazzling. Ethereal.");
}
