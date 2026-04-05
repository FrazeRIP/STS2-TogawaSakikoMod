using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: only playable if you have 1 or fewer other Skills in hand; draw 4 cards.
/// The canUse check is not enforced (no easy STS2 equivalent). Just draw 4 cards.
/// </summary>
public class BandInvitationCard : TogawaSakikoCard
{
    public BandInvitationCard() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(4, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Band Invitation",
            "Draw !Cards! cards. (TODO: only playable if 1 or fewer other Skills in hand)");
}
