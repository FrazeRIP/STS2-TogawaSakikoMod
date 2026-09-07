using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class WeaknessCard : CardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override string PortraitPath => NativeAssetPaths.WeaknessPortrait;

    public WeaknessCard()
        : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    public override async Task BeforeCombatStart()
    {
        if (Pile?.Type == PileType.Draw)
        {
            await CardPileCmd.Add(this, PileType.Discard, skipVisuals: true);
        }
    }
}
