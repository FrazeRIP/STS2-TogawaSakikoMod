using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: Gain 2 energy. Innate. Exhaust.
/// TODO: Energy gain mid-combat not available. Simplified: draw 2 cards, Innate, Exhaust.
/// </summary>
public class GreetingsCard : TogawaSakikoCard
{
    public GreetingsCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(2, upgrade: 1);
        WithKeywords(CardKeyword.Innate, CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Greetings", "Draw !Cards! cards. Innate. Exhaust. (TODO: gain energy)");
}
