using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class WorldviewPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    internal static IEnumerable<CardModel> GetEligibleAttacks(Player player) =>
        player.Character.CardPool.AllCards.Where(candidate =>
            candidate.Type == CardType.Attack &&
            !candidate.Keywords.Contains(CardKeyword.Unplayable));

    // Draws, generation and other moves into Hand all pass through this hook, before AfterCardDrawn.
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != Owner.Player || card.Type != CardType.Curse ||
            card.Pile?.Type != PileType.Hand || oldPileType == PileType.Hand)
        {
            return;
        }

        await TransformCurseAsync(card);
    }

    private async Task TransformCurseAsync(CardModel card)
    {
        Player player = card.Owner;
        CardModel? replacement = CardFactory.GetForCombat(
                player,
                GetEligibleAttacks(player),
                1,
                player.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (replacement is null)
        {
            return;
        }

        Flash();
        await CardPileCmd.RemoveFromCombat(card);
        await CardPileCmd.AddGeneratedCardToCombat(replacement, PileType.Hand, player);
    }
}
