using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class HeartsBarrierCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move)
            .WithMultiplier(static (CardModel card, Creature? _) => card.Owner.Deck.Cards.Count)
    ];

    public override string PortraitPath => NativeAssetPaths.HeartsBarrierPortrait;

    public HeartsBarrierCard()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "HeartsBarrier");
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
            DynamicVars.CalculatedBlock.Props,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
