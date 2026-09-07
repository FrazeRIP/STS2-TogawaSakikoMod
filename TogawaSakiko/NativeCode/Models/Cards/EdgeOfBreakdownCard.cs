using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class EdgeOfBreakdownCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FrailPower>()];

    public override string PortraitPath => NativeAssetPaths.EdgeOfBreakdownPortrait;

    public EdgeOfBreakdownCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "EdgeOfBreakdown");
        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1)))
            .FirstOrDefault();
        if (selected is not null)
        {
            SakikoPurgeResult result = await SakikoPurgeCommand.RemoveAsync(
                selected,
                choiceContext: choiceContext);
            if (!result.Success && !result.Prevented)
            {
                throw new InvalidOperationException(
                    $"Edge of Breakdown could not purge {selected.Id}: {result.FailureReason}");
            }
        }

        await PowerCmd.Apply<FrailPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(-1m);
    }
}
