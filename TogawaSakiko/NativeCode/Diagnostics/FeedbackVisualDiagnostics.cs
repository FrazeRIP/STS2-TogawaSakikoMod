using System.IO;
using System.Threading;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;
using TogawaSakiko.NativeCode.Patches;

namespace TogawaSakiko.NativeCode.Diagnostics;

// Explicit visual-smoke runs use an isolated profile and never run during ordinary play.
internal static class FeedbackVisualDiagnostics
{
    internal static async Task CaptureAsync(RunState runState, CancellationToken cancellationToken)
    {
        if (DisplayServer.GetName() == "headless")
        {
            throw new InvalidOperationException("Feedback visual capture requires a rendered window.");
        }
        string output = CommandLineHelper.GetValue("togawa-feedback-output")
            ?? throw new InvalidOperationException("Feedback visual capture requires an output directory.");
        if (!Path.IsPathFullyQualified(output))
        {
            throw new InvalidOperationException("Feedback visual output must be an absolute path.");
        }
        Directory.CreateDirectory(output);
        await SnapshotAsync(Path.Combine(output, "neow-gift-done.png"), cancellationToken);
        await RunManager.Instance.EnterRoomDebug(RoomType.Shop, MapPointType.Shop, showTransition: false);
        await SnapshotAsync(Path.Combine(output, "sakiko-merchant.png"), cancellationToken);
        await RunManager.Instance.EnterRoomDebug(RoomType.RestSite, MapPointType.RestSite, showTransition: false);
        await SnapshotAsync(Path.Combine(output, "sakiko-campfire.png"), cancellationToken);
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Monster, showTransition: false);
        ulong combatStartDeadline = Time.GetTicksMsec() + 15000;
        while (CombatManager.Instance.IsStarting && Time.GetTicksMsec() < combatStartDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        await SnapshotAsync(Path.Combine(output, "sakiko-combat-initial.png"), cancellationToken);
        var player = runState.Players.Single();
        var context = new BlockingPlayerChoiceContext();
        string previousPortrait = player.Relics.Any(relic => relic is AnotherMask)
            ? NativeAssetPaths.CharacterMaskedPortrait : NativeAssetPaths.CharacterPortrait;
        AssertPortrait(player.Creature, previousPortrait);
        await SakikoStanceCmd.EnterDivinityAsync(context, player.Creature, player.Creature, null);
        AssertPortrait(player.Creature, NativeAssetPaths.CharacterMasterOfMelodiaPortrait);
        await SnapshotAsync(Path.Combine(output, "sakiko-master-of-melodia.png"), cancellationToken);
        await PowerCmd.Remove(player.Creature.GetPower<MonsterDivinityPower>());
        AssertPortrait(player.Creature, previousPortrait);
        await SnapshotAsync(Path.Combine(output, "sakiko-portrait-restored.png"), cancellationToken);
        var combat = (runState.CurrentRoom as CombatRoom
            ?? throw new InvalidOperationException("Feedback visual capture did not enter combat.")).CombatState;
        var enemy = combat.HittableEnemies.First();
        var kao = combat.CreateCard<KaoCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(kao, PileType.Hand, player);
        await PowerCmd.Apply<StrengthPower>(context, enemy, 1m, enemy, null);
        await SnapshotAsync(Path.Combine(output, "sakiko-kao-modified-damage.png"), cancellationToken);
        if (kao.DynamicVars.Damage.BaseValue != 5m || kao.DynamicVars.Damage.PreviewValue <= 5m)
        {
            throw new InvalidOperationException("Live hand Kao did not refresh its modified damage preview.");
        }
        var oblivion = combat.CreateCard<OblivionisCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(oblivion, PileType.Hand, player);
        if (!kao.ShouldGlowRed)
        {
            throw new InvalidOperationException("An attack in hand did not warn about Oblivionis exhaust.");
        }
        await SnapshotAsync(Path.Combine(output, "sakiko-kao-oblivion-warning.png"), cancellationToken);
        await SakikoPresentationDiagnostics.ValidateRotationAsync(NGame.Instance!, cancellationToken);
        var submenu = NRun.Instance!.GlobalUi.SubmenuStack;
        submenu.ShowScreen(CapstoneSubmenuType.Compendium);
        NCardLibrary library = submenu.Stack.GetSubmenuType<NCardLibrary>();
        library.Initialize(runState);
        submenu.Stack.Push(library);
        NCardPoolFilter filter = library.GetNode<NCardPoolFilter>(SakikoCardLibraryPatch.FilterPath);
        if (!filter.IsSelected || !filter.IsVisibleInTree())
        {
            throw new InvalidOperationException("The Sakiko card-library filter was not visible and selected.");
        }
        await SnapshotAsync(Path.Combine(output, "sakiko-card-library.png"), cancellationToken);
        submenu.Stack.Pop();
        NRelicCollection relics = submenu.Stack.PushSubmenuType<NRelicCollection>();
        await SnapshotAsync(Path.Combine(output, "sakiko-relic-collection.png"), cancellationToken);
        if (relics.FindChild("TogawaSakikoAncientRelics", recursive: true, owned: false) is null)
        {
            throw new InvalidOperationException("The starting relics were absent from the native collection.");
        }
        NativeSmokeTrace.StartingRoomInfo("feedback visual rooms captured.");
    }

    private static void AssertPortrait(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature, string expected)
    {
        Sprite2D portrait = NCombatRoom.Instance!.GetCreatureNode(creature)!.Visuals.GetNode<Sprite2D>("%Visuals");
        if (portrait.Texture?.ResourcePath != expected)
        {
            throw new InvalidOperationException($"Combat portrait did not switch to {expected}; got {portrait.Texture?.ResourcePath}.");
        }
    }

    internal static async Task SnapshotAsync(string path, CancellationToken cancellationToken)
    {
        Node node = NGame.Instance ?? throw new InvalidOperationException("Game viewport is unavailable.");
        ulong settleUntil = Time.GetTicksMsec() + 1500;
        while (Time.GetTicksMsec() < settleUntil)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        await node.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image screenshot = node.GetViewport().GetTexture().GetImage();
        if (screenshot.SavePng(path) != Error.Ok)
        {
            throw new InvalidOperationException("Could not save feedback screenshot: " + path);
        }
    }
}
