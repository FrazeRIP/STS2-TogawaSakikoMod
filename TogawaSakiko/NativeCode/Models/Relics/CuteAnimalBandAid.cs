using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class CuteAnimalBandAid : SakikoRelicModel
{
    protected override string AssetStem => "cuteanimalbandaid";

    public override RelicRarity Rarity => RelicRarity.Common;

    public override Task AfterObtained()
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        if (!ReferenceEquals(player, Owner) ||
            player.Creature.IsDead ||
            player.RunState.CurrentRoom is not CombatRoom { IsPreFinished: true })
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(player.Creature, 1m);
    }
}
