using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class MasqueradeMask : SakikoRelicModel
{
    internal const int TriggerAmount = 3;

    private int _purgedCards;

    protected override string AssetStem => "masquerademask";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => PurgedCards;

    [SavedProperty]
    public int PurgedCards
    {
        get => _purgedCards;
        set
        {
            AssertMutable();
            _purgedCards = Math.Max(0, value);
            Status = _purgedCards == TriggerAmount - 1 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    internal int OnCardPurged(CombatRoom room)
    {
        if (!ReferenceEquals(room, Owner.RunState.CurrentRoom))
        {
            return 0;
        }

        (int remaining, int rewardsAdded) = AdvanceCounter(PurgedCards, 1);
        PurgedCards = remaining;
        for (int index = 0; index < rewardsAdded; index++)
        {
            room.AddExtraReward(Owner, new CardRemovalReward(Owner));
            Flash();
        }

        return rewardsAdded;
    }

    internal static (int Remaining, int RewardsAdded) AdvanceCounter(int current, int cardsPurged)
    {
        int total = Math.Max(0, current) + Math.Max(0, cardsPurged);
        return (total % TriggerAmount, total / TriggerAmount);
    }
}
