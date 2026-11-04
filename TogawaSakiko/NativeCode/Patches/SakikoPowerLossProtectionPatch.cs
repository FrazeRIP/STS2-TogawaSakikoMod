using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Patches;

// Clamps Free Next Card at zero, then protects other buffs from reductions: Kindness blocks them,
// and Forever Dream spends one Hype instead, running the native reduction only if nothing was paid.
[HarmonyPatch(
    typeof(PowerCmd),
    nameof(PowerCmd.ModifyAmount),
    [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(decimal),
        typeof(Creature), typeof(CardModel), typeof(bool)])]
internal static class SakikoPowerAmountLossPatch
{
    private static bool Prefix(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        ref decimal offset,
        Creature? applier,
        CardModel? cardSource,
        bool silent,
        ref Task<int> __result)
    {
        if (power is FreeNextCardPower && offset < -power.Amount)
        {
            offset = -power.Amount;
        }

        if (decimal.Truncate(offset) >= 0 || power.Amount <= 0)
        {
            return true;
        }

        if (KindnessPower.Protects(power))
        {
            __result = Task.FromResult(power.Amount);
            return false;
        }

        if (ForeverDreamPower.GetPayment(power) is not { } payment)
        {
            return true;
        }

        __result = ResolveAsync(payment, choiceContext, power, offset, applier, cardSource, silent);
        return false;
    }

    private static async Task<int> ResolveAsync(
        HypePower payment,
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal offset,
        Creature? applier,
        CardModel? cardSource,
        bool silent)
    {
        return await ForeverDreamPower.TryPayAsync(payment)
            ? power.Amount
            : await Original(choiceContext, power, offset, applier, cardSource, silent);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(
        typeof(PowerCmd),
        nameof(PowerCmd.ModifyAmount),
        [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(decimal),
            typeof(Creature), typeof(CardModel), typeof(bool)])]
    internal static Task<int> Original(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal offset,
        Creature? applier,
        CardModel? cardSource,
        bool silent) =>
        throw new NotImplementedException("Harmony reverse patch");
}

// Kindness blocks buff removal; Forever Dream spends one Hype instead, removing only if nothing was paid.
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Remove), [typeof(PowerModel)])]
internal static class SakikoPowerRemovalPatch
{
    private static bool Prefix(PowerModel? power, ref Task __result)
    {
        // Native Remove accepts null (e.g. Remove(creature.GetPower<T>()) when T is absent).
        if (power is null)
        {
            return true;
        }

        if (KindnessPower.Protects(power))
        {
            __result = Task.CompletedTask;
            return false;
        }

        if (ForeverDreamPower.GetPayment(power) is not { } payment)
        {
            return true;
        }

        __result = ResolveAsync(payment, power);
        return false;
    }

    private static async Task ResolveAsync(HypePower payment, PowerModel power)
    {
        if (!await ForeverDreamPower.TryPayAsync(payment))
        {
            await Original(power);
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Remove), [typeof(PowerModel)])]
    internal static Task Original(PowerModel? power) => throw new NotImplementedException("Harmony reverse patch");
}
