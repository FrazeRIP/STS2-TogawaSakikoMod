using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: whenever you draw a 0-cost or unplayable card, remove it from combat and draw a replacement.
/// </summary>
public class PerdereOmniaPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Perdere Omnia",
            "Whenever you draw a 0-cost or Unplayable card, exhaust it and draw a replacement.",
            "Whenever you draw a 0-cost or Unplayable card, exhaust it and draw a replacement."
        );

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (Owner.Player != null && card.Owner == Owner.Player &&
            (card.EnergyCost.Cost == 0 || card.Keywords.Contains(CardKeyword.Unplayable)))
        {
            Flash();
            await CardPileCmd.Add(card, PileType.Exhaust);
            await CardPileCmd.Draw(ctx, 1, Owner.Player);
        }
    }
}
