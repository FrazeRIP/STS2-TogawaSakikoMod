using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class KindnessCard : CardModel
{
    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override string PortraitPath => NativeAssetPaths.KindnessPortrait;

    public KindnessCard()
        : base(0, CardType.Power, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await KindnessPower.AddAsync(choiceContext, this);
    }
}
