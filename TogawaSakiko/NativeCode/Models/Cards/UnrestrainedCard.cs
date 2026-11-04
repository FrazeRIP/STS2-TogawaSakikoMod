using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class UnrestrainedCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];

    public override string PortraitPath => NativeAssetPaths.OurSongPortrait;

    public UnrestrainedCard()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await UnrestrainedPower.AddAsync(choiceContext, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1);
    }
}
