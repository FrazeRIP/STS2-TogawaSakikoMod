using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class OblivionisCard : CardModel
{
    private bool _flashingExhaustWarning;

    protected override bool ShouldGlowRedInternal => IsMutable && Pile?.Type == PileType.Hand &&
        (_flashingExhaustWarning || Pile.Cards.Any(card => card.Type == CardType.Attack));

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

    public override Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation)
    {
        if (cardLocation.pileType == PileType.Exhaust &&
            NPlayerHand.Instance?.GetCardHolder(this) is NHandCardHolder holder)
        {
            // Flash the curse itself when its hand effect sends an attack to the exhaust pile.
            _flashingExhaustWarning = true;
            try
            {
                holder.Flash();
            }
            finally
            {
                _flashingExhaustWarning = false;
            }
        }
        return Task.CompletedTask;
    }

}
