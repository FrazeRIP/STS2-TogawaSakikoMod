using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: whenever a Desire card is played, draw Amount cards.
/// </summary>
public class CrueltyPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Cruelty",
            "Whenever you play a Desire card, draw !Amount! card(s).",
            "Whenever you play a Desire card, draw {Amount} card(s)."
        );

    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (cardPlay.Card is DesireCard)
        {
            await CardPileCmd.Draw(ctx, Amount, Owner.Player!);
        }
    }
}
