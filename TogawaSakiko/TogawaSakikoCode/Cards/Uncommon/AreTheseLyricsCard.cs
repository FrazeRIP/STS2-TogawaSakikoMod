using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// @CardEnable=false in STS1. Simplified skeletal: add Melody cards to hand.
/// </summary>
public class AreTheseLyricsCard : TogawaSakikoCard
{
    public AreTheseLyricsCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("MelodyCount", 2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int count = DynamicVars["MelodyCount"].IntValue;
        for (int i = 0; i < count; i++)
        {
            var melody = new MelodyCard();
            await CardPileCmd.AddGeneratedCardToCombat(melody, PileType.Hand, addedByPlayer: true);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Are These Lyrics?", "Add !MelodyCount! Melody card(s) to your hand.");
}
