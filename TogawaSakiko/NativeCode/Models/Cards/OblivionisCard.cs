using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class OblivionisCard : CardModel
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Ethereal];

    public override string PortraitPath => NativeAssetPaths.OblivionisPortrait;

    public OblivionisCard()
        : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation location)
    {
        if (Pile?.Type == PileType.Hand &&
            card.Owner.Creature == Owner.Creature &&
            card.Type == CardType.Attack)
        {
            location.pileType = PileType.Exhaust;
        }

        return location;
    }

}
