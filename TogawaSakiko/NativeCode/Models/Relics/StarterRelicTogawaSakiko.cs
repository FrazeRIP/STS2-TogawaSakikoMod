using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class StarterRelicTogawaSakiko : RelicModel
{
    private MegaCrit.Sts2.Core.Combat.CombatState? _rewardedCombat;
    private bool _kingsRewardPending;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath => NativeAssetPaths.MonochromeHairbandIcon;

    protected override string PackedIconOutlinePath => NativeAssetPaths.MonochromeHairbandOutline;

    protected override string BigIconPath => NativeAssetPaths.MonochromeHairbandBigIcon;

    [SavedProperty]
    public bool KingsRewardPending
    {
        get => _kingsRewardPending;
        private set
        {
            AssertMutable();
            _kingsRewardPending = value;
        }
    }

    public override Task BeforeCardRemoved(CardModel card)
    {
        N3ContractDiagnostics.RecordBeforeCardRemoved(card);
        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        KingsLifecycleDiagnostics.RecordAfterCombatVictory(Owner);
        if (Owner.Creature.IsDead ||
            ReferenceEquals(_rewardedCombat, room.CombatState) ||
            !IsEligibleVictory(room.Act.Id.Entry, room.Encounter.Id.Entry))
        {
            return;
        }

        _rewardedCombat = room.CombatState;
        Flash();
        CardPileAddResult result = await PersistentDeckMutation.AddCanonicalAsync<DesireCard>(Owner);
        CardCmd.PreviewCardPileAdd(result, 2f);
        NativeSmokeTrace.Info($"Monochrome Hairband added Desire to the deck; success={result.success}.");
    }

    public override Task BeforeCombatStart()
    {
        KingsRewardState.Clear(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Creature.HasPower<KingsPower>())
        {
            KingsRewardState.MarkPending(Owner);
        }

        KingsLifecycleDiagnostics.RecordAfterCombatEnd(Owner);

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
        if (reward is CardReward && ReferenceEquals(player, Owner))
        {
            KingsRewardState.Clear(player);
            KingsLifecycleDiagnostics.RecordAfterRewardTaken(player, reward);
        }

        return Task.CompletedTask;
    }

    internal void SetKingsRewardPending(bool pending)
    {
        KingsRewardPending = pending;
    }

    internal static bool IsEligibleVictory(string actEntry, string encounterEntry)
    {
        return !IsOutOfScopeRoute(actEntry) && !IsOutOfScopeRoute(encounterEntry);
    }

    private static bool IsOutOfScopeRoute(string entry)
    {
        return entry.StartsWith(NativeStableIds.EntryPrefix, StringComparison.Ordinal);
    }
}
