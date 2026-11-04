using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UnceasingMelodyCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.BlackAndWhiteKeysPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<HypePower>(), HoverTipFactory.FromCard<TirednessCard>()];

    public UnceasingMelodyCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HypePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);
        var addition = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(Owner);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
