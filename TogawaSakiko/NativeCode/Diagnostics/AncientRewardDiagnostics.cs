using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TogawaSakiko.NativeCode.Content;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Relics;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Diagnostics;

// Explicit diagnostic launch only, in an isolated test installation and save directory.
internal static class AncientRewardDiagnostics
{
    public const string Argument = "togawa-ancient-rewards-test";
    public static bool Enabled => CommandLineHelper.HasArg(Argument);
    internal static bool IsValidatingSetup { get; private set; }
    private static bool _started;
    private static int _assertions;

    private static void Require(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException("Ancient rewards: " + message);
    }

    private sealed class Selector(CardModel selected, Action? onSelect = null) : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            Require(options.Contains(selected), "expected card missing from native selection");
            onSelect?.Invoke();
            return Task.FromResult<IEnumerable<CardModel>>([selected]);
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives) => new() { card = options.FirstOrDefault()?.Card };
    }

    public static async Task RunAsync(PlayerChoiceContext context, CardModel source)
    {
        if (!Enabled || _started) return;
        _started = true;
        IsValidatingSetup = true;
        try { ValidateSetupAndPools(); }
        finally { IsValidatingSetup = false; }
        Player player = source.Owner;
        CombatState combat = (CombatState)player.Creature.CombatState!;
        CombatRoom room = (CombatRoom)player.RunState.CurrentRoom!;
        Creature target = combat.GetOpponentsOf(player.Creature).First(c => c.IsHittable);

        foreach (RelicModel relic in player.Relics.ToArray()) await RelicCmd.Remove(relic);
        foreach (PowerModel power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (PowerModel power in target.Powers.ToArray()) await PowerCmd.Remove(power);
        await CreatureCmd.GainBlock(target, 10000m, ValueProp.Unpowered, null, fast: true);

        foreach (RelicModel canonical in new RelicModel[]
        {
            ModelDb.Relic<StarterRelicTogawaSakiko>(), ModelDb.Relic<BlazingHairband>(), ModelDb.Relic<AnotherMask>()
        })
        {
            RelicModel original = await RelicCmd.Obtain(canonical.ToMutable(), player);
            var touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
            Require(touch.SetupForPlayer(player), "Orobas did not offer " + original.Id);
            touch = (TouchOfOrobas)RelicModel.FromSerializable(touch.ToSerializable());
            CardModel[] deckBeforeReplace = player.Deck.Cards.ToArray();
            await RelicCmd.Obtain(touch, player);
            EnchantedHairband enchanted = player.GetRelic<EnchantedHairband>()!;
            Require(enchanted != null && original.HasBeenRemovedFromState, "Orobas did not replace the chosen relic");
            Require(player.Deck.Cards.SequenceEqual(deckBeforeReplace), "Orobas changed the deck, including Another Mask's previous changes");
            Require(!((TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable()).SetupForPlayer(player),
                "Enchanted Hairband was offered for refinement again");
            HashSet<CardModel> before = player.Deck.Cards.ToHashSet();
            await enchanted!.AfterCombatVictory(room);
            CardModel[] additions = player.Deck.Cards.Where(c => !before.Contains(c)).ToArray();
            Require(additions.Length == 1 && additions[0].IsUpgraded && additions[0].Rarity != CardRarity.Ancient,
                "Enchanted Hairband did not add exactly one upgraded non-Ancient card");
            await enchanted.AfterCombatVictory(room);
            Require(player.Deck.Cards.Count == before.Count + 1, "duplicate victory awarded another card");
            Require(RelicModel.FromSerializable(enchanted.ToSerializable()) is EnchantedHairband,
                "Enchanted Hairband save round-trip");
            await RelicCmd.Remove(enchanted);
            await RelicCmd.Remove(touch);
        }
        GD.Print("Ancient rewards: all three Orobas pickups, saved previews, upgraded victory rewards and idempotence passed.");

        foreach (CardModel moonlight in player.Deck.Cards.OfType<TheMoonlightSonataCard>().ToArray())
            await CardPileCmd.RemoveFromDeck(moonlight);
        Require(!((ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable()).SetupForPlayer(player), "Tooth accepted a missing Moonlight Sonata");
        Orobas orobas = (Orobas)ModelDb.Event<Orobas>().ToMutable();
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Owner)).SetValue(orobas, player);
        var locked = (IEnumerable<EventOption>)AccessTools.Property(typeof(Orobas), "OptionPool3").GetValue(orobas)!;
        Require(locked.Single().Relic is null, "Orobas did not safely lock its third option without either prerequisite");
        foreach (bool upgrade in new[] { false, true })
        {
            var add = await PersistentDeckMutation.AddCanonicalAsync<TheMoonlightSonataCard>(player, skipVisuals: true,
                upgradeLevel: upgrade ? 1 : 0);
            if (upgrade) CardCmd.Enchant((Sharp)ModelDb.Enchantment<Sharp>().MutableClone(), add.cardAdded, 3m);
            var tooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
            Require(tooth.SetupForPlayer(player), "Tooth could not find Moonlight Sonata");
            tooth = (ArchaicTooth)RelicModel.FromSerializable(tooth.ToSerializable());
            HashSet<CardModel> before = player.Deck.Cards.ToHashSet();
            await RelicCmd.Obtain(tooth, player);
            CardModel replacement = player.Deck.Cards.Single(c => !before.Contains(c));
            Require(!player.Deck.Cards.Contains(add.cardAdded) && replacement is TheThirdMovementCard && replacement.IsUpgraded == upgrade,
                "Tooth replacement did not preserve upgrade");
            Require(!upgrade || replacement.Enchantment is Sharp { Amount: 3 }, "Tooth did not preserve enchantment");
            await RelicCmd.Remove(tooth);
        }
        var tome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
        tome.SetupForPlayer(player);
        tome = (DustyTome)RelicModel.FromSerializable(tome.ToSerializable());
        HashSet<CardModel> beforeTome = player.Deck.Cards.ToHashSet();
        await RelicCmd.Obtain(tome, player);
        CardModel tomeCard = player.Deck.Cards.Single(c => !beforeTome.Contains(c));
        Require(tomeCard is CharismaticFormCard && tomeCard.IsUpgraded && tomeCard.Keywords.Contains(CardKeyword.Innate),
            "Dusty Tome did not grant Charismatic Form+");
        GD.Print("Ancient rewards: native Tooth and Tome pickups with serialized setup passed.");

        foreach (CardModel card in PileType.Hand.GetPile(player).Cards.ToArray())
            await CardPileCmd.Add(card, PileType.Discard);

        foreach (CardModel original in player.Deck.Cards.Where(c => c is TheThirdMovementCard or CharismaticFormCard).ToArray())
        {
            CardModel handCard = combat.CloneCard(original);
            handCard.DeckVersion = original;
            await CardPileCmd.AddGeneratedCardToCombat(handCard, PileType.Hand, player);
            HashSet<CardModel> beforeCopy = player.Deck.Cards.ToHashSet();
            using (CardSelectCmd.PushSelector(new Selector(handCard)))
                await PlayAsync<AsYourHeartDesiresCard>(combat, player, context, null);
            CardModel copy = player.Deck.Cards.Single(c => !beforeCopy.Contains(c));
            Require(copy.Id == original.Id && copy.CurrentUpgradeLevel == original.CurrentUpgradeLevel && !ReferenceEquals(copy, original),
                "As Your Heart Desires failed to copy an owned Ancient card");
            Require(copy.Enchantment?.Id == original.Enchantment?.Id && copy.Enchantment?.Amount == original.Enchantment?.Amount,
                "As Your Heart Desires failed to preserve an Ancient card's enchantment");
            await CardPileCmd.Add(handCard, PileType.Discard);
        }

        foreach (bool upgraded in new[] { false, true })
        {
            var addition = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<StrikeTogawaSakiko>(
                player, combat, PileType.Draw, skipPersistentVisuals: true);
            CardModel victim = addition.CombatCard!;
            int blockBefore = target.Block;
            bool selectedBeforeDamage = false;
            TheThirdMovementCard played;
            using (CardSelectCmd.PushSelector(new Selector(victim, () => selectedBeforeDamage = target.Block == blockBefore)))
                played = await PlayAsync<TheThirdMovementCard>(combat, player, context, target, upgraded);
            Require(selectedBeforeDamage && !player.Deck.Cards.Contains(addition.PersistentCard!), "purge did not precede damage");
            Require(blockBefore - target.Block == (upgraded ? 45 : 30), "Third Movement damage incorrect");
            Require(played.Pile?.Type == PileType.Exhaust, "Third Movement did not Exhaust");
        }
        foreach (PileType pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
            foreach (CardModel card in pile.GetPile(player).Cards.ToArray())
                await CardPileCmd.Add(card, PileType.Exhaust);
        int emptyBlockBefore = target.Block;
        await PlayAsync<TheThirdMovementCard>(combat, player, context, target);
        Require(emptyBlockBefore - target.Block == 30, "empty purge candidates suppressed damage");
        Player reloaded = Player.FromSerializable(player.ToSerializable());
        Require(reloaded.Deck.Cards.Count(c => c is TheThirdMovementCard or CharismaticFormCard) ==
            player.Deck.Cards.Count(c => c is TheThirdMovementCard or CharismaticFormCard), "Ancient cards lost on player inventory reload");
        await CaptureArtAsync(player);

        var lethalVictim = await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<StrikeTogawaSakiko>(
            player, combat, PileType.Draw, skipPersistentVisuals: true);
        await CreatureCmd.LoseBlock(context, target, target.Block, player.Creature);
        await CreatureCmd.SetCurrentHp(target, 1m);
        using (CardSelectCmd.PushSelector(new Selector(lethalVictim.CombatCard!)))
            await PlayAsync<TheThirdMovementCard>(combat, player, context, target);
        Require(target.IsDead && !player.Deck.Cards.Contains(lethalVictim.PersistentCard!), "killing blow skipped the purge");
        GD.Print($"Ancient rewards: PASS ({_assertions} assertions). Pickups, pools, save round-trips, copying, purge order, damage and Exhaust verified.");
    }

    private static async Task CaptureArtAsync(Player player)
    {
        if (DisplayServer.GetName() == "headless") return;
        string output = CommandLineHelper.GetValue("togawa-ancient-output")
            ?? throw new InvalidOperationException("Rendered Ancient diagnostics require an output directory.");
        System.IO.Directory.CreateDirectory(output);
        var overlay = new CanvasLayer { Layer = 1000 };
        NGame.Instance!.AddChild(overlay);
        Vector2 size = NGame.Instance.GetViewport().GetVisibleRect().Size;
        overlay.AddChild(new ColorRect { Color = new Color("141824"), Size = size });
        CardModel[] cards =
        [
            player.RunState.CreateCard<TheThirdMovementCard>(player), player.RunState.CreateCard<TheThirdMovementCard>(player),
            player.RunState.CreateCard<CharismaticFormCard>(player), player.RunState.CreateCard<CharismaticFormCard>(player)
        ];
        for (int i = 0; i < cards.Length; i++)
        {
            if (i % 2 == 1) CardCmd.Upgrade(cards[i], CardPreviewStyle.None);
            NCard node = NCard.Create(cards[i])!;
            overlay.AddChild(node);
            node.Position = new Vector2(size.X * (i + 1) / 5f, size.Y * 0.47f);
            node.Scale = Vector2.One * Math.Min(1.2f, size.X / 1700f);
            node.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            Require(node.GetNode<TextureRect>("%AncientPortrait").Visible && !node.GetNode<TextureRect>("%Frame").Visible,
                "card did not activate native full-art Ancient layout");
        }
        overlay.AddChild(new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(NativeAssetPaths.RelicIcon("enchantedhairband")),
            Position = new Vector2(size.X / 2f - 64, size.Y - 160), Size = new Vector2(128, 128)
        });
        await FeedbackVisualDiagnostics.SnapshotAsync(System.IO.Path.Combine(output, "ancient-cards.png"), CancellationToken.None);
        overlay.QueueFree();
        GD.Print("Ancient rewards: rendered full-art cards and relic captured.");
    }

    private static async Task<T> PlayAsync<T>(CombatState combat, Player player, PlayerChoiceContext context,
        Creature? target, bool upgraded = false) where T : CardModel
    {
        T card = combat.CreateCard<T>(player);
        if (upgraded) CardCmd.Upgrade(card, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player);
        await CardCmd.AutoPlay(context, card, target, AutoPlayType.Default, skipCardPileVisuals: true);
        return card;
    }

    private static void ValidateSetupAndPools()
    {
        Player first = Player.CreateForNewRun<SakikoCharacter>(UnlockState.all, 8101);
        Player second = Player.CreateForNewRun<SakikoCharacter>(UnlockState.all, 8102);
        Player vanilla = Player.CreateForNewRun<Ironclad>(UnlockState.all, 8103);
        _ = RunState.CreateForTest([first, second, vanilla], seed: "SAKIKOANCIENT");
        foreach (Player player in new[] { first, second, vanilla })
            foreach (RelicModel relic in player.Relics.ToArray()) player.RemoveRelicInternal(relic, silent: true);
        second.AddRelicInternal(ModelDb.Relic<StarterRelicTogawaSakiko>().ToMutable());
        vanilla.AddRelicInternal(ModelDb.Relic<BurningBlood>().ToMutable());
        Require(!((TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable()).SetupForPlayer(first),
            "Orobas used another player's relic");
        second.Relics.Single().IsMelted = true;
        Require(!((TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable()).SetupForPlayer(second), "Orobas accepted a melted relic");
        second.Relics.Single().IsMelted = false;
        var vanillaTouch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
        Require(vanillaTouch.SetupForPlayer(vanilla) && vanillaTouch.UpgradedRelic == ModelDb.Relic<BlackBlood>().Id,
            "vanilla Orobas mapping changed");
        var vanillaTooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
        Require(vanillaTooth.SetupForPlayer(vanilla) && vanillaTooth.AncientCard!.Id == ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Break>().Id,
            "vanilla Tooth mapping changed");

        foreach (CardModel card in new CardModel[] { ModelDb.Card<TheThirdMovementCard>(), ModelDb.Card<CharismaticFormCard>() })
        {
            Require(card.Rarity == CardRarity.Ancient && !card.CanBeGeneratedInCombat && !card.CanBeGeneratedByModifiers,
                "Ancient generation metadata");
            Require(!CardFactory.FilterForCombat([card]).Any() && !BlazingHairband.IsEligibleRandomCard(card),
                "combat/potion/hairband generation admitted " + card.Id);
            Require(CardModel.FromSerializable(card.ToMutable().ToSerializable()).Id == card.Id, "card stable save identity");
        }
        var choices = first.Character.CardPool.GetUnlockedCards(first.UnlockState, first.RunState.CardMultiplayerConstraint);
        Require(choices.Where(c => c.Rarity == CardRarity.Ancient && !ArchaicTooth.TranscendenceCards.Contains(c))
            .SequenceEqual(new[] { ModelDb.Card<CharismaticFormCard>() }), "Tome must have exactly one eligible card");
        for (int i = 0; i < 32; i++)
        {
            var tome = (DustyTome)ModelDb.Relic<DustyTome>().ToMutable();
            tome.SetupForPlayer(first);
            Require(tome.AncientCard == ModelDb.Card<CharismaticFormCard>().Id, "Tome selected the wrong card");
        }
        CardModel[] combatPool = CardFactory.FilterForCombat(first.Character.CardPool.AllCards.Where(BlazingHairband.IsEligibleRandomCard)).ToArray();
        Require(combatPool.Length > 0 && combatPool.All(c => c.MaxUpgradeLevel > 0), "hairband pool contains a card without an upgrade");
        Require(CardFactory.GetDefaultTransformationOptions(first.Deck.Cards.OfType<TheMoonlightSonataCard>().First(), false)
            .All(c => c.Rarity != CardRarity.Ancient), "random transformation admitted an Ancient card");
        bool sawTome = false;
        for (uint seed = 1; seed <= 16; seed++)
        {
            Darv darv = (Darv)ModelDb.Event<Darv>().ToMutable();
            AccessTools.Property(typeof(EventModel), nameof(EventModel.Owner)).SetValue(darv, first);
            AccessTools.Property(typeof(EventModel), nameof(EventModel.Rng)).SetValue(darv, new Rng(seed));
            var options = (IReadOnlyList<EventOption>)AccessTools.Method(typeof(Darv), "GenerateInitialOptions").Invoke(darv, null)!;
            Require(options.Count == 3, "Darv option generation failed");
            foreach (DustyTome reward in options.Select(o => o.Relic).OfType<DustyTome>())
            {
                sawTome = true;
                Require(reward.AncientCard == ModelDb.Card<CharismaticFormCard>().Id, "Darv offered the wrong Tome card");
            }
            Require(CardFactory.CreateForReward(first, 3, CardCreationOptions.ForRoom(first, RoomType.Monster))
                .All(c => c.Card.Rarity != CardRarity.Ancient), "normal rewards admitted an Ancient card");
            Require(CardFactory.CreateForMerchant(first, choices, CardType.Power).Card.Rarity != CardRarity.Ancient,
                "merchant admitted an Ancient card");
        }
        Require(sawTome, "Darv probe did not exercise Dusty Tome");
        GD.Print("Ancient rewards: pool exclusion and multiplayer owner-isolated native setup passed.");
    }
}
