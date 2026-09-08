using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// In STS1: choose between BlackKeys effect or WhiteKeys effect.
/// Simplified: add both BlackKeys and WhiteKeys cards to hand; player chooses which to play.
/// </summary>
public class BlackAndWhiteKeysCard : TogawaSakikoCard
{
    public BlackAndWhiteKeysCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var black = new BlackKeysCard();
        var white = new WhiteKeysCard();
        await CardPileCmd.AddGeneratedCardToCombat(black, PileType.Hand, addedByPlayer: true);
        await CardPileCmd.AddGeneratedCardToCombat(white, PileType.Hand, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Black and White Keys", "Add Black Keys and White Keys to your hand. Choose one to play.");
}
