using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff/Counter: at the start of each turn, add a random Phantom (Melody) card to hand,
/// then reduce stack by 1. When stacks reach 0 the power is consumed.
/// </summary>
public class CrychicPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Crychic",
            "At the start of your turn, add a Melody to your hand. Trigger !Amount! time(s).",
            "At the start of your turn, add a Melody to your hand. Trigger {Amount} time(s)."
        );

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (Owner == player.Creature)
        {
            var melody = new MelodyCard();
            await CardPileCmd.AddGeneratedCardToCombat(melody, MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand, addedByPlayer: true);
            await PowerCmd.ModifyAmount(this, -1, Owner, null);
        }
    }
}
