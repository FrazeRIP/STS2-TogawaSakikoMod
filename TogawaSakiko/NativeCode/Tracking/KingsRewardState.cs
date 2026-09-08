using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Modifiers;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Tracking;

internal static class KingsRewardState
{
    public static bool MarkPending(Player player)
    {
        bool foundCarrier = false;
        foreach (AbstractModel carrier in GetCarriers(player))
        {
            SetPending(carrier, player, true);
            foundCarrier = true;
        }

        return foundCarrier;
    }

    public static void Clear(Player player)
    {
        foreach (AbstractModel carrier in GetCarriers(player))
        {
            SetPending(carrier, player, false);
        }
    }

    public static bool IsPending(Player player)
    {
        return GetCarriers(player).Any(carrier => IsPending(carrier, player));
    }

    public static bool TryReduceCardRewardOptions(
        AbstractModel carrier,
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        if (!IsCombatCardReward(creationOptions) ||
            !IsCarrierForPlayer(carrier, player) ||
            !IsPending(carrier, player) ||
            !ReferenceEquals(GetPrimaryPendingCarrier(player), carrier))
        {
            return false;
        }

        return RemoveOneOption(cardRewardOptions);
    }

    internal static bool IsCombatCardReward(CardCreationOptions creationOptions)
    {
        return creationOptions.Source == CardCreationSource.Encounter &&
               creationOptions.Flags.HasFlag(CardCreationFlags.IsCardReward);
    }

    internal static bool RemoveOneOption<T>(IList<T> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
        {
            return false;
        }

        options.RemoveAt(options.Count - 1);
        return true;
    }

    private static IEnumerable<AbstractModel> GetCarriers(Player player)
    {
        foreach (KingsCard card in player.Deck.Cards.OfType<KingsCard>())
        {
            yield return card;
        }

        foreach (StarterRelicTogawaSakiko relic in player.Relics.OfType<StarterRelicTogawaSakiko>())
        {
            if (!relic.IsMelted)
            {
                yield return relic;
            }
        }

        foreach (KingsRewardCarrierModifier modifier in
                 player.RunState.Modifiers.OfType<KingsRewardCarrierModifier>())
        {
            yield return modifier;
        }
    }

    private static AbstractModel? GetPrimaryPendingCarrier(Player player)
    {
        return GetCarriers(player).FirstOrDefault(carrier => IsPending(carrier, player));
    }

    private static bool IsPending(AbstractModel carrier, Player player)
    {
        return carrier switch
        {
            KingsCard card => card.KingsRewardPending,
            StarterRelicTogawaSakiko relic => relic.KingsRewardPending,
            KingsRewardCarrierModifier modifier => modifier.IsPending(player.NetId),
            _ => false
        };
    }

    private static void SetPending(AbstractModel carrier, Player player, bool pending)
    {
        switch (carrier)
        {
            case KingsCard card:
                card.SetKingsRewardPending(pending);
                break;
            case StarterRelicTogawaSakiko relic:
                relic.SetKingsRewardPending(pending);
                break;
            case KingsRewardCarrierModifier modifier:
                modifier.SetPending(player.NetId, pending);
                break;
        }
    }

    internal static bool IsCarrierForPlayer(AbstractModel carrier, Player player)
    {
        return carrier switch
        {
            KingsCard card => ReferenceEquals(card.Owner, player),
            StarterRelicTogawaSakiko relic => ReferenceEquals(relic.Owner, player),
            KingsRewardCarrierModifier modifier =>
                player.RunState.Modifiers.Any(candidate => ReferenceEquals(candidate, modifier)),
            _ => false
        };
    }

    internal static bool HasLegacyPendingCarrier(Player player)
    {
        return player.Deck.Cards.OfType<KingsCard>().Any(card => card.KingsRewardPending) ||
               player.Relics.OfType<StarterRelicTogawaSakiko>()
                   .Any(relic => !relic.IsMelted && relic.KingsRewardPending);
    }
}
