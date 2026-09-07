using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class ColorfulNotebook : SakikoRelicModel
{
    protected override string AssetStem => "colorfulnotebook";

    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<DazzlingPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            1m,
            Owner.Creature,
            null);
    }
}
