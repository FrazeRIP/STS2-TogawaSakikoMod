using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    // A Split Moment still forwards these turn hooks. No N5 scenario observes them since
    // Symbol III: Water stopped granting an extra turn.
    public static Task ObserveAfterTakingExtraTurnAsync(Player player, CardModel observer)
    {
        return Task.CompletedTask;
    }

    public static Task ObserveAfterPlayerTurnStartAsync(Player player, CardModel observer)
    {
        return Task.CompletedTask;
    }

    private static async Task<int> RemoveAndRememberPowerAsync<TPower>(Creature creature)
        where TPower : PowerModel
    {
        TPower? power = creature.GetPower<TPower>();
        int amount = power?.Amount ?? 0;
        await PowerCmd.Remove(power);
        return amount;
    }

    private static async Task RestoreRememberedPowerAsync<TPower>(
        PlayerChoiceContext choiceContext,
        Creature creature,
        int amount)
        where TPower : PowerModel
    {
        if (amount != 0)
        {
            await PowerCmd.Apply<TPower>(choiceContext, creature, amount, creature, null);
        }
    }
}
