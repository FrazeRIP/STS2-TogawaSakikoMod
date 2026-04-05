using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: when you would lose HP, gain Block equal to Amount first (approximation via BeforeDamageReceived).
/// </summary>
public class EndurancePower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Endurance",
            "When you take damage, gain !Amount! Block.",
            "When you take damage, gain {Amount} Block."
        );

    public override async Task BeforeDamageReceived(
        PlayerChoiceContext ctx, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && amount > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, BlockProps.cardUnpowered, null);
        }
    }
}
