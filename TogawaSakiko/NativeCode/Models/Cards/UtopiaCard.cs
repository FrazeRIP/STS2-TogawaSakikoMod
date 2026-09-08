using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UtopiaCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HpLossVar("MagicNumber", 15m)];

    public override string PortraitPath => NativeAssetPaths.UtopiaPortrait;

    public UtopiaCard()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DynamicVars["MagicNumber"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            dealer: cardPlay.Target,
            cardSource: this,
            cardPlay: cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(5m);
    }
}
