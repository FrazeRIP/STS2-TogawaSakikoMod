using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AmorisCard : CardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Retain];

    public override string PortraitPath => NativeAssetPaths.AmorisPortrait;

    public AmorisCard()
        : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }
}
