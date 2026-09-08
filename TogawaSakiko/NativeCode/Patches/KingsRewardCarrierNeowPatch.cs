using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Modifiers;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch]
internal static class KingsRewardCarrierNeowPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(Neow), nameof(Neow.InitialDescription))
            ?? throw new MissingMethodException(typeof(Neow).FullName, "get_InitialDescription");
        yield return AccessTools.Method(typeof(Neow), "GenerateInitialOptions")
            ?? throw new MissingMethodException(typeof(Neow).FullName, "GenerateInitialOptions");
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original)
    {
        MethodInfo modifiersGetter = AccessTools.PropertyGetter(typeof(IRunState), nameof(IRunState.Modifiers));
        MethodInfo filter = AccessTools.Method(typeof(KingsRewardCarrierNeowPatch), nameof(VisibleModifiers));
        List<CodeInstruction> result = [];
        int replacements = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            result.Add(instruction);
            if (instruction.Calls(modifiersGetter))
            {
                result.Add(new CodeInstruction(OpCodes.Call, filter));
                replacements++;
            }
        }

        int expected = original.Name == "get_InitialDescription" ? 1 : 2;
        if (replacements != expected)
        {
            throw new InvalidOperationException(
                $"Neow Kings carrier compatibility expected {expected} modifier reads in {original.Name}, found {replacements}.");
        }

        return result;
    }

    internal static IReadOnlyList<ModifierModel> VisibleModifiers(IReadOnlyList<ModifierModel> modifiers)
    {
        // The save carrier is infrastructure, not a custom-run modifier with a Neow option.
        return modifiers.Any(modifier => modifier is KingsRewardCarrierModifier)
            ? modifiers.Where(modifier => modifier is not KingsRewardCarrierModifier).ToArray()
            : modifiers;
    }
}
