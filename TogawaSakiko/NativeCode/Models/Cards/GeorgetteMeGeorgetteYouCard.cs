using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class GeorgetteMeGeorgetteYouCard : CardModel
{
    private int _permanentCostIncrease;

    [SavedProperty]
    public int PermanentCostIncrease
    {
        get => _permanentCostIncrease;
        set
        {
            AssertMutable();
            _permanentCostIncrease = Math.Max(0, value);
            EnergyCost.SetCustomBaseCost(_permanentCostIncrease);
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override string PortraitPath => NativeAssetPaths.GeorgetteMeGeorgetteYouPortrait;

    public GeorgetteMeGeorgetteYouCard()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var persistent = DeckVersion as GeorgetteMeGeorgetteYouCard;
        bool increasesCost = !IsUpgraded;
        await WishFulfilledCard.ChooseAndPurgeAsync(
            choiceContext,
            Owner,
            new LocString("cards", "TOGAWASAKIKO-WISH_FULFILLED_CARD.selectionScreenPrompt"));
        if (increasesCost)
        {
            int next = (persistent?.PermanentCostIncrease ?? PermanentCostIncrease) + 1;
            PermanentCostIncrease = next;
            if (persistent is not null && !persistent.HasBeenRemovedFromState)
            {
                persistent.PermanentCostIncrease = next;
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(PermanentCostIncrease);
    }
}
