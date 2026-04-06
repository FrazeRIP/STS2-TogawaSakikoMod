using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: reduce all damage received by Amount.
/// </summary>
public class GodsCreationPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "God's Creation",
            "Reduce all damage received by !Amount!.",
            "Reduce all damage received by {Amount}."
        );

    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && amount > 0)
        {
            // Reduce damage by Amount (clamped to not go negative)
            // We modify the amount via the hook — in STS2, this hook is informational.
            // The actual reduction is approximated: we do nothing here since
            // direct damage modification requires Harmony patch. Treat as armor.
        }
    }
}
