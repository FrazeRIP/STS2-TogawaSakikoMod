using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Models.Relics;

public class BlazingHairband : SakikoRelicModel
{
    private ICombatState? _rewardedCombat;

    protected override string AssetStem => "blazinghairband";

    protected virtual bool UpgradeReward => false;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Any(player =>
            player.Relics.OfType<StarterRelicTogawaSakiko>().Any(relic => !relic.IsMelted));
    }

    public override async Task AfterObtained()
    {
        StarterRelicTogawaSakiko? starter = Owner.Relics
            .OfType<StarterRelicTogawaSakiko>()
            .FirstOrDefault(relic => !relic.IsMelted);
        if (starter is not null)
        {
            await RelicCmd.Remove(starter);
        }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature.IsDead ||
            ReferenceEquals(_rewardedCombat, room.CombatState) ||
            !StarterRelicTogawaSakiko.IsEligibleVictory(room.Act.Id.Entry, room.Encounter.Id.Entry))
        {
            return;
        }

        _rewardedCombat = room.CombatState;
        CardModel? randomCard = CardFactory.GetForCombat(
                Owner,
                Owner.Character.CardPool.AllCards.Where(IsEligibleRandomCard),
                1,
                Owner.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (randomCard is null)
        {
            return;
        }

        Flash();
        if (UpgradeReward && randomCard.IsUpgradable)
        {
            CardCmd.Upgrade(randomCard, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
        }
        CardPileAddResult result = await PersistentDeckMutation.AddStatEquivalentAsync(Owner, randomCard);
        HairbandCardPreview.Show(result);
        NativeSmokeTrace.Info($"{GetType().Name} added random card {randomCard.Id} to the deck; upgraded={randomCard.IsUpgraded}; success={result.success}.");
    }

    internal static bool IsEligibleRandomCard(CardModel card)
    {
        return card is not CarefreeCard and not WeaknessCard && card.Rarity != CardRarity.Ancient &&
            card.Type is not CardType.Curse and not CardType.Status;
    }
}
