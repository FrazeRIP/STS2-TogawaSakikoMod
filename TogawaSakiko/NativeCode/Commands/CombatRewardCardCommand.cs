using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Commands;

internal static class CombatRewardCardCommand
{
    internal static async Task AddAsync<T>(Player owner, bool upgraded) where T : CardModel
    {
        // Resolve after the attack: a lethal final hit must not add a permanent reward.
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        ICombatState combatState = owner.Creature.CombatState
            ?? throw new InvalidOperationException("Combat reward cards require an active combat.");
        if (!combatState.GetOpponentsOf(owner.Creature).Any(enemy => !enemy.IsDead))
        {
            return;
        }

        PersistentDeckAndCombatAddResult addition = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<T>(
            owner,
            upgradeLevel: upgraded ? 1 : 0);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }
}
