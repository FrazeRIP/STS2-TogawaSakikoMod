using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class CharismaticFormPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        int copiedAmount = (int)amount;
        if (copiedAmount <= 0 ||
            power.Owner.Side == Owner.Side ||
            power.TypeForCurrentAmount != PowerType.Buff)
        {
            return;
        }

        PowerCopyCompatibility compatibility = PowerCopyCommand.GetCompatibility(power);
        if (!compatibility.Supported)
        {
            NativeSmokeTrace.N5Info(
                $"Charismatic Form skipped unsupported {power.Id}: {compatibility.Reason}");
            return;
        }

        Flash();
        for (int index = 0; index < Amount; index++)
        {
            PowerCopyResult result = await PowerCopyCommand.ApplyCopyAsync(
                choiceContext,
                power,
                Owner,
                PowerCopyApplierPolicy.Target,
                cardSource: cardSource,
                amountOverride: copiedAmount);
            if (!result.Success)
            {
                NativeSmokeTrace.N5Info(
                    $"Charismatic Form could not copy {power.Id}: {result.Reason}");
                break;
            }
        }
    }
}
