using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Tracking;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPowerAmountChanged))]
internal static class PowerChangeLedgerNativeHookPatch
{
    private static void Prefix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (combatState is CombatState concreteState)
        {
            // Recording is synchronous. Preserve the native task and all gameplay hook ordering.
            PowerChangeLedgerService.GetOrCreateHook(concreteState)
                .RecordPowerAmountChanged(choiceContext, power, amount, applier, cardSource)
                .GetAwaiter().GetResult();
        }
    }
}
