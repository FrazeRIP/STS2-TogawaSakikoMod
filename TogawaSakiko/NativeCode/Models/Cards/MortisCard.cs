using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class MortisCard : CardModel
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MortisPower>(1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Injury>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool HasTurnEndInHandEffect => true;

    public override string PortraitPath => NativeAssetPaths.MortisPortrait;

    public MortisCard()
        : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        MortisPower? power = await PowerCmd.Apply<MortisPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(MortisPower)].BaseValue,
            Owner.Creature,
            this);
        if (power is not null)
        {
            power.SkipNextDurationTick = false;
        }
    }
}
