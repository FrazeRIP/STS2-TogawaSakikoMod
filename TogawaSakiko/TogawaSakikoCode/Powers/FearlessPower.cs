using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff/Counter: at the end of each turn, exhaust 1 card from hand, then reduce stacks by 1.
/// </summary>
public class FearlessPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Fearless",
            "At the end of your turn, exhaust the top card of your hand. Trigger !Amount! time(s).",
            "At the end of your turn, exhaust the top card of your hand. Trigger {Amount} time(s)."
        );

    public override async Task BeforeTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (side == CombatSide.Player && Owner.Player != null)
        {
            var hand = Owner.Player.Hand;
            if (hand.Count > 0)
            {
                Flash();
                var card = hand[0];
                await CardPileCmd.Add(card, MegaCrit.Sts2.Core.Entities.Cards.PileType.Exhaust);
            }
            await PowerCmd.ModifyAmount(this, -1, Owner, null);
        }
    }
}
