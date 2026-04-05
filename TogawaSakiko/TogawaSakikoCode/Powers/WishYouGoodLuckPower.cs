using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: when you receive 0 damage from an attack, gain Amount Thorns.
/// </summary>
public class WishYouGoodLuckPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Wish You Good Luck",
            "When you receive 0 damage from an attack, gain !Amount! Thorns.",
            "When you receive 0 damage from an attack, gain {Amount} Thorns."
        );

    public override async Task AfterDamageReceived(
        PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.DamageDealt == 0 && dealer != null)
        {
            Flash();
            await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.ThornsPower>(Owner, Amount, Owner, null);
        }
    }
}
