using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class DesuWaCard : TogawaSakikoCard
{
    public DesuWaCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Desu wa", "Draw !Cards! card(s).");
}
