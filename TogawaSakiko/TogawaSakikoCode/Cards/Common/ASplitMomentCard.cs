using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class ASplitMomentCard : TogawaSakikoCard
{
    public ASplitMomentCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(1);
        WithPower<DazzlingPower>(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.ApplySelf<DazzlingPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("A Split Moment", "Gain !Block! Block. Gain !DazzlingPower! Dazzling.");
}
