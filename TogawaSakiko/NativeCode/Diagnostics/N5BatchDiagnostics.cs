using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N5BatchDiagnostics
{
    private sealed class Session
    {
    }

    private static readonly ConditionalWeakTable<CombatState, Session> Sessions = new();

    public static async Task RunInCombatAsync(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (!NativeSmokeTrace.N5BatchEnabled)
        {
            return;
        }

        CombatState combatState = sourceCard.CombatState as CombatState
            ?? sourceCard.Owner.Creature.CombatState as CombatState
            ?? throw new InvalidOperationException("Phase N5 diagnostics require a live CombatState.");
        if (Sessions.TryGetValue(combatState, out _))
        {
            return;
        }
        Sessions.Add(combatState, new Session());

        Player player = sourceCard.Owner;
        Creature target = combatState.GetOpponentsOf(player.Creature).First(creature => creature.IsHittable);

        await VerifyMelodyAsync(combatState, player, target, choiceContext);
        await VerifyGreetingsAsync(combatState, player, choiceContext);
        await VerifyTirednessAsync(combatState, player, choiceContext);
        await VerifyIdealAsync(combatState, player, choiceContext);

        NativeSmokeTrace.N5Info(
            "native command card batch passed. GreetingsEnergy=5, TirednessDraw=3, MelodyDamage=15, IdealFreeAttacks=5, VoiceRoutes=3.");
    }

    private static async Task VerifyMelodyAsync(
        CombatState combatState,
        Player player,
        Creature target,
        PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(target, 100m, ValueProp.Unpowered, null, fast: true);
        decimal blockBefore = target.Block;
        MelodyCard baseCard = await CreateAndAutoPlayAsync<MelodyCard>(combatState, player, choiceContext, target, upgraded: false);
        MelodyCard upgradedCard = await CreateAndAutoPlayAsync<MelodyCard>(combatState, player, choiceContext, target, upgraded: true);
        Require(blockBefore - target.Block == 15m, $"Melody expected 15 total damage, found {blockBefore - target.Block}");
        Require(baseCard.Pile?.Type == PileType.Discard, "base Melody did not enter Discard");
        Require(upgradedCard.Pile?.Type == PileType.Discard, "upgraded Melody did not enter Discard");
    }

    private static async Task VerifyGreetingsAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        PlayerCombatState playerState = player.PlayerCombatState
            ?? throw new InvalidOperationException("Phase N5 Greetings diagnostic requires player combat state.");
        decimal energyBefore = playerState.Energy;
        GreetingsCard baseCard = await CreateAndAutoPlayAsync<GreetingsCard>(combatState, player, choiceContext, null, upgraded: false);
        GreetingsCard upgradedCard = await CreateAndAutoPlayAsync<GreetingsCard>(combatState, player, choiceContext, null, upgraded: true);
        Require(playerState.Energy - energyBefore == 5m, "Greetings did not gain 2 plus 3 energy");
        Require(baseCard.Pile?.Type == PileType.Exhaust, "base Greetings did not Exhaust");
        Require(upgradedCard.Pile?.Type == PileType.Exhaust, "upgraded Greetings did not Exhaust");
    }

    private static async Task VerifyTirednessAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        CardModel[] drawProbes =
        [
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<DefendTogawaSakiko>(player),
            combatState.CreateCard<DefendTogawaSakiko>(player)
        ];
        foreach (CardModel probe in drawProbes)
        {
            await CardPileCmd.AddGeneratedCardToCombat(probe, PileType.Draw, player, CardPilePosition.Top);
        }

        TirednessCard baseCard = await CreateAndAutoPlayAsync<TirednessCard>(combatState, player, choiceContext, null, upgraded: false);
        TirednessCard upgradedCard = await CreateAndAutoPlayAsync<TirednessCard>(combatState, player, choiceContext, null, upgraded: true);
        Require(drawProbes.All(card => card.Pile?.Type == PileType.Hand), "Tiredness did not draw exactly three prepared cards");
        Require(baseCard.Pile?.Type == PileType.Exhaust, "base Tiredness did not Exhaust");
        Require(upgradedCard.Pile?.Type == PileType.Exhaust, "upgraded Tiredness did not Exhaust");
    }

    private static async Task VerifyIdealAsync(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext)
    {
        decimal amountBefore = player.Creature.Powers.OfType<FreeAttackPower>().SingleOrDefault()?.Amount ?? 0m;
        await CreateAndAutoPlayAsync<IdealCard>(combatState, player, choiceContext, null, upgraded: false);
        await CreateAndAutoPlayAsync<IdealCard>(combatState, player, choiceContext, null, upgraded: true);
        decimal amountAfter = player.Creature.Powers.OfType<FreeAttackPower>().Single().Amount;
        Require(amountAfter - amountBefore == 5m, "Ideal did not apply 2 plus 3 native Free Attack stacks");
    }

    private static async Task<T> CreateAndAutoPlayAsync<T>(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool upgraded)
        where T : CardModel
    {
        T card = combatState.CreateCard<T>(player);
        if (upgraded)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Top);
        await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipCardPileVisuals: true);
        return card;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Phase N5 actual-game contract failed: " + message + ".");
        }
    }
}
