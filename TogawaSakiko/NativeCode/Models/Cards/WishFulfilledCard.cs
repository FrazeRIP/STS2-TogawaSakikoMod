using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
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

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        ChooseAndPurgeAsync(choiceContext, Owner, SelectionScreenPrompt);

    internal static async Task ChooseAndPurgeAsync(PlayerChoiceContext choiceContext, Player owner, LocString prompt)
    {
        CardModel[] candidates = GetPurgeCandidates(
            PileType.Hand.GetPile(owner).Cards
                .Concat(PileType.Discard.GetPile(owner).Cards)
                .Concat(PileType.Draw.GetPile(owner).Cards));
        if (candidates.Length == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                owner,
                new CardSelectorPrefs(prompt, 1)))
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
