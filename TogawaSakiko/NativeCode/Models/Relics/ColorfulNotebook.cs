using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
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
        // Room hooks have no incoming context. Native hook contexts defer a downstream choice
        // into the owner's synchronized action queue while room entry is allowed to finish.
        var choiceContext = new HookPlayerChoiceContext(
            Owner, LocalContext.NetId ?? Owner.NetId, GameActionType.CombatPlayPhaseOnly);
        choiceContext.PushModel(this);
        Task application = PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            null);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(application);
    }
}
