using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Tracking;

internal static class TirednessCostState
{
    private sealed class Counter
    {
        public int Amount;
    }

    // Combat-owned state, not a removable power; never persisted into the permanent deck.
    private static readonly ConditionalWeakTable<PlayerCombatState, Counter> Counters = new();

    internal static void Increase(Player player)
    {
        if (player.PlayerCombatState is not { } state)
        {
            return;
        }

        Counters.GetOrCreateValue(state).Amount++;
        Refresh(player);
    }

    internal static void Refresh(Player player)
    {
        if (player.PlayerCombatState is not { } state)
        {
            return;
        }

        int cost = Counters.GetOrCreateValue(state).Amount;
        foreach (TirednessCard card in state.AllCards.OfType<TirednessCard>())
        {
            card.EnergyCost.SetCustomBaseCost(cost);
        }
    }
}
