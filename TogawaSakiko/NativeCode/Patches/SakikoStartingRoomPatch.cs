using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Events;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch]
internal static class SakikoStartingRoomPatch
{
    private static readonly PropertyInfo RunStateProperty = AccessTools.Property(typeof(RunManager), "State")
        ?? throw new MissingMemberException(typeof(RunManager).FullName, "State");

    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RunManager), "CreateRoom", [typeof(RoomType), typeof(MapPointType), typeof(AbstractModel)])
        ?? throw new MissingMethodException(typeof(RunManager).FullName, "CreateRoom");

    private static void Postfix(RunManager __instance, MapPointType mapPointType, AbstractModel? model, ref AbstractRoom __result)
    {
        if (model is null && mapPointType == MapPointType.Ancient &&
            __result is EventRoom { CanonicalEvent: Neow neow } && neow.GetType() == typeof(Neow) &&
            RunStateProperty.GetValue(__instance) is IRunState state && ShouldReplace(state))
        {
            __result = new EventRoom(ModelDb.Event<OceanOfMemories>());
        }
    }

    internal static bool ShouldReplace(IRunState runState) =>
        runState.GameMode == GameMode.Standard &&
        runState.CurrentActIndex == 0 &&
        runState.ExtraFields.StartedWithNeow &&
        runState.Players.Count == 1 &&
        runState.Players[0].Character is SakikoCharacter &&
        KingsRewardCarrierNeowPatch.VisibleModifiers(runState.Modifiers).Count == 0 &&
        runState.CurrentMapCoord == runState.Map.StartingMapPoint.coord;
}

[HarmonyPatch]
internal static class OceanOfMemoriesAncientIconPathPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (string property in new[] { "MapIconPath", "MapIconOutlinePath", "RunHistoryIconOutlinePath" })
        {
            yield return AccessTools.PropertyGetter(typeof(AncientEventModel), property)
                ?? throw new MissingMethodException(typeof(AncientEventModel).FullName, "get_" + property);
        }
    }

    private static void Postfix(AncientEventModel __instance, MethodBase __originalMethod, ref string __result)
    {
        if (__instance is OceanOfMemories)
        {
            __result = __originalMethod.Name switch
            {
                "get_MapIconPath" => "res://images/packed/map/ancients/ancient_node_neow.png",
                "get_MapIconOutlinePath" => "res://images/packed/map/ancients/ancient_node_neow_outline.png",
                _ => "res://images/ui/run_history/event_outline.png"
            };
        }
    }
}

[HarmonyPatch]
internal static class OceanOfMemoriesScenePathPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.PropertyGetter(typeof(EventModel), "LayoutScenePath")
        ?? throw new MissingMethodException(typeof(EventModel).FullName, "get_LayoutScenePath");

    private static void Postfix(EventModel __instance, ref string __result)
    {
        if (__instance is OceanOfMemories)
        {
            __result = OceanOfMemories.ScenePath;
        }
    }
}

[HarmonyPatch]
internal static class OceanOfMemoriesHistoryIconPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(MegaCrit.Sts2.Core.Helpers.ImageHelper), "GetRoomIconSuffix")
        ?? throw new MissingMethodException(typeof(MegaCrit.Sts2.Core.Helpers.ImageHelper).FullName, "GetRoomIconSuffix");

    private static void Postfix(ModelId? modelId, ref string? __result)
    {
        if (modelId?.Entry == OceanOfMemories.Entry)
        {
            __result = "event";
        }
    }
}
