using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Debuff: at end of turn, reduce the target's DazzlingPower by Amount.
/// </summary>
public class DazzlingDownPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Dazzling Down",
            "At the end of your turn, lose !Amount! Dazzling.",
            "At the end of your turn, lose {Amount} Dazzling."
        );

    public override async Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (side == CombatSide.Player && Owner.Player != null)
        {
            var dazzling = Owner.Powers.FirstOrDefault(p => p is DazzlingPower);
            if (dazzling != null)
            {
                Flash();
                await PowerCmd.ModifyAmount(dazzling, -Amount, Owner, null);
            }
        }
    }
}
