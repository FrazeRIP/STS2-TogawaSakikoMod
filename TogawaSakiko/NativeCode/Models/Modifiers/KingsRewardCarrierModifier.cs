using System.Globalization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Models.Modifiers;

public sealed class KingsRewardCarrierModifier : ModifierModel
{
    private string _pendingPlayerIds = string.Empty;

    public override LocString Title =>
        new("powers", NativeStableIds.EntryPrefix + "KINGS_POWER.title");

    public override LocString Description =>
        new("powers", NativeStableIds.EntryPrefix + "KINGS_POWER.description");

    protected override string IconPath => NativeAssetPaths.KingsIcon;

    [SavedProperty]
    public string PendingPlayerIds
    {
        get => _pendingPlayerIds;
        private set
        {
            AssertMutable();
            _pendingPlayerIds = value ?? string.Empty;
        }
    }

    public override Task BeforeCombatStart()
    {
        foreach (Player player in RunState.Players)
        {
            KingsRewardState.Clear(player);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        foreach (Player player in room.CombatState.Players)
        {
            if (player.Creature.HasPower<KingsPower>())
            {
                KingsRewardState.MarkPending(player);
            }

            KingsLifecycleDiagnostics.RecordAfterCombatEnd(player);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        foreach (Player player in room.CombatState.Players)
        {
            KingsLifecycleDiagnostics.RecordAfterCombatVictory(player);
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        bool modified = KingsRewardState.TryReduceCardRewardOptions(
            this,
            player,
            cardRewardOptions,
            creationOptions);
        KingsLifecycleDiagnostics.RecordRewardModification(
            this,
            player,
            cardRewardOptions,
            creationOptions,
            modified);
        return modified;
    }

    public override Task AfterRewardTaken(Player player, Reward reward)
    {
        if (reward is CardReward)
        {
            KingsRewardState.Clear(player);
            KingsLifecycleDiagnostics.RecordAfterRewardTaken(player, reward);
        }

        return Task.CompletedTask;
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        ImportLegacyPendingState(runState.Players);
    }

    internal bool IsPending(ulong playerId)
    {
        return ParsePendingPlayerIds(PendingPlayerIds).Contains(playerId);
    }

    internal void SetPending(ulong playerId, bool pending)
    {
        SortedSet<ulong> playerIds = ParsePendingPlayerIds(PendingPlayerIds);
        if (pending)
        {
            playerIds.Add(playerId);
        }
        else
        {
            playerIds.Remove(playerId);
        }

        PendingPlayerIds = string.Join(",", playerIds.Select(id =>
            id.ToString(CultureInfo.InvariantCulture)));
    }

    internal void ImportLegacyPendingState(IEnumerable<Player> players)
    {
        foreach (Player player in players)
        {
            if (KingsRewardState.HasLegacyPendingCarrier(player))
            {
                SetPending(player.NetId, true);
            }
        }
    }

    internal static SortedSet<ulong> ParsePendingPlayerIds(string? serializedPlayerIds)
    {
        SortedSet<ulong> playerIds = [];
        if (string.IsNullOrWhiteSpace(serializedPlayerIds))
        {
            return playerIds;
        }

        foreach (string token in serializedPlayerIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (ulong.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out ulong playerId))
            {
                playerIds.Add(playerId);
            }
        }

        return playerIds;
    }
}
