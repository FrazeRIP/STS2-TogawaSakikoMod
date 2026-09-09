using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using TogawaSakiko.NativeCode.Bootstrap;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(ModManager), nameof(ModManager.GetGameplayRelevantModNameList))]
internal static class NativePackageHandshakePatch
{
    private static void Postfix(ref List<string>? __result)
    {
        if (__result is null || !ModManager.GetLoadedMods().Any(mod => mod.manifest?.id == ModEntryPoint.ModId))
        {
            return;
        }

        // Native connection setup compares this list before entering a lobby or load lobby.
        // Keep native version/model checks and error UI; native replay metadata is separate.
        // On the recorded v0.111.0 build, PeerVersionInfo and HandshakeManager perform
        // this check before JoinFlow receives the initial lobby or reconnect message.
        __result.Add(NativePackageIdentity.HandshakeEntry);
    }
}
