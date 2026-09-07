using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;
using TogawaSakiko.NativeCode.Models.Modifiers;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(
    typeof(RunState),
    nameof(RunState.CreateForNewRun),
    [
        typeof(IReadOnlyList<Player>),
        typeof(IReadOnlyList<ActModel>),
        typeof(IReadOnlyList<ModifierModel>),
        typeof(GameMode),
        typeof(int),
        typeof(string)
    ])]
internal static class KingsRewardCarrierNewRunPatch
{
    private static void Prefix(
        IReadOnlyList<Player> players,
        ref IReadOnlyList<ModifierModel> modifiers)
    {
        modifiers = EnsureCarrier(players, modifiers);
    }

    internal static IReadOnlyList<ModifierModel> EnsureCarrier(
        IReadOnlyList<Player> players,
        IReadOnlyList<ModifierModel> modifiers)
    {
        if (!players.Any(player => player.Character is SakikoCharacter) ||
            modifiers.Any(modifier => modifier is KingsRewardCarrierModifier))
        {
            return modifiers;
        }

        return modifiers
            .Append(ModelDb.Modifier<KingsRewardCarrierModifier>().ToMutable())
            .ToArray();
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class KingsRewardCarrierLoadPatch
{
    private static void Postfix(RunState __result)
    {
        if (!__result.Players.Any(player => player.Character is SakikoCharacter))
        {
            return;
        }

        KingsRewardCarrierModifier? carrier =
            __result.Modifiers.OfType<KingsRewardCarrierModifier>().FirstOrDefault();
        if (carrier is null)
        {
            carrier = (KingsRewardCarrierModifier)
                ModelDb.Modifier<KingsRewardCarrierModifier>().ToMutable();
            __result.AddModifierDebug(carrier);
        }

        carrier.ImportLegacyPendingState(__result.Players);
    }
}

[HarmonyPatch(typeof(NTopBarModifier), nameof(NTopBarModifier.Create))]
internal static class KingsRewardCarrierTopBarItemPatch
{
    private static bool Prefix(ModifierModel modifier, ref NTopBarModifier? __result)
    {
        if (modifier is not KingsRewardCarrierModifier)
        {
            return true;
        }

        __result = null;
        return false;
    }
}

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
internal static class KingsRewardCarrierTopBarContainerPatch
{
    private static readonly System.Reflection.FieldInfo ModifiersContainerField =
        AccessTools.Field(typeof(NTopBar), "_modifiersContainer");

    private static void Postfix(NTopBar __instance, IRunState runState)
    {
        if (ModifiersContainerField.GetValue(__instance) is Control modifiersContainer)
        {
            modifiersContainer.Visible = runState.Modifiers.Any(
                modifier => modifier is not KingsRewardCarrierModifier);
        }
    }
}
