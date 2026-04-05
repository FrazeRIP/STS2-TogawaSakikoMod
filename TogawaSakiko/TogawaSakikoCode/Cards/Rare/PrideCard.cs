using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: Innate; add 1 random Attack to hand at start of turn via PridePower.
/// TODO: PridePower tracking complex. Simplified: add 2 random attacks to hand, Exhaust.
/// </summary>
public class PrideCard : TogawaSakikoCard
{
    public PrideCard() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(2, upgrade: 1);
        WithKeywords(CardKeyword.Innate, CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Add random attacks from the card pool to hand
        int count = DynamicVars.Cards.IntValue;
        for (int i = 0; i < count; i++)
        {
            await CardPileCmd.Draw(ctx, 1, Owner);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Pride",
            "Draw !Cards! cards. Innate. Exhaust. (TODO: add random Attack cards to hand via PridePower)");
}
