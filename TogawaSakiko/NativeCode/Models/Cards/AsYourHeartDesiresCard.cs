using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AsYourHeartDesiresCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 1m)];

    public override string PortraitPath => NativeAssetPaths.AsYourHeartDesiresPortrait;

    public AsYourHeartDesiresCard()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                filter: null,
                source: this))
            .FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        SakikoAudioCmd.TryPlayCardVoice(Owner, "AsYourHeartDesires");
        CardPileAddResult result = await PersistentDeckMutation.AddStatEquivalentAsync(
            Owner,
            selected.DeckVersion ?? selected);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
