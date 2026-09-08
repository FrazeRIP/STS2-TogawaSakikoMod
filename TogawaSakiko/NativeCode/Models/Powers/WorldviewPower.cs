using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card.Owner != Owner.Player || !card.Keywords.Contains(CardKeyword.Unplayable))
        {
            return;
        }

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
