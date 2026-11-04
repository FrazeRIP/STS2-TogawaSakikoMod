using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class ImprisonedXIICard : CardModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard(ModelDb.Card<DesireCard>(), IsUpgraded)];

    public override string PortraitPath => NativeAssetPaths.ImprisonedXIIPortrait;

    public ImprisonedXIICard()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Imprisoned XII requires an active combat.");
        int maximum = PileType.Hand.GetPile(Owner).Cards.Count;
        if (maximum == 0)
        {
            return;
        }

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, maximum) { Cancelable = true },
            filter: null,
            source: this)).ToArray();
        foreach (var card in selected)
        {
            // Same create, upgrade, then transform sequence as the native Guards card.
            var desire = combatState.CreateCard<DesireCard>(Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(desire);
            }

            CardCmd.ApplyKeyword(desire, CardKeyword.Retain);
            await CardCmd.Transform(card, desire, CardPreviewStyle.HorizontalLayout);
        }
    }
}
