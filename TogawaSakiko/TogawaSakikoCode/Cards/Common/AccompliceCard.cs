using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class AccompliceCard : TogawaSakikoCard
{
    public AccompliceCard() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("DesireCount", 2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        int count = DynamicVars["DesireCount"].IntValue;
        for (int i = 0; i < count; i++)
        {
            var desire = new DesireCard();
            await CardPileCmd.AddGeneratedCardToCombat(desire, PileType.Hand, addedByPlayer: true);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Accomplice", "Add !DesireCount! Desire card(s) to your hand.");
}
