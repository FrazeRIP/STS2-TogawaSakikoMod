using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Relics;

public sealed class AnotherMask : SakikoRelicModel
{
    private bool _appliedStartingChange;
    private bool _applyingStartingChange;

    protected override string AssetStem => "anothermask";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    [SavedProperty]
    public bool AppliedStartingChange
    {
        get => _appliedStartingChange;
        private set
        {
            AssertMutable();
            _appliedStartingChange = value;
        }
    }

    public override bool IsAllowed(IRunState runState) => runState.Players.Any(player =>
        player.Relics.OfType<StarterRelicTogawaSakiko>().Any(relic => !relic.IsMelted));

    public override async Task AfterObtained()
    {
        if (AppliedStartingChange || _applyingStartingChange)
        {
            return;
        }

        _applyingStartingChange = true;
        try
        {
            StarterRelicTogawaSakiko? starter = Owner.Relics
                .OfType<StarterRelicTogawaSakiko>().FirstOrDefault(relic => !relic.IsMelted);
            if (starter is not null)
            {
                await RelicCmd.Remove(starter);
            }

            List<CardTransformation> transformations = [];
            foreach (DefendTogawaSakiko defend in PileType.Deck.GetPile(Owner).Cards.OfType<DefendTogawaSakiko>().ToArray())
            {
                if (!defend.IsTransformable)
                {
                    continue;
                }

                CardModel desire = Owner.RunState.CreateCard<DesireCard>(Owner);
                for (int level = 0; level < defend.CurrentUpgradeLevel; level++)
                {
                    CardCmd.Upgrade(desire, CardPreviewStyle.None);
                }
                transformations.Add(new CardTransformation(defend, desire));
            }

            // Native transformation records deck history and applies normal card-addition hooks.
            CardPileAddResult[] results = (await CardCmd.Transform(transformations, null, CardPreviewStyle.None)).ToArray();
            if (results.Length != transformations.Count || results.Any(result => !result.success))
            {
                throw new InvalidOperationException("Another Mask could not finish replacing the starting Defends.");
            }

            AppliedStartingChange = true;
        }
        finally
        {
            _applyingStartingChange = false;
        }
    }
}
