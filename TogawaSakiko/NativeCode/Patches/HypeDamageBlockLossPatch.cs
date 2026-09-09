using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDamageReceived))]
internal static class HypeBeforeDamageBlockLossPatch
{
    private static void Postfix(Creature target, decimal amount, ValueProp props, ref Task __result)
    {
        __result = PreparePreventionAsync(__result, target, amount, props);
    }

    private static async Task PreparePreventionAsync(Task hooks, Creature target, decimal amount, ValueProp props)
    {
        await hooks;

        // Native damage uses the pet owner's block when the target is a pet.
        Creature blockOwner = target.PetOwner?.Creature ?? target;
        HypePower? hype = blockOwner.GetPower<HypePower>();
        if (props.HasFlag(ValueProp.Unblockable) ||
            amount < 1m ||
            !HypeBlockLossPolicy.CanPreventExplicitLoss(
                CombatManager.Instance.IsInProgress,
                CombatManager.Instance.IsEnding,
                blockOwner.IsDead,
                blockOwner.Block,
                amount,
                hype?.Amount ?? 0))
        {
            return;
        }

        // Finish the native power command before damage continues, including its
        // hooks and final-stack removal. Arm only afterwards so nested damage
        // during those hooks cannot take this hit's prevention.
        await hype!.ConsumeOneAsync();
        HypeDamageBlockLossPatch.Arm(blockOwner);
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.DamageBlockInternal))]
internal static class HypeDamageBlockLossPatch
{
    private static readonly ConditionalWeakTable<Creature, object> PreventedHits = new();

    internal static void Arm(Creature creature) => PreventedHits.GetOrCreateValue(creature);

    private static bool Prefix(Creature __instance, decimal amount, ValueProp props, ref decimal __result)
    {
        // DamageBlockInternal is the immediate next native operation after the
        // awaited BeforeDamageReceived hook. This reservation belongs to one hit.
        if (!PreventedHits.Remove(__instance))
        {
            return true;
        }

        // Report the normal absorbed damage without subtracting block, so only
        // damage exceeding block reaches HP and no false block-break is emitted.
        __result = props.HasFlag(ValueProp.Unblockable) ? 0m : Math.Min(__instance.Block, amount);
        return false;
    }
}
