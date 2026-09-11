using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using TogawaSakiko.NativeCode.Models.Events;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(NEventOptionButton), nameof(NEventOptionButton._Ready))]
internal static class SakikoNormalBlessingIconPatch
{
    private static void Postfix(NEventOptionButton __instance)
    {
        if (__instance.Option.TextKey != SakikoStartingRewards.NormalOptionKey)
        {
            return;
        }
        TextureRect icon = __instance.GetNode<TextureRect>("%RelicIcon");
        icon.Texture = ResourceLoader.Load<Texture2D>(SakikoStartingRewards.SpeechIconPath);
        icon.GetNode<TextureRect>("%Outline").Visible = false;
        icon.Visible = true;
    }
}

// Native Neow creates keys from the concrete class name. Resolve those options in Neow's own table.
[HarmonyPatch]
internal static class OceanNativeBlessingLocalizationPatch
{
    private const string NativeOptionPrefix = "OCEAN_OF_MEMORIES.pages.INITIAL.options.";

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(EventModel), nameof(EventModel.GetOptionTitle));
        yield return AccessTools.Method(typeof(EventModel), nameof(EventModel.GetOptionDescription));
    }

    private static bool Prefix(EventModel __instance, string key, MethodBase __originalMethod, ref LocString? __result)
    {
        if (__instance is not OceanOfMemories || !key.StartsWith(NativeOptionPrefix, StringComparison.Ordinal))
        {
            return true;
        }
        string suffix = __originalMethod.Name == nameof(EventModel.GetOptionTitle) ? ".title" : ".description";
        __result = LocString.GetIfExists("ancients", "NEOW.pages.INITIAL.options." + key[NativeOptionPrefix.Length..] + suffix);
        return false;
    }
}

// Filter the collection input before both navigation registration and icon construction.
// The relic remains registered for old saves and keeps all of its runtime effects.
[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
internal static class SakikoTemporarilyHiddenRelicPatch
{
    internal static IEnumerable<RelicModel> VisibleRelics(IEnumerable<RelicModel> relics) =>
        relics.Where(relic => relic is not TheThirdMovement);

    // Filter before the category creates its model cache: ordinary event categories bypass
    // LoadSubcategory, and the small AddRelics helper can be inlined by the native assembly.
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo allRelics = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllRelics));
        MethodInfo filter = AccessTools.Method(typeof(SakikoTemporarilyHiddenRelicPatch), nameof(VisibleRelics));
        int matches = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(allRelics))
            {
                matches++;
                yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, filter);
            }
        }
        if (matches != 1)
        {
            throw new InvalidOperationException($"Relic collection visibility expected one model enumeration, found {matches}.");
        }
    }
}
