using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: Gain 15 gold + add Tiredness. Gold gain mid-combat not ideal.
/// Simplified: draw 2 cards + add Tiredness.
/// </summary>
public class ClockOutCard : TogawaSakikoCard
{
    public ClockOutCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(2, upgrade: 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.Draw(this, ctx);
        var tired = new TirednessCard();
        await CardPileCmd.AddGeneratedCardToCombat(tired, PileType.Discard, addedByPlayer: false);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Clock Out", "Draw !Cards! cards. Add Tiredness to discard. Exhaust. (TODO: gain gold)");
}
