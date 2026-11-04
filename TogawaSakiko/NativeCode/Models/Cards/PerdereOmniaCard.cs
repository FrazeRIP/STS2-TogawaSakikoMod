using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class PerdereOmniaCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 2m)];

    public override string PortraitPath => NativeAssetPaths.PerdereOmniaPortrait;

    public PerdereOmniaCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "PerdereOmnia");
        await WishFulfilledCard.ChooseFromCandidatesAsync(
            choiceContext,
            Owner,
            SelectionScreenPrompt,
            SakikoPurgeCommand.GetCandidates(Owner).Where(card => card.Type == CardType.Curse),
            DynamicVars["MagicNumber"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
