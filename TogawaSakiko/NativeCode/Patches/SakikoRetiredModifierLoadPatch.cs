using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Patches;

/// <summary>
/// Runs saved before the Kings rework carry a hidden Kings reward modifier. Its model no longer exists,
/// so drop it before the save is deserialized instead of letting it load as a visible DeprecatedModifier.
/// </summary>
[HarmonyPatch]
internal static class SakikoRetiredModifierLoadPatch
{
    internal const string RetiredKingsCarrierEntry = NativeStableIds.EntryPrefix + "KINGS_REWARD_CARRIER_MODIFIER";

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))
            ?? throw new MissingMethodException(typeof(RunSaveManager).FullName, nameof(RunSaveManager.LoadRunSave));
        yield return AccessTools.Method(typeof(RunSaveManager), nameof(RunSaveManager.LoadMultiplayerRunSave))
            ?? throw new MissingMethodException(typeof(RunSaveManager).FullName, nameof(RunSaveManager.LoadMultiplayerRunSave));
    }

    private static void Postfix(ReadSaveResult<SerializableRun> __result)
    {
        RemoveRetiredModifiers(__result.SaveData);
    }

    internal static int RemoveRetiredModifiers(SerializableRun? save)
    {
        return save?.Modifiers.RemoveAll(modifier => modifier.Id?.Entry == RetiredKingsCarrierEntry) ?? 0;
    }
}
