using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class TheDoll : SakikoRelicModel
{
    private int _turnsElapsed;

    protected override string AssetStem => "thedoll";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, TurnsElapsed);

    [SavedProperty]
    public int TurnsElapsed
    {
        get => _turnsElapsed;
        set
        {
            AssertMutable();
            _turnsElapsed = value;
            Status = value < 0 ? RelicStatus.Disabled : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeCombatStart()
    {
        TurnsElapsed = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(player, Owner) || TurnsElapsed < 0)
        {
            return;
        }

        TurnsElapsed++;
        if (TurnsElapsed != 2)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<HypePower>(
            choiceContext,
            Owner.Creature,
            2m,
            Owner.Creature,
            null);
        TurnsElapsed = -1;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        TurnsElapsed = -1;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
