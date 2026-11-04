using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private static async Task VerifyFeedbackGameplayAsync(CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        await VerifyWishHumanPerHitAsync(combatState, player, target, choiceContext);
        CardModel[] worldviewPool = CardFactory.FilterForCombat(WorldviewPower.GetEligibleAttacks(player)).ToArray();
        Require(worldviewPool.Length > 0 && worldviewPool.All(card =>
                card.Type == CardType.Attack && card.Pool.Id == player.Character.CardPool.Id &&
                !card.Keywords.Contains(CardKeyword.Unplayable) && card.Rarity != CardRarity.Token &&
                card.CanBeGeneratedInCombat),
            "Worldview replacement pool contains a foreign-character, unplayable, or generated-only card");
        NativeSmokeTrace.N5Info("Feedback gameplay passed. WishHuman=Dazzling2+2+PriorStackDamage2, Worldview=OwnCharacterPoolOnly.");
    }

    private static async Task VerifyWishHumanPerHitAsync(CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        int strength = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int weak = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int dazzling = await RemoveAndRememberPowerAsync<DazzlingPower>(player.Creature);
        int vulnerable = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        Creature[] enemies = combatState.GetOpponentsOf(player.Creature).Where(enemy => enemy.IsHittable).ToArray();
        Dictionary<Creature, int> blocks = enemies.ToDictionary(enemy => enemy, enemy => enemy.Block);
        foreach (Creature enemy in enemies)
        {
            await CreatureCmd.GainBlock(enemy, 100m, ValueProp.Unpowered, null, fast: true);
        }
        await CreatureCmd.LoseBlock(choiceContext, target, target.Block, null);
        await CreatureCmd.Heal(target, target.MaxHp, playAnim: false);
        Require(target.CurrentHp > 10, "Wish Human per-hit test requires an enemy with more than ten HP");
        PowerChangeLedger ledger = PowerChangeLedgerService.GetLedger(combatState);
        int ledgerBefore = ledger.Snapshot(combatState.RoundNumber).Count();
        int vitalityBefore = enemies.Sum(enemy => enemy.CurrentHp + enemy.Block);
        WishToBecomeHumanCard wish = await CreateAndAutoPlayAsync<WishToBecomeHumanCard>(combatState, player, choiceContext, target, upgraded: false);
        decimal[] changes = ledger.Snapshot(combatState.RoundNumber).Skip(ledgerBefore)
            .Where(change => change.PowerModelId == ModelDb.GetId<DazzlingPower>() && change.IsGain && change.Target.PlayerNetId == player.NetId)
            .Select(change => change.Delta).ToArray();
        Require(changes.SequenceEqual([2m, 2m]) && player.Creature.GetPower<DazzlingPower>()?.Amount == 4,
            "Wish Human did not gain two separate two-stack Dazzling amounts");
        Require(vitalityBefore - enemies.Sum(enemy => enemy.CurrentHp + enemy.Block) == 6,
            "Wish Human did not trigger exactly two prior Dazzling stacks on its second gain");
        await PowerCmd.Remove(player.Creature.GetPower<DazzlingPower>());
        await RemoveCombatCardsAsync([wish]);
        await CreatureCmd.Heal(target, target.MaxHp, playAnim: false);
        foreach (Creature enemy in enemies)
        {
            if (enemy.Block > 0)
            {
                await CreatureCmd.LoseBlock(choiceContext, enemy, enemy.Block, null);
            }
            if (blocks[enemy] > 0)
            {
                await CreatureCmd.GainBlock(enemy, blocks[enemy], ValueProp.Unpowered, null, fast: true);
            }
        }
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, strength);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, weak);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, vulnerable);
        await RestoreRememberedPowerAsync<DazzlingPower>(choiceContext, player.Creature, dazzling);
    }
}
