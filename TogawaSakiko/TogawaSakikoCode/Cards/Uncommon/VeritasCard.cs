using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class VeritasCard : TogawaSakikoCard
{
    public VeritasCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(3, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Veritas", "Draw !Cards! cards.");
}
