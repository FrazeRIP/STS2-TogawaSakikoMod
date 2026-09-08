using System.IO;
using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
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
    internal static readonly string[] ChoiceKeys = ["ANOTHER_MASK", "THE_THIRD_MOVEMENT", "BLAZING_HAIRBAND"];

    internal static async Task HandleAsync(CancellationToken cancellationToken)
    {
        RunState runState = RunManager.Instance.DebugOnlyGetState()
            ?? throw Failure("there was no active run");
        EventRoom room = runState.CurrentRoom as EventRoom
            ?? throw Failure("the starting room was not an event");
        OceanOfMemories ocean = room.LocalMutableEvent as OceanOfMemories
            ?? throw Failure("Sakiko's starting room was not Ocean of Memories");
        Player player = ocean.Owner!;
        NEventRoom node = NEventRoom.Instance ?? throw Failure("the event room node was missing");
        OceanOfMemoriesScene scene = node.CustomEventNode as OceanOfMemoriesScene
            ?? throw Failure("the custom full-screen layout was missing");

        // Broader gameplay diagnostics require the unchanged starter relic and starter deck.
        if (!NativeSmokeTrace.StartingRoomEnabled)
        {
            scene.OnOptionClicked(ocean.CurrentOptions[1], 1);
            await RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks();
            // Keep earlier combat contracts at their original damage baseline after exercising the new choice.
            await RelicCmd.Remove(player.Relics.OfType<TheThirdMovement>().Single());
            await NEventRoom.Proceed();
            return;
        }

        int choice = int.TryParse(CommandLineHelper.GetValue("togawa-starting-choice"), out int parsed)
            ? parsed : 0;
        Require(choice is >= 0 and < 3, "choice index must be 0, 1, or 2");
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
        TextureRect artwork = scene.GetNode<TextureRect>("Artwork");
        Require(artwork.Texture is not null && artwork.Texture.GetWidth() > 0 && artwork.Texture.GetHeight() > 0,
            "the full-screen artwork did not resolve");
        Require(artwork.Size.IsEqualApprox(scene.Size) && scene.Size.X > 0 && scene.Size.Y > 0,
            "the artwork did not fill the room");
        Require(scene.GetNode<VBoxContainer>("Content/Options").GetChildren().OfType<NEventOptionButton>().Count() == 3,
            "the custom layout did not create three native option buttons");
        await CaptureIfRequestedAsync(node, cancellationToken);

        // Exercise the same routed native button entry point used by the visible UI.
        node.OptionButtonClicked(initialOptions[choice], choice);
        await RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks();
        Require(ocean.IsFinished && room.IsPreFinished, "choosing an option did not finish the native ancient room");
        VerifyChoice(player, choice);
        foreach (EventOption staleOption in initialOptions)
        {
            await staleOption.Chosen();
        }
        if (choice == 0)
        {
            await player.Relics.OfType<AnotherMask>().Single().AfterObtained();
        }
        VerifyChoice(player, choice);

        await SaveManager.Instance.SaveRun(room, saveProgress: false);
        ReadSaveResult<SerializableRun> read = SaveManager.Instance.LoadRunSave();
        Require(read.Success && read.SaveData is not null, "the completed starting room save could not be read");
        SerializableRun saved = read.SaveData!;
        Require(saved.PreFinishedRoom is { IsPreFinished: true } &&
                saved.PreFinishedRoom.EventId == ocean.Id,
            "the native save did not preserve the finished custom starting room");
        var choices = saved.MapPointHistory.SelectMany(act => act)
            .SelectMany(point => point.PlayerStats).SelectMany(stats => stats.AncientChoices)
            .Where(entry => entry.Title.LocEntryKey.StartsWith(OceanOfMemories.Entry + ".", StringComparison.Ordinal))
            .ToArray();
        Require(choices.Length == 3 && choices.Count(entry => entry.WasChosen) == 1 &&
                choices.Single(entry => entry.WasChosen).TextKey == ChoiceKeys[choice],
            "the save did not preserve exactly one chosen option in native ancient history");

        RunState reloadedRun = RunState.FromSerializable(saved);
        Player reloadedPlayer = reloadedRun.Players.Single();
        VerifyChoice(reloadedPlayer, choice);
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
        VerifyChoice(reloadedPlayer, choice);
        NativeSmokeTrace.StartingRoomInfo(
            $"passed. Choice={ChoiceKeys[choice]}, Deck=9, Repeat=blocked, SavedChoice=exact, Reload=finished, ThirdMovementUses={(choice == 1 ? 3 : 0)}, Vanilla=preserved, FullscreenArtwork=resolved.");

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
            1 => player.Relics.Count == 2 && starter == 1 && blazing == 0 && movement?.RemainingUses == 3,
            2 => player.Relics.Count == 1 && starter == 0 && blazing == 1 && movement is null,
            _ => false
        }, "the chosen relic inventory or Third Movement charges were not exact");
    }

    private static async Task CaptureIfRequestedAsync(NEventRoom node, CancellationToken cancellationToken)
    {
        string? path = CommandLineHelper.GetValue("togawa-starting-screenshot");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        Require(DisplayServer.GetName() != "headless", "screenshot capture requires a rendered game window");
        Require(Path.IsPathFullyQualified(path), "screenshot capture requires an absolute output path");
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
        }
    }
}
