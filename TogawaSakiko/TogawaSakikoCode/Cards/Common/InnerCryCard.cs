using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class InnerCryCard : TogawaSakikoCard
{
    public InnerCryCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<DazzlingPower>(3, upgrade: 1);
        WithBlock(8, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<DazzlingPower>(this);
        await CommonActions.CardBlock(this, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Inner Cry", "Gain !DazzlingPower! Dazzling. Gain !Block! Block.");
}
