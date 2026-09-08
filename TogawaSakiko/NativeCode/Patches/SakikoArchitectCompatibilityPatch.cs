using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(TheArchitect), "WinRun")]
internal static class SakikoArchitectCompatibilityPatch
{
    private static bool Prefix(TheArchitect __instance, AncientDialogue? ____dialogue, ref Task __result)
    {
        if (__instance.Owner?.Character is not SakikoCharacter)
        {
            return true;
        }

        if (____dialogue is not null)
        {
            return true;
        }

        NativeSmokeTrace.N7Info(
            "used the native WinRun fallback because the base-game Architect event has no custom-character dialogue entry.");
        __result = RunManager.Instance.WinRun();
        return false;
    }
}

[HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
internal static class SakikoArchitectDialoguePatch
{
    private static void Postfix(AncientDialogueSet __result)
    {
        // PopulateLocKeys runs after this method and derives speakers and buttons from ancients.json.
        // Show each conversation on the first three wins, then reuse the native repeating pool.
        __result.CharacterDialogues[NativeStableIds.Entries[typeof(SakikoCharacter)]] =
        [
            new AncientDialogue("", "", "", "") { VisitIndex = 0, EndAttackers = ArchitectAttackers.Both },
            new AncientDialogue("", "", "", "") { VisitIndex = 1, EndAttackers = ArchitectAttackers.Both },
            new AncientDialogue("", "") { VisitIndex = 2, EndAttackers = ArchitectAttackers.Both }
        ];
    }
}
