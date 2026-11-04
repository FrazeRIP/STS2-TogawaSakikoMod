using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class WishFulfilledCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    public override bool CanBeGeneratedInCombat => false;

    public override string PortraitPath => NativeAssetPaths.WishFulfilledPortrait;

    public WishFulfilledCard()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    internal static CardModel[] GetPurgeCandidates(IEnumerable<CardModel> cards)
    {
        return cards.Where(SakikoPurgeCommand.IsEligible).ToArray();
    }

    internal static int ComparePurgeCandidates(CardModel left, CardModel right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await ChooseAndPurgeAsync(choiceContext, Owner, SelectionScreenPrompt, DynamicVars["MagicNumber"].IntValue);
    }

    internal static Task ChooseAndPurgeAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        LocString prompt,
        int maxCount = 1)
    {
        return ChooseFromCandidatesAsync(choiceContext, owner, prompt, GetPurgeCandidates(owner.Deck.Cards), maxCount);
    }

    internal static async Task ChooseFromCandidatesAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        LocString prompt,
        IEnumerable<CardModel> available,
        int maxCount = 1)
    {
        CardModel[] candidates = GetPurgeCandidates(available);
        if (candidates.Length == 0 || maxCount <= 0)
        {
            return;
        }

        using var locationTips = PurgeCardLocationTips.ShowFor(candidates);
        var selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                owner,
                new CardSelectorPrefs(prompt, 0, Math.Min(maxCount, candidates.Length))
                {
                    Cancelable = true,
                    Comparison = ComparePurgeCandidates
                }))
            .ToArray();
        foreach (CardModel card in selected)
        {
            SakikoPurgeResult result = await SakikoPurgeCommand.RemoveAsync(card);
            if (!result.Success && !result.Prevented)
            {
                throw new InvalidOperationException($"Could not purge {card.Id}: {result.FailureReason}");
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
