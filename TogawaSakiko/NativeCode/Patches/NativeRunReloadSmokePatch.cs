using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadRunSave))]
internal static class NativeRunReloadSmokePatch
{
    private static int _validationStarted;

    private static void Postfix(ReadSaveResult<SerializableRun> __result)
    {
        if (!NativeSmokeTrace.ReloadEnabled ||
            Interlocked.CompareExchange(ref _validationStarted, 1, 0) != 0)
        {
            return;
        }

        if (!__result.Success || __result.SaveData is null)
        {
            throw new InvalidOperationException(
                $"The native reload smoke could not read the run save. Status={__result.Status}, Error={__result.ErrorMessage}");
        }

        RunState runState = RunState.FromSerializable(__result.SaveData);
        var player = runState.Players.SingleOrDefault(candidate => candidate.Character is SakikoCharacter)
            ?? throw new InvalidOperationException("The reloaded run does not contain Togawa Sakiko.");

        if (!player.Relics.Any(relic => relic is StarterRelicTogawaSakiko))
        {
            throw new InvalidOperationException("The reloaded run does not contain Monochrome Hairband.");
        }

        int desireCount = player.Deck.Cards.Count(card => card is DesireCard);
        if (desireCount == 0)
        {
            throw new InvalidOperationException("The reloaded run does not contain the generated Desire card.");
        }

        NativeSmokeTrace.ReloadInfo(
            $"deserialized Togawa Sakiko with Monochrome Hairband and {desireCount} Desire card(s).");
    }
}
