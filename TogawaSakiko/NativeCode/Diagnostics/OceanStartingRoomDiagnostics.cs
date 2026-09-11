using System.IO;
using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Events;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Patches;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Diagnostics;

// This path runs only with an explicit diagnostic argument and an isolated test profile.
internal static class OceanStartingRoomDiagnostics
{
    internal static readonly string[] ChoiceKeys = ["ANOTHER_MASK", "BLAZING_HAIRBAND", "NORMAL_BLESSING"];

    internal static async Task HandleAsync(CancellationToken cancellationToken)
    {
        RunState runState = RunManager.Instance.DebugOnlyGetState()
            ?? throw Failure("there was no active run");
        EventRoom room = runState.CurrentRoom as EventRoom
            ?? throw Failure("the starting room was not an event");
        // Native Neow preloads an animated background before its mutable event and UI become available.
        ulong roomDeadline = Time.GetTicksMsec() + 30000;
        while ((RunManager.Instance.EventSynchronizer.Events.Count == 0 || NEventRoom.Instance?.Layout is null) &&
               Time.GetTicksMsec() < roomDeadline)
        {
            await Task.Delay(50, cancellationToken);
        }
        Require(RunManager.Instance.EventSynchronizer.Events.Count > 0, "native event preload did not complete");
        OceanOfMemories ocean = room.LocalMutableEvent as OceanOfMemories
            ?? throw Failure("Sakiko's starting room was not Ocean of Memories");
        Player player = ocean.Owner!;
        NEventRoom node = NEventRoom.Instance ?? throw Failure("the event room node was missing");
        NAncientEventLayout scene = node.Layout as NAncientEventLayout
            ?? throw Failure("the original ancient event layout was missing");
        ulong setupDeadline = Time.GetTicksMsec() + 10000;
        while (scene.OptionButtons.Count() != 3 && Time.GetTicksMsec() < setupDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Require(scene.OptionButtons.Count() == 3, "native option setup did not complete");

        // Broader gameplay diagnostics require the unchanged starter relic and starter deck.
        if (!NativeSmokeTrace.StartingRoomEnabled)
        {
            AccessTools.Method(typeof(AncientEventModel), "Done").Invoke(ocean, null);
            // Keep earlier combat contracts at their original damage baseline after exercising the new choice.
            // No starting reward is granted in this broader diagnostic-only path.
            await NEventRoom.Proceed();
            return;
        }

        int choice = int.TryParse(CommandLineHelper.GetValue("togawa-starting-choice"), out int parsed)
            ? parsed : 0;
        Require(choice is >= 0 and < 3, "choice index must be 0, 1, or 2");
        int normalChoice = int.TryParse(CommandLineHelper.GetValue("togawa-normal-choice"), out int normalParsed) ? normalParsed : 0;
        Require(normalChoice is >= 0 and < 3, "normal choice index must be 0, 1, or 2");
        StartingOptionsDiagnostics.Validate(player);
        Require(SakikoStartingRoomPatch.ShouldReplace(runState), "the starting-room boundary did not match the standard Sakiko start");
        VerifyVanillaBoundary();
        VerifyDeck(player);
        Require(player.Relics.Count == 1 && player.Relics[0] is StarterRelicTogawaSakiko,
            "the initial relic inventory was not exactly Monochrome Hairband");
        Require(!ocean.IsFinished && ocean.CurrentOptions.Count == 3,
            "the initial event did not expose exactly three choices");
        EventOption[] initialOptions = ocean.CurrentOptions.ToArray();
        Require(initialOptions.Select(option => option.TextKey.Split('.').Last()).SequenceEqual(ChoiceKeys),
            "the initial choices were missing or reordered");

        for (int frame = 0; frame < 8; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Control artwork = scene.GetNode<Control>("%AncientBgContainer").GetChild<Control>(0);
        Require(artwork.SceneFilePath == OceanOfMemories.NativeBackgroundScenePath,
            "the original Neow background scene did not resolve");
        Require(scene.SceneFilePath == NAncientEventLayout.ancientScenePath && node.CustomEventNode is null,
            "the room still used a custom layout");
        Require(scene.OptionButtons.Count() == 3 &&
                scene.OptionButtons.Select(button => button.Option).SequenceEqual(initialOptions),
            "the native layout did not create the three fixed option buttons");
        Require(ocean.AmbientBgm == ModelDb.Event<Neow>().AmbientBgm &&
                ocean.ButtonColor == ModelDb.Event<Neow>().ButtonColor &&
                ocean.Title.LocEntryKey == "NEOW.title" && ocean.Epithet.LocEntryKey == "NEOW.epithet",
            "the room did not reuse Neow's native presentation");
        foreach (int visits in new[] { 0, 1, 2, 5 })
        {
            var dialogues = ocean.DialogueSet.GetValidDialogues(player.Character.Id, visits, visits, true).ToArray();
            Require(dialogues.Length > 0 && dialogues.SelectMany(dialogue => dialogue.Lines)
                    .All(line => line.LineText is not null && line.LineText.Exists()),
                "native Neow dialogue was missing on a first or repeat visit");
        }
        await VerifyDialogueAsync(scene, ocean, node, cancellationToken);
        await CaptureIfRequestedAsync(node, cancellationToken);

        Require(scene.OptionButtons.ElementAt(2).GetNode<TextureRect>("%RelicIcon") is { Visible: true, Texture: not null },
            "normal blessing speech icon is missing");
        EventOption[] rewardOptions = initialOptions.Take(2).ToArray();
        EventOption selectedReward = initialOptions[choice];
        using IDisposable selector = CardSelectCmd.UseSelector(new MultiplayerStartingEventDiagnostics.FirstCardSelector(), localOnly: true);

        // Exercise the same routed native button entry point used by the visible UI.
        scene.OptionButtons.ElementAt(choice).ForceClick();
        await RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks();
        if (choice == 2)
        {
            Require(!ocean.IsFinished && !room.IsPreFinished && ocean.CurrentOptions.Count == 3 &&
                ocean.CurrentOptions.All(option => option.Relic != null), "navigation did not reveal three native blessings");
            VerifyDeck(player);
            Require(player.Relics.Count == 1 && player.Relics[0] is StarterRelicTogawaSakiko, "navigation granted a reward");
            rewardOptions = ocean.CurrentOptions.ToArray();
            selectedReward = rewardOptions[normalChoice];
            // Invoke the stale reward callbacks; native BeforeChosen UI hooks intentionally disable current buttons.
            foreach (string stale in ChoiceKeys) { await ocean.ChooseAsync(stale); }
            Require(ocean.CurrentOptions.SequenceEqual(rewardOptions) && player.Relics.Count == 1, "stale first-stage option changed stage two");
            Require(scene.OptionButtons.Select(button => button.Option).SequenceEqual(rewardOptions), "native buttons did not refresh");
            for (int frame = 0; frame < 4; frame++)
            {
                await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            scene.OptionButtons.First().GrabFocus();
            Require(scene.OptionButtons.First().HasFocus(), "stage-two controller focus is unavailable");
            await CaptureIfRequestedAsync(node, cancellationToken, "normal-blessings");
            scene.OptionButtons.ElementAt(normalChoice).ForceClick();
            await RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks();
        }
        Require(ocean.IsFinished && room.IsPreFinished, "choosing an option did not finish the native ancient room");
        if (choice < 2) { VerifyChoice(player, choice); }
        Require(player.Relics.Any(relic => relic.Id == selectedReward.Relic!.Id), "selected reward was not obtained");
        string finalInventory = MultiplayerStartingEventDiagnostics.InventorySnapshot(player);
        foreach (EventOption staleOption in initialOptions)
        {
            await staleOption.Chosen();
        }
        if (choice == 0)
        {
            await player.Relics.OfType<AnotherMask>().Single().AfterObtained();
        }
        foreach (EventOption stale in rewardOptions) { await stale.Chosen(); }
        Require(finalInventory == MultiplayerStartingEventDiagnostics.InventorySnapshot(player), "stale options changed final inventory");

        await SaveManager.Instance.SaveRun(room, saveProgress: false);
        ReadSaveResult<SerializableRun> read = SaveManager.Instance.LoadRunSave();
        Require(read.Success && read.SaveData is not null, "the completed starting room save could not be read");
        SerializableRun saved = read.SaveData!;
        Require(saved.PreFinishedRoom is { IsPreFinished: true } &&
                saved.PreFinishedRoom.EventId == ocean.Id,
            "the native save did not preserve the finished custom starting room");
        var choices = saved.MapPointHistory.SelectMany(act => act)
            .SelectMany(point => point.PlayerStats).SelectMany(stats => stats.AncientChoices)
            .ToArray();
        Require(choices.Length == rewardOptions.Length && choices.Count(entry => entry.WasChosen) == 1 &&
                choices.Single(entry => entry.WasChosen).Title.LocEntryKey == selectedReward.Title.LocEntryKey &&
                choices.All(entry => entry.TextKey != SakikoStartingRewards.NormalBlessing),
            "the save did not preserve exactly one chosen option in native ancient history");

        RunState reloadedRun = RunState.FromSerializable(saved);
        Player reloadedPlayer = reloadedRun.Players.Single();
        Require(finalInventory == MultiplayerStartingEventDiagnostics.InventorySnapshot(reloadedPlayer), "reloaded inventory changed");
        EventRoom reloadedRoom = new(saved.PreFinishedRoom!);
        Require(reloadedRoom.CanonicalEvent is OceanOfMemories && reloadedRoom.IsPreFinished,
            "the saved event ID did not restore its custom canonical room");
        OceanOfMemories reloadedEvent = (OceanOfMemories)reloadedRoom.CanonicalEvent.ToMutable();
        await reloadedEvent.BeginEvent(reloadedPlayer, null, isPreFinished: true);
        Require(reloadedEvent.IsFinished && reloadedEvent.CurrentOptions.Count == 0,
            "reloading the finished room reopened the three starting choices");
        foreach (string optionKey in ChoiceKeys)
        {
            await reloadedEvent.ChooseAsync(optionKey);
        }
        if (choice == 0)
        {
            await reloadedPlayer.Relics.OfType<AnotherMask>().Single().AfterObtained();
        }
        Require(finalInventory == MultiplayerStartingEventDiagnostics.InventorySnapshot(reloadedPlayer), "reloaded inventory changed");
        if (CommandLineHelper.HasArg("togawa-starting-collection"))
        {
            // Reconstruct a legacy inventory using the unchanged saved relic ID and counter.
            TheThirdMovement legacy = (TheThirdMovement)ModelDb.Relic<TheThirdMovement>().ToMutable();
            legacy.RemainingUses = 2;
            saved.Players.Single().Relics.Add(legacy.ToSerializable());
            Player legacyPlayer = RunState.FromSerializable(saved).Players.Single();
            TheThirdMovement restoredLegacy = legacyPlayer.GetRelic<TheThirdMovement>()!;
            Require(restoredLegacy is { RemainingUses: 2, IsUsedUp: false } &&
                restoredLegacy.ShouldMultiplyDamage(MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,
                    legacyPlayer.Creature, ModelDb.Card<TheMoonlightSonataCard>()), "legacy Third Movement save or effect was lost");
            var submenu = NRun.Instance!.GlobalUi.SubmenuStack;
            submenu.ShowScreen(CapstoneSubmenuType.Compendium);
            NRelicCollection collection = submenu.Stack.PushSubmenuType<NRelicCollection>();
            Require(collection.Relics.All(relic => relic is not TheThirdMovement), "Third Movement remains in collection navigation");
            Require(collection.GetNode<NRelicCollectionCategory>("%Event").GetGridItems().SelectMany(row => row)
                .OfType<NRelicCollectionEntry>().All(entry => entry.relic is not TheThirdMovement), "Third Movement remains in collection entries");
            Require(collection.Relics.Any(relic => relic is AnotherMask) && collection.Relics.Any(relic => relic is BlazingHairband),
                "the available custom blessings disappeared from the collection");
            await CaptureIfRequestedAsync(node, cancellationToken, "relic-collection");
            submenu.Stack.Pop();
            submenu.Stack.Pop();
            NativeSmokeTrace.StartingRoomInfo("collection passed. ThirdMovement=hidden, CustomBlessings=visible, LegacySave=working.");
        }
        NativeSmokeTrace.StartingRoomInfo(
            $"passed. Choice={ChoiceKeys[choice]}, NormalIndex={normalChoice}, Reward={selectedReward.Relic!.Id.Entry}, Repeat=blocked, SavedChoice=exact, Reload=finished, Vanilla=preserved, NativeNeowLayout=resolved.");

        if (CommandLineHelper.HasArg("togawa-feedback-visuals"))
        {
            await FeedbackVisualDiagnostics.CaptureAsync(runState, cancellationToken);
        }

        // Keep autoslay from advancing or overwriting the completed starting-room save before the harness stops this process.
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private static void VerifyVanillaBoundary()
    {
        Player ironclad = Player.CreateForNewRun<Ironclad>(UnlockState.all, 7481);
        RunState vanillaRun = RunState.CreateForTest([ironclad], seed: "OCEANVANILLA");
        vanillaRun.ExtraFields.StartedWithNeow = true;
        Require(!SakikoStartingRoomPatch.ShouldReplace(vanillaRun), "the custom starting room matched a vanilla character");
        Neow neow = (Neow)ModelDb.Event<Neow>().ToMutable();
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Owner)).SetValue(neow, ironclad);
        AccessTools.Property(typeof(EventModel), nameof(EventModel.Rng)).SetValue(neow, new MegaCrit.Sts2.Core.Random.Rng(7321));
        IReadOnlyList<EventOption> options = (IReadOnlyList<EventOption>)
            AccessTools.Method(typeof(Neow), "GenerateInitialOptions").Invoke(neow, null)!;
        Require(options.Count == 3 && options.All(option => option.Relic is not null) &&
                neow.InitialDescription.LocEntryKey == "NEOW.pages.INITIAL.description",
            "vanilla Neow's three relic choices or description changed");
    }

    private static void VerifyDeck(Player player, bool anotherMask = false)
    {
        Require(player.Deck.Cards.Count == 9 &&
                player.Deck.Cards.Count(card => card is StrikeTogawaSakiko) == 4 &&
                player.Deck.Cards.Count(card => card is DefendTogawaSakiko) == (anotherMask ? 0 : 4) &&
                player.Deck.Cards.Count(card => card is DesireCard) == (anotherMask ? 4 : 0) &&
                player.Deck.Cards.Count(card => card is TheMoonlightSonataCard) == 1 &&
                player.Deck.Cards.All(card => !card.IsUpgraded),
            "the starting choice changed the nine-card starter deck");
    }

    private static void VerifyChoice(Player player, int choice)
    {
        VerifyDeck(player, anotherMask: choice == 0);
        int starter = player.Relics.Count(relic => relic is StarterRelicTogawaSakiko);
        int blazing = player.Relics.Count(relic => relic is BlazingHairband);
        TheThirdMovement? movement = player.Relics.OfType<TheThirdMovement>().SingleOrDefault();
        Require(choice switch
        {
            0 => player.Relics.Count == 1 && player.Relics[0] is AnotherMask { AppliedStartingChange: true } &&
                 starter == 0 && blazing == 0 && movement is null,
            1 => player.Relics.Count == 1 && starter == 0 && blazing == 1 && movement is null,
            _ => false
        }, "the chosen relic inventory or Third Movement charges were not exact");
    }

    private static async Task VerifyDialogueAsync(NAncientEventLayout scene, OceanOfMemories ocean,
        NEventRoom node, CancellationToken cancellationToken)
    {
        AncientDialogueSet set = ocean.DialogueSet;
        ModelId character = ocean.Owner!.Character.Id;
        IReadOnlyList<AncientDialogue> conversations = set.CharacterDialogues[character.Entry];
        Require(conversations.Count == 3 && set.FirstVisitEverDialogue is null,
            "the first Sakiko conversation could be replaced by Neow's shared introduction");
        AncientDialogueSpeaker[][] speakers =
        [
            [AncientDialogueSpeaker.Character, AncientDialogueSpeaker.Ancient, AncientDialogueSpeaker.Ancient],
            [AncientDialogueSpeaker.Character, AncientDialogueSpeaker.Ancient, AncientDialogueSpeaker.Character, AncientDialogueSpeaker.Ancient],
            [AncientDialogueSpeaker.Character, AncientDialogueSpeaker.Ancient, AncientDialogueSpeaker.Ancient]
        ];
        for (int previousVisits = 0; previousVisits < 3; previousVisits++)
        {
            AncientDialogue[] eligible = set.GetValidDialogues(character, previousVisits, previousVisits, true).ToArray();
            Require(eligible.Length == 1 && ReferenceEquals(eligible[0], conversations[previousVisits]),
                "conversation selection did not match the first, second, and third visits");
            Require(eligible[0].Lines.Select(line => line.Speaker).SequenceEqual(speakers[previousVisits]) &&
                    eligible[0].Lines.All(line => line.LineText?.Exists() == true) &&
                    eligible[0].Lines.SkipLast(1).All(line => line.NextButtonText?.Exists() == true),
                "a conversation had incorrect speakers or missing text/Continue localization");
        }
        foreach (int previousVisits in new[] { 3, 4, 10 })
        {
            AncientDialogue[] eligible = set.GetValidDialogues(character, previousVisits, previousVisits, true).ToArray();
            Require(eligible.Contains(conversations[1]) && !eligible.Contains(conversations[0]) &&
                    eligible.Contains(conversations[2]) && eligible.Length == set.AgnosticDialogues.Count + 2,
                "later visits did not use Neow's generic pool plus the repeatable second and third conversations");
        }
        Require(ModelDb.Event<Neow>().DialogueSet.FirstVisitEverDialogue is not null,
            "Sakiko's introduction override changed vanilla Neow's first-ever introduction");

        int visit = int.TryParse(CommandLineHelper.GetValue("togawa-starting-visit"), out int parsed) ? parsed : 1;
        Require(visit is >= 1 and <= 3, "diagnostic visit must be 1, 2, or 3");
        NAncientDialogueLine[] rendered = scene.GetNode<VBoxContainer>("%DialogueContainer")
            .GetChildren().OfType<NAncientDialogueLine>().ToArray();
        Require(rendered.Length == conversations[visit - 1].Lines.Count,
            "the native room did not select the requested visit's conversation");
        NAncientDialogueHitbox next = scene.GetNode<NAncientDialogueHitbox>("%DialogueHitbox");
        for (int index = 0; index < rendered.Length; index++)
        {
            AncientDialogueLine line = (AncientDialogueLine)AccessTools.Field(typeof(NAncientDialogueLine), "_line")
                .GetValue(rendered[index])!;
            Require(ReferenceEquals(line, conversations[visit - 1].Lines[index]),
                "the rendered conversation was reordered or replaced");
            await CaptureIfRequestedAsync(node, cancellationToken, $"dialogue-{visit}-{index + 1}");
            if (index < rendered.Length - 1)
            {
                Require(next.Visible, "Continue was hidden before the end of the conversation");
                next.ForceClick();
                await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        Require(!next.Visible && scene.DefaultFocusedControl is NEventOptionButton,
            "finishing the conversation did not expose the native reward choices");
        NativeSmokeTrace.StartingRoomInfo($"dialogue passed. Visit={visit}, Conversations=3, Speakers=exact, Continue=working, RepeatPool=second-and-third.");
    }

    private static async Task CaptureIfRequestedAsync(NEventRoom node, CancellationToken cancellationToken, string? suffix = null)
    {
        string? path = CommandLineHelper.GetValue("togawa-starting-screenshot");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        Require(DisplayServer.GetName() != "headless", "screenshot capture requires a rendered game window");
        Require(Path.IsPathFullyQualified(path), "screenshot capture requires an absolute output path");
        if (suffix is not null)
        {
            path = Path.Combine(Path.GetDirectoryName(path)!, suffix + ".png");
        }
        ulong settleUntil = Time.GetTicksMsec() + 2000;
        while (Time.GetTicksMsec() < settleUntil)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        await node.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image screenshot = node.GetViewport().GetTexture().GetImage();
        Require(screenshot.GetWidth() > 0 && screenshot.GetHeight() > 0 && screenshot.SavePng(path) == Error.Ok,
            "the rendered viewport screenshot could not be saved");
        NativeSmokeTrace.StartingRoomInfo("screenshot saved to " + path + ".");
    }

    private static InvalidOperationException Failure(string message) =>
        new("Starting room contract failed: " + message + ".");

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw Failure(message);
        }
    }
}

[HarmonyPatch(typeof(EventRoomHandler), nameof(EventRoomHandler.HandleAsync))]
internal static class OceanStartingRoomDiagnosticHandlerPatch
{
    private static bool Prefix(CancellationToken ct, ref Task __result)
    {
        if (!NativeSmokeTrace.AutoSlayEnabled ||
            RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is not EventRoom { CanonicalEvent: OceanOfMemories })
        {
            return true;
        }
        __result = OceanStartingRoomDiagnostics.HandleAsync(ct);
        return false;
    }
}

[HarmonyPatch(
    typeof(Player),
    nameof(Player.CreateForNewRun),
    [typeof(CharacterModel), typeof(UnlockState), typeof(ulong)])]
internal static class OceanStartingRoomDiagnosticUnlockPatch
{
    private static void Prefix(CharacterModel character, ref UnlockState unlockState)
    {
        if (NativeSmokeTrace.StartingRoomEnabled &&
            character is Models.Characters.TogawaSakiko)
        {
            // A fresh isolated profile has not revealed Neow yet; the test needs the normal unlocked starting room.
            unlockState = UnlockState.all;
            if (int.TryParse(CommandLineHelper.GetValue("togawa-starting-visit"), out int visit) && visit > 1)
            {
                AncientStats stats = SaveManager.Instance.Progress.GetOrCreateAncientStats(ModelDb.Event<OceanOfMemories>().Id);
                if (stats.CharStats.All(entry => entry.Character != character.Id))
                {
                    stats.CharStats.Add(new AncientCharacterStats { Character = character.Id, Losses = visit - 1 });
                }
            }
        }
    }
}
