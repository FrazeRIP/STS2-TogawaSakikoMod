using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class WarmthInfusedPorcelainCup : SakikoRelicModel
{
    internal const float TriggerThreshold = 0.5f;

    protected override string AssetStem => "warmthinfusedporcelaincup";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override Task AfterObtained()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        bool alreadyContainsHeartsBarrier = cardRewardOptions.Any(option => option.Card is HeartsBarrierCard);
        if (!ShouldForceOption(
                ReferenceEquals(player, Owner),
                cardRewardOptions.Count,
                alreadyContainsHeartsBarrier,
                (creationOptions.RngOverride ?? player.PlayerRng.Rewards).NextFloat()))
        {
            return false;
        }

        CardModel forced = player.RunState.CreateCard(ModelDb.Card<HeartsBarrierCard>(), player);
        cardRewardOptions[0].ModifyCard(forced, this);
        return true;
    }

    internal static bool ShouldForceOption(
        bool ownerMatches,
        int optionCount,
        bool alreadyContainsHeartsBarrier,
        float roll)
    {
        return ownerMatches &&
               optionCount > 0 &&
               !alreadyContainsHeartsBarrier &&
               roll >= TriggerThreshold;
    }
}
