using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AllWillsBecomeOneCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.WorldviewPortrait;

    public AllWillsBecomeOneCard()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AllWillsBecomeOnePower.AddAsync(choiceContext, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
