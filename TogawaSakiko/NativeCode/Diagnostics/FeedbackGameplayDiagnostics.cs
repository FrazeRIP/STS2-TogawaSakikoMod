using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Tracking;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N5BatchDiagnostics
{
    private sealed class FireReplaySelector : ICardSelector
    {
        internal List<CardModel> Selected { get; } = [];

        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            CardModel[] offered = options.ToArray();
            CardModel choice = Selected.Count == 0
                ? offered.First(card => card is SymbolIFireCard)
                : offered.First(card => card is SymbolIIAirCard or SymbolIVEarthCard or EtherCard);
            Selected.Add(choice);
            return Task.FromResult<IEnumerable<CardModel>>([choice]);
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives) =>
            new() { card = options.FirstOrDefault()?.Card };
    }

    private static async Task VerifyFeedbackGameplayAsync(CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        await VerifyWishHumanPerHitAsync(combatState, player, target, choiceContext);
        await VerifyKaoFeedbackAsync(combatState, player, target, choiceContext);
        await VerifyAveFireReplayAsync(combatState, player, target, choiceContext);
        CardModel[] worldviewPool = WorldviewPower.GetEligibleAttacks(player).ToArray();
        Require(worldviewPool.Length > 0 && worldviewPool.All(card =>
                card.Type == CardType.Attack && card.Pool.Id == player.Character.CardPool.Id &&
                !card.Keywords.Contains(CardKeyword.Unplayable)),
            "Worldview replacement pool contains a foreign-character or unplayable card");
        NativeSmokeTrace.N5Info("Feedback gameplay passed. WishHuman=Dazzling2+2+PriorStackDamage2, Kao=BuffIntentGlow+GreenDamage9, AveFire=ActiveAveReplay+SecondChoice, Worldview=OwnCharacterPoolOnly.");
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

    private static async Task VerifyKaoFeedbackAsync(CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] moved = await MoveCombatPilesAsideAsync(player, PileType.Hand);
        int strength = await RemoveAndRememberPowerAsync<StrengthPower>(player.Creature);
        int weak = await RemoveAndRememberPowerAsync<WeakPower>(player.Creature);
        int vulnerable = await RemoveAndRememberPowerAsync<VulnerablePower>(target);
        int enemyStrength = await RemoveAndRememberPowerAsync<StrengthPower>(target);
        KaoCard kao = combatState.CreateCard<KaoCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(kao, PileType.Hand, player);
        Dictionary<MoveState, IReadOnlyList<AbstractIntent>> intents = combatState.HittableEnemies
            .Where(enemy => enemy.Monster is not null).Select(enemy => enemy.Monster!.NextMove)
            .Distinct().ToDictionary(move => move, move => move.Intents);
        foreach (MoveState move in intents.Keys)
        {
            AccessTools.Property(typeof(MoveState), nameof(MoveState.Intents)).SetValue(move, Array.Empty<AbstractIntent>());
        }
        Require(!kao.ShouldGlowGold, "Kao glowed without a buff intent");
        AccessTools.Property(typeof(MoveState), nameof(MoveState.Intents)).SetValue(target.Monster!.NextMove, new AbstractIntent[] { new BuffIntent() });
        Require(kao.ShouldGlowGold, "Kao did not glow for an enemy buff intent");
        foreach ((MoveState move, IReadOnlyList<AbstractIntent> previous) in intents)
        {
            AccessTools.Property(typeof(MoveState), nameof(MoveState.Intents)).SetValue(move, previous);
        }
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, 1m, target, null);
        kao.DynamicVars.Damage.UpdateCardPreview(kao, CardPreviewMode.Normal, target, runGlobalHooks: true);
        Require(kao.DynamicVars.Damage.BaseValue == 5m && kao.DynamicVars.Damage.PreviewValue == 9m,
            "Kao did not preserve base five while previewing modified damage nine");
        await RemoveCombatCardsAsync([kao]);
        await PowerCmd.Remove(target.GetPower<StrengthPower>());
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, target, enemyStrength);
        await RestoreRememberedPowerAsync<StrengthPower>(choiceContext, player.Creature, strength);
        await RestoreRememberedPowerAsync<WeakPower>(choiceContext, player.Creature, weak);
        await RestoreRememberedPowerAsync<VulnerablePower>(choiceContext, target, vulnerable);
        await RestoreCombatCardsAsync(moved);
    }

    private static async Task VerifyAveFireReplayAsync(CombatState combatState, Player player, Creature target, PlayerChoiceContext choiceContext)
    {
        CombatCardLocation[] moved = await MoveCombatPilesAsideAsync(player, PileType.Hand, PileType.Draw, PileType.Discard);
        HashSet<CardModel> deckBefore = player.Deck.Cards.ToHashSet<CardModel>(ReferenceEqualityComparer.Instance);
        HashSet<CardModel> combatBefore = player.PlayerCombatState!.AllCards.ToHashSet<CardModel>(ReferenceEqualityComparer.Instance);
        Dictionary<Creature, int> blocks = combatState.HittableEnemies.ToDictionary(enemy => enemy, enemy => enemy.Block);
        foreach (Creature enemy in blocks.Keys)
        {
            await CreatureCmd.GainBlock(enemy, 2000m, ValueProp.Unpowered, null, fast: true);
        }
        Rng rng = player.RunState.Rng.CombatCardGeneration;
        var rngBefore = rng.ToSerializable();
        ulong seed = 0;
        while (!AveMujicaCard.SelectOptionCanonicals(new Rng(seed)).Any(card => card is SymbolIFireCard))
        {
            seed++;
            Require(seed < 1000, "Ave Fire diagnostic could not find a deterministic Fire offer");
        }
        rng.LoadFromSerializable(new Rng(seed).ToSerializable());
        int historyBefore = CombatManager.Instance.History.CardPlaysStarted.Count();
        FireReplaySelector selector = new();
        using (CardSelectCmd.PushSelector(selector))
        {
            await CreateAndAutoPlayAsync<AveMujicaCard>(combatState, player, choiceContext, target, upgraded: false);
        }
        var newPlays = CombatManager.Instance.History.CardPlaysStarted.Skip(historyBefore).ToArray();
        Require(selector.Selected.Count == 2 && selector.Selected[0] is SymbolIFireCard &&
                newPlays.Count(entry => entry.CardPlay.Card is AveMujicaCard) == 2,
            "choosing Fire from Ave Mujica did not replay the actively resolving Ave and offer a second choice");
        foreach (CardModel added in player.Deck.Cards.Where(card => !deckBefore.Contains(card)).ToArray())
        {
            Require((await PersistentDeckMutation.RemoveAsync(added, false, true)).Success,
                "Ave Fire diagnostic failed to remove a generated persistent card");
        }
        await RemoveCombatCardsAsync(player.PlayerCombatState.AllCards.Where(card => !combatBefore.Contains(card)).ToArray());
        foreach ((Creature enemy, int block) in blocks)
        {
            await RemoveAddedBlockAsync(choiceContext, enemy, block);
        }
        rng.LoadFromSerializable(rngBefore);
        await RestoreCombatCardsAsync(moved);
    }
}
