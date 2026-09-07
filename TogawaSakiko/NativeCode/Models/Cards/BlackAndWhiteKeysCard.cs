using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class BlackAndWhiteKeysCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<BlackKeysCard>(), HoverTipFactory.FromCard<WhiteKeysCard>()];

    public override string PortraitPath => NativeAssetPaths.BlackAndWhiteKeysPortrait;

    public BlackAndWhiteKeysCard()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = CombatState
            ?? throw new InvalidOperationException("Black and White Keys requires an active combat.");
        BlackKeysCard blackKeys = combatState.CreateCard<BlackKeysCard>(Owner);
        WhiteKeysCard whiteKeys = combatState.CreateCard<WhiteKeysCard>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade([blackKeys, whiteKeys], CardPreviewStyle.HorizontalLayout);
        }

        CardModel? choice = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            [blackKeys, whiteKeys],
            Owner);
        switch (choice)
        {
            case BlackKeysCard black:
                await black.ApplyChoiceAsync(choiceContext, cardPlay.Target, this);
                break;
            case WhiteKeysCard white:
                await white.ApplyChoiceAsync(choiceContext, cardPlay.Target, this);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
