using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class WishFulfilledCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Ethereal];

    public override string PortraitPath => NativeAssetPaths.WishFulfilledPortrait;

    public WishFulfilledCard()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    internal static CardModel[] GetPurgeCandidates(IEnumerable<CardModel> cards)
    {
        return cards.Where(card => card.IsRemovable).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel[] candidates = GetPurgeCandidates(
            PileType.Hand.GetPile(Owner).Cards
                .Concat(PileType.Discard.GetPile(Owner).Cards)
                .Concat(PileType.Draw.GetPile(Owner).Cards));
        if (candidates.Length == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1)))
            .FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        SakikoPurgeResult result = await SakikoPurgeCommand.RemoveAsync(
            selected,
            choiceContext: choiceContext);
        if (!result.Success && !result.Prevented)
        {
            NativeSmokeTrace.N5Info(
                $"Wish Fulfilled could not purge {selected.Id}: {result.FailureReason}");
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
