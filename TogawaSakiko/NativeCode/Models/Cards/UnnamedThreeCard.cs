using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UnnamedThreeCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.WhiteKeysPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 2m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public UnnamedThreeCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ArdorPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1);
    }
}
