using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class WhiteKeysCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DazzlingPower>(), HoverTipFactory.FromPower<StrengthPower>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool CanBeGeneratedInCombat => false;

    public override string PortraitPath => NativeAssetPaths.WhiteKeysPortrait;

    public WhiteKeysCard()
        : base(-1, CardType.Power, CardRarity.Token, TargetType.None)
    {
    }

    public async Task ApplyChoiceAsync(
        PlayerChoiceContext choiceContext,
        Creature? enemy,
        CardModel cardSource)
    {
        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            cardSource.Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            cardSource.Owner.Creature,
            cardSource);
        if (enemy is not null)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                enemy,
                DynamicVars["MagicNumber"].BaseValue,
                cardSource.Owner.Creature,
                cardSource);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
