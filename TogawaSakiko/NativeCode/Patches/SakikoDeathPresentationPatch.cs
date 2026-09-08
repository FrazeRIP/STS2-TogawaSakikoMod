using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Presentation.Godot;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
internal static class SakikoDeathPresentationPatch
{
    private static void Prefix(NCreature __instance)
    {
        // Native StartDeathAnim only sends Dead to Spine animators; Sakiko uses a static sprite.
        if (__instance.Visuals is SakikoCreatureVisualsNode visuals)
        {
            visuals.SetDeadPortrait(true);
        }
    }
}

[HarmonyPatch(typeof(NCreature), "ImmediatelySetIdle")]
internal static class SakikoRevivePresentationPatch
{
    private static bool Prefix(NCreature __instance)
    {
        if (__instance.Visuals is not SakikoCreatureVisualsNode visuals)
        {
            return true;
        }
        // Keep the native revive fade/UI sequence, but bypass its unconditional Spine access.
        visuals.SetDeadPortrait(false);
        return false;
    }
}

[HarmonyPatch(typeof(NFireBurstVfx), nameof(NFireBurstVfx.Create), [typeof(Creature), typeof(float)])]
internal static class SakikoArchitectDeathPresentationPatch
{
    private static void Postfix(Creature creature)
    {
        // The Architect creates this burst at the hit without changing HP or calling StartDeathAnim.
        if (creature.Player?.RunState.CurrentRoom is EventRoom { CanonicalEvent: TheArchitect } &&
            NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals is SakikoCreatureVisualsNode visuals)
        {
            visuals.SetDeadPortrait(true);
        }
    }
}
