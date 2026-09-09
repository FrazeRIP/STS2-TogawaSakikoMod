using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Modifiers;
using TogawaSakiko.NativeCode.Models.Pools;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class MultiplayerContentContractTests
{
    internal static int Run()
    {
        int assertions = 0;
        void Require(bool condition, string message)
        {
            assertions++;
            if (!condition)
            {
                throw new InvalidOperationException("Multiplayer content contract failed: " + message);
            }
        }

        foreach (CardModel card in new CardModel[]
                 { ModelDb.Card<MomentMemoryCard>(), ModelDb.Card<NovaHistoriaCard>() })
        {
            bool isMomentMemory = card is MomentMemoryCard;
            Require(card.EnergyCost.Canonical == (isMomentMemory ? 4 : 2) && card.Type == CardType.Skill, "skill cost");
            Require(card.Rarity == CardRarity.Token && card.TargetType == TargetType.AllAllies,
                "test-only team card");
            Require(card.Keywords.SetEquals([CardKeyword.Exhaust]), "exhaust exactly once");
            Require(!TogawaSakikoCardPool.IsEnabledCard(card), "excluded from ordinary generation");
            Require(!CardFactory.FilterForCombat([card]).Any(), "native Perfection/Hairband generation excludes test card");
            Require(!card.CanBeGeneratedByModifiers, "modifier generation excludes test card");
            Require(ModelDb.CardPool<TogawaSakikoCardPool>().AllCards.Contains(card), "explicitly registered");
            Require(new LocString("cards", card.Id.Entry + ".title").Exists() && card.Title.Length > 0, "localized title");
            Require(ResourceLoader.Exists(card.PortraitPath), "existing portrait resolves");
            CardModel mutable = card.ToMutable();
            string variable = card is MomentMemoryCard ? "Rewards" : "HypePower";
            Require(mutable.DynamicVars[variable].BaseValue == (isMomentMemory ? 1m : 2m), "base effect");
            string description = mutable.GetDescriptionForPile(PileType.Deck);
            Require(description.Length > 0 && !description.Contains('{'), "base description formats");
            mutable.UpgradeInternal();
            Require(mutable.EnergyCost.Canonical == (isMomentMemory ? 3 : 2), "upgraded cost");
            Require(mutable.DynamicVars[variable].BaseValue == (isMomentMemory ? 1m : 3m), "upgraded effect");
            Require(!mutable.GetDescriptionForPile(PileType.Deck).Contains('{'), "upgrade description formats");
        }

        KingsRewardCarrierModifier carrier = (KingsRewardCarrierModifier)
            ModelDb.Modifier<KingsRewardCarrierModifier>().ToMutable();
        carrier.SetPending(92, true);
        carrier.SetPending(11, true);
        carrier.SetPending(92, true);
        Require(carrier.PendingPlayerIds == "11,92", "stable sorted duplicate-free saved owners");
        carrier.SetPending(11, false);
        Require(!carrier.IsPending(11) && carrier.IsPending(92), "clearing one player preserves another");
        Require(KingsRewardCarrierModifier.ParsePendingPlayerIds("92,11,92,invalid")
            .SequenceEqual(new ulong[] { 11, 92 }), "legacy save parsing retains valid owners");
        Require(NativePackageIdentity.HandshakeEntry.StartsWith("TogawaSakiko-package-sha256-", StringComparison.Ordinal),
            "exact deployed package identity available");
        Player first = Player.CreateForNewRun<Ironclad>(UnlockState.all, 7311);
        Player second = Player.CreateForNewRun<Models.Characters.TogawaSakiko>(UnlockState.all, 7312);
        RunState run = RunState.CreateForTest([first, second], seed: "PURGEREWARDSAVE");
        CombatRoom room = new(ModelDb.Encounter<VineShamblerNormal>().ToMutable(), run);
        room.AddExtraReward(first, new CardRemovalReward(first));
        room.AddExtraReward(second, new CardRemovalReward(second));
        room.AddExtraReward(second, new CardRemovalReward(second));
        room.MarkPreFinished();
        CombatRoom restored = CombatRoom.FromSerializable(room.ToSerializable(), run);
        Require(restored.IsPreFinished && restored.ExtraRewards.Count == 2, "completed reward room survives native save");
        Require(restored.ExtraRewards[first].Count == 1 && restored.ExtraRewards[second].Count == 2,
            "native save preserves independent purge reward counts");
        Require(restored.ExtraRewards.All(pair => pair.Value.All(reward =>
                reward is CardRemovalReward && ReferenceEquals(reward.Player, pair.Key))),
            "native reward reconstruction preserves each recipient");
        return assertions;
    }
}
