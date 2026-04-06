using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

/// <summary>
/// Buff: whenever a new buff is applied to you, draw Amount cards.
/// </summary>
public class SharedDestinyPower : TogawaSakikoPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Shared Destiny",
            "Whenever a buff is applied to you, draw !Amount! card(s).",
            "Whenever a buff is applied to you, draw {Amount} card(s)."
        );

    public override async Task AfterPowerAmountChanged(
        PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
    }
}
