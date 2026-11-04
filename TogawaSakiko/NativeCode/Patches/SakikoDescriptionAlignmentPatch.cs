using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TogawaSakiko.NativeCode.Models.Pools;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
internal static class SakikoDescriptionAlignmentPatch
{
    private static void Prefix(NCard __instance, MegaRichTextLabel ____descriptionLabel)
    {
        // Center every paragraph, including text following explicit line breaks.
        // Apply before native text layout and automatic font sizing.
        if (__instance.Model?.Pool is TogawaSakikoCardPool && ____descriptionLabel is not null)
        {
            ____descriptionLabel.HorizontalAlignment = HorizontalAlignment.Center;
        }
    }
}
