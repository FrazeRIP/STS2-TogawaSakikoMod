using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff (TODO): in STS1, at end of turn gain Amount HypePower.
/// Implemented as: at end of turn, apply HypePower equal to Amount.
/// </summary>
public class SeizeTheFatePower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Seize the Fate",
            "At the end of your turn, gain !Amount! Hype.",
            "At the end of your turn, gain {Amount} Hype."
        );

    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (side == CombatSide.Player && Owner.Player != null)
        {
            Flash();
            await PowerCmd.Apply<HypePower>(Owner, Amount, Owner, null);
        }
    }
}
