using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class TheCompass : SakikoRelicModel
{
    protected override string AssetStem => "thecompass";

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            Owner.Creature,
            2m,
            Owner.Creature,
            null);
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<DazzlingPower>(
                choiceContext,
                enemy,
                1m,
                Owner.Creature,
                null);
        }
    }
}
