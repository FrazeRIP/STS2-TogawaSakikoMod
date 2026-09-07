using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Pools;

public sealed class TogawaSakikoCardPool : CardPoolModel
{
    public override string Title => "TogawaSakiko";

    public override string EnergyColorName => "togawa_sakiko";

    public override string CardFrameMaterialPath => "card_frame_togawa_sakiko";

    public override Color DeckEntryCardColor => new("8295A8");

    public override Color EnergyOutlineColor => new("1D5673");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        return Array.Empty<CardModel>();
    }

    protected override IEnumerable<CardModel> FilterThroughEpochs(
        UnlockState unlockState,
        IEnumerable<CardModel> cards)
    {
        return cards.Where(card =>
            card is ASplitMomentCard or TwoMoonsCard or SilentFarewellCard or GreetingsCard);
    }
}
