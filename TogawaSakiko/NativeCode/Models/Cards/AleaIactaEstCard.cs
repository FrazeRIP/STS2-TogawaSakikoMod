using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AleaIactaEstCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<NoDrawPower>()];

    public override string PortraitPath => NativeAssetPaths.AleaIactaEstPortrait;

    public AleaIactaEstCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> cards = PileType.Hand.GetPile(Owner).Cards;
        if (IsUpgraded)
        {
            cards = cards.Concat(PileType.Draw.GetPile(Owner).Cards);
        }

        foreach (CardModel card in cards.Where(card => card.IsUpgradable).ToArray())
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        await PowerCmd.Apply<NoDrawPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
    }
}
