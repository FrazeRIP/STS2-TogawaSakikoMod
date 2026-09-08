using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
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
                _ => "res://images/ui/run_history/neow_outline.png"
            };
        }
    }
}

[HarmonyPatch]
internal static class OceanOfMemoriesScenePathPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.PropertyGetter(typeof(EventModel), "BackgroundScenePath")
        ?? throw new MissingMethodException(typeof(EventModel).FullName, "get_BackgroundScenePath");

    private static void Postfix(EventModel __instance, ref string __result)
    {
        if (__instance is OceanOfMemories)
        {
            __result = OceanOfMemories.NativeBackgroundScenePath;
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
            __result = "neow";
        }
    }
}

// Reuse Neow's populated dialogue keys; populating them with the saved custom event ID
// would ask the game for dialogue that does not exist. Fixed reward generation stays on the custom model.
[HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.DialogueSet), MethodType.Getter)]
internal static class OceanOfMemoriesNativeDialoguePatch
{
    private static bool Prefix(AncientEventModel __instance, ref AncientDialogueSet __result)
    {
        if (__instance is not OceanOfMemories)
        {
            return true;
        }

        __result = ModelDb.Event<Neow>().DialogueSet;
        // The dedicated Sakiko room starts with her first-visit conversation even on a fresh profile.
        __result = new AncientDialogueSet
        {
            FirstVisitEverDialogue = null,
            CharacterDialogues = __result.CharacterDialogues,
            AgnosticDialogues = __result.AgnosticDialogues
        };
        return false;
    }
}

[HarmonyPatch(typeof(Neow), "DefineDialogues")]
internal static class SakikoNeowDialoguePatch
{
    private static void Postfix(AncientDialogueSet __result)
    {
        // VisitIndex counts previous visits. The second and third conversations join the native repeat pool.
        __result.CharacterDialogues[ModelDb.Character<SakikoCharacter>().Id.Entry] =
        [
            new AncientDialogue("", "event:/sfx/npcs/neow/neow_curious", "event:/sfx/npcs/neow/neow_welcome")
                { VisitIndex = 0 },
            new AncientDialogue("", "event:/sfx/npcs/neow/neow_welcome", "", "event:/sfx/npcs/neow/neow_sleepy")
                { VisitIndex = 1 },
            new AncientDialogue("", "event:/sfx/npcs/neow/neow_curious", "event:/sfx/npcs/neow/neow_welcome")
                { VisitIndex = 2, IsRepeating = true }
        ];
    }
}

[HarmonyPatch]
internal static class OceanOfMemoriesNativeTextPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(EventModel), "L10NLookup", [typeof(string)])
        ?? throw new MissingMethodException(typeof(EventModel).FullName, "L10NLookup");

    private static void Postfix(EventModel __instance, string entryName, ref LocString __result)
    {
        if (__instance is OceanOfMemories && entryName is
            OceanOfMemories.Entry + ".title" or
            OceanOfMemories.Entry + ".epithet" or
            OceanOfMemories.Entry + ".pages.DONE.description")
        {
            __result = new LocString("ancients", "NEOW" + entryName[OceanOfMemories.Entry.Length..]);
        }
    }
}
