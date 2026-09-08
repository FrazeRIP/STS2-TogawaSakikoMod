using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch]
internal static class OceanOfMemoriesOptionPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(NEventRoom), nameof(NEventRoom.OptionButtonClicked), [typeof(EventOption), typeof(int)])
        ?? throw new MissingMethodException(typeof(NEventRoom).FullName, nameof(NEventRoom.OptionButtonClicked));

    private static bool Prefix(NEventRoom __instance, EventOption option, int index)
    {
        if (__instance.CustomEventNode is not OceanOfMemoriesScene ocean)
        {
            return true;
        }

        ocean.OnOptionClicked(option, index);
        return false;
    }
}
