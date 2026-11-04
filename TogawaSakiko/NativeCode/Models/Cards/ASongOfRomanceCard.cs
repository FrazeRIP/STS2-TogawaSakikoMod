using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class ASongOfRomanceCard : CardModel
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<DesireCard>()];

    public override string PortraitPath => NativeAssetPaths.AreTheseLyricsPortrait;

    public ASongOfRomanceCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var desire = CombatState!.CreateCard<DesireCard>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(desire, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
