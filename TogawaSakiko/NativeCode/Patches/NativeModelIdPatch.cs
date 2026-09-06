using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry), [typeof(Type)])]
internal static class NativeModelIdPatch
{
    private static void Postfix(Type type, ref string __result)
    {
        if (NativeStableIds.IsOwnedModel(type) && !__result.StartsWith(NativeStableIds.EntryPrefix, StringComparison.Ordinal))
        {
            __result = NativeStableIds.EntryPrefix + __result;
        }
    }
}
