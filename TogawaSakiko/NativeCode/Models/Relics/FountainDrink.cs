using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class FountainDrink : SakikoRelicModel
{
    protected override string AssetStem => "fountaindrink";

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldForcePotionReward(Player player, RoomType roomType)
    {
        return ShouldForceFor(
            ReferenceEquals(player, Owner),
            roomType,
            player.HasOpenPotionSlots);
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (!Owner.Creature.IsDead && !Owner.HasOpenPotionSlots)
        {
            Flash();
        }

        return Task.CompletedTask;
    }

    internal static bool ShouldForceFor(bool ownerMatches, RoomType roomType, bool hasOpenPotionSlots)
    {
        return ownerMatches &&
               roomType is RoomType.Monster or RoomType.Elite or RoomType.Boss &&
               !hasOpenPotionSlots;
    }
}
