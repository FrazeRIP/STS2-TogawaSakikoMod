using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(
    typeof(CreatureCmd),
    nameof(CreatureCmd.LoseBlock),
    [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(Creature)])]
internal static class HypeExplicitBlockLossPatch
{
    private static bool Prefix(Creature target, decimal amount, ref Task __result)
    {
        HypePower? hype = target.GetPower<HypePower>();
        if (!HypeBlockLossPolicy.CanPreventExplicitLoss(
                CombatManager.Instance.IsInProgress,
                CombatManager.Instance.IsEnding,
                target.IsDead,
                target.Block,
                amount,
                hype?.Amount ?? 0))
        {
            return true;
        }

        __result = hype!.ConsumeOneAsync();
        return false;
    }
}

internal static class HypeBlockLossPolicy
{
    public static bool CanPreventExplicitLoss(
        bool combatInProgress,
        bool combatEnding,
        bool targetDead,
        int block,
        decimal amount,
        int hypeAmount)
    {
        return combatInProgress &&
               !combatEnding &&
               !targetDead &&
               block > 0 &&
               amount > 0m &&
               hypeAmount > 0;
    }
}
