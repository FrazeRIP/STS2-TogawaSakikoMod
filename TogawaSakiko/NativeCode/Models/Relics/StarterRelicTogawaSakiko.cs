using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class StarterRelicTogawaSakiko : RelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath => NativeAssetPaths.MonochromeHairbandIcon;

    protected override string PackedIconOutlinePath => NativeAssetPaths.MonochromeHairbandOutline;

    protected override string BigIconPath => NativeAssetPaths.MonochromeHairbandBigIcon;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature.IsDead)
        {
            return;
        }

        Flash();
        CardModel desire = Owner.RunState.CreateCard<DesireCard>(Owner);
        CardPileAddResult result = await CardPileCmd.Add(desire, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result, 2f);
        NativeSmokeTrace.Info($"Monochrome Hairband added Desire to the deck; success={result.success}.");
    }
}
