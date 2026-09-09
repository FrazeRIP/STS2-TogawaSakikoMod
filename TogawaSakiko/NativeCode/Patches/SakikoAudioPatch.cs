using HarmonyLib;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch]
internal static class SakikoAudioLifecyclePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(RunManager), "InitializeShared")
            ?? throw new MissingMethodException(typeof(RunManager).FullName, "InitializeShared");
        yield return AccessTools.Method(typeof(RunManager), nameof(RunManager.CleanUp))
            ?? throw new MissingMethodException(typeof(RunManager).FullName, nameof(RunManager.CleanUp));
        yield return AccessTools.PropertySetter(typeof(LocalContext), nameof(LocalContext.NetId))
            ?? throw new MissingMethodException(typeof(LocalContext).FullName, "set_NetId");
    }

    private static void Prefix() => SakikoAudioCmd.ResetVoiceState();
}

[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)])]
internal static class SakikoResourceAudioPatch
{
    private static bool Prefix(string sfx, float volume)
    {
        if (!SakikoAudioCmd.IsSakikoResourceAudioPath(sfx))
        {
            return true;
        }

        SakikoAudioCmd.TryPlayResourcePath(sfx, volume);
        return false;
    }
}

[HarmonyPatch(
    typeof(CreatureCmd),
    nameof(CreatureCmd.Damage),
    [
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<Creature>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel),
        typeof(CardPlay)
    ])]
internal static class SakikoHurtVoicePatch
{
    private static void Postfix(ref Task<IEnumerable<DamageResult>> __result)
    {
        __result = PlayHurtVoiceAfterDamageAsync(__result);
    }

    private static async Task<IEnumerable<DamageResult>> PlayHurtVoiceAfterDamageAsync(
        Task<IEnumerable<DamageResult>> damageTask)
    {
        List<DamageResult> results = (await damageTask).ToList();
        foreach (DamageResult result in results)
        {
            if (result.UnblockedDamage <= 0 ||
                !LocalContext.IsMe(result.Receiver) ||
                result.Receiver.Player?.Character is not SakikoCharacter)
            {
                continue;
            }

            SakikoAudioCmd.TryPlayHurtVoice(result.Receiver.Player);
            break;
        }

        return results;
    }
}
