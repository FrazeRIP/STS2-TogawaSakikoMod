using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class PrideCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.PridePortrait;

    public PrideCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    internal static CardModel[] SelectCandidatePile(IEnumerable<CardModel> drawPile)
    {
        return drawPile.Where(card => card.Type == CardType.Attack).ToArray();
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        var normal = base.GetResultLocationForCardPlay();
        return normal.pileType == PileType.Discard
            ? new CardLocation(Owner, PileType.Draw, CardPilePosition.Top)
            : normal;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "Pride");
        var candidates = SelectCandidatePile(PileType.Draw.GetPile(Owner).Cards);
        if (candidates.Length == 0)
        {
            return;
        }

        var chosen = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1)))
            .FirstOrDefault();
        if (chosen is not null)
        {
            await CardPileCmd.Add(chosen, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
