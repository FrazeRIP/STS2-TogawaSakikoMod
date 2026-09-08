using System.IO;
using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Diagnostics;

// Opt-in isolated starting-room harness: --togawa-feedback-visuals --togawa-death-presentation.
internal static class DeathPresentationDiagnostics
{
    internal static async Task RunAsync(RunState runState, CancellationToken cancellationToken)
    {
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Monster, showTransition: false);
        ulong deadline = Time.GetTicksMsec() + 15000;
        while (CombatManager.Instance.IsStarting)
        {
            if (Time.GetTicksMsec() > deadline)
            {
                throw new TimeoutException("Death presentation combat did not start.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        var player = runState.Players.Single();
        var creature = player.Creature;
        NCreature node = NCombatRoom.Instance!.GetCreatureNode(creature)!;
        string livingPath = player.GetRelic<AnotherMask>() is { IsMelted: false }
            ? NativeAssetPaths.CharacterMaskedPortrait : NativeAssetPaths.CharacterPortrait;
        AssertPortrait(node, livingPath);

        // The same burst in regular gameplay must not select the Architect's corpse pose.
        NFireBurstVfx.Create(creature, 1f)?.QueueFree();
        AssertPortrait(node, livingPath);
        foreach (bool master in new[] { false, true })
        {
            if (master)
            {
                await SakikoStanceCmd.EnterDivinityAsync(new BlockingPlayerChoiceContext(), creature, creature, null);
            }
            AssertPortrait(node, master ? NativeAssetPaths.CharacterMasterOfMelodiaPortrait : livingPath);
            node.StartDeathAnim(shouldRemove: false);
            AssertPortrait(node, NativeAssetPaths.CharacterDeadPortrait);
            await SnapshotAsync(master ? "combat-dead-master" : "combat-dead", cancellationToken);
            // Keep the test's frame waits outside zero HP so combat cannot auto-advance dead-player turns.
            creature.SetCurrentHpInternal(0);
            AssertPortrait(node, NativeAssetPaths.CharacterDeadPortrait);
            // Exercise the native heal/revive fade, including its delayed ImmediatelySetIdle callback.
            await CreatureCmd.Heal(creature, creature.MaxHp);
            await WaitFramesAsync(700, cancellationToken);
            AssertPortrait(node, master ? NativeAssetPaths.CharacterMasterOfMelodiaPortrait : livingPath);
            if (master)
            {
                await PowerCmd.Remove(creature.GetPower<MonsterDivinityPower>());
            }
        }

        // Loading a visual for an already-dead creature must select the corpse without an animation call.
        creature.SetCurrentHpInternal(0);
        var loadedVisuals = creature.CreateVisuals()!;
        node.AddChild(loadedVisuals);
        AssertSprite(loadedVisuals.GetNode<Sprite2D>("%Visuals"), NativeAssetPaths.CharacterDeadPortrait);
        loadedVisuals.QueueFree();
        await CreatureCmd.Heal(creature, creature.MaxHp);
        await WaitFramesAsync(700, cancellationToken);

        // Use the real Architect room and its native attack sequence; do not kill the player model.
        await RunManager.Instance.EnterRoomDebug(RoomType.Event, model: ModelDb.Event<TheArchitect>(), showTransition: false);
        node = NCombatRoom.Instance!.GetCreatureNode(creature)!;
        AssertPortrait(node, livingPath);
        var architect = (TheArchitect)((EventRoom)runState.CurrentRoom!).LocalMutableEvent;
        int hpBefore = creature.CurrentHp;
        var attack = AccessTools.Method(typeof(TheArchitect), "AnimArchitectAttackIfNecessary");
        await (Task)attack.Invoke(architect, [ArchitectAttackers.None])!;
        AssertPortrait(node, livingPath);
        await (Task)attack.Invoke(architect, [ArchitectAttackers.Both])!;
        AssertPortrait(node, NativeAssetPaths.CharacterDeadPortrait);
        if (creature.CurrentHp != hpBefore || creature.IsDead)
        {
            throw new InvalidOperationException("Architect corpse presentation changed player health.");
        }
        await SnapshotAsync("architect-dead", cancellationToken);
        await RunManager.Instance.EnterRoomDebug(RoomType.Monster, MapPointType.Monster, showTransition: false);
        await WaitFramesAsync(1500, cancellationToken);
        node = NCombatRoom.Instance!.GetCreatureNode(creature)!;
        // Debug room entry omits the encounter ID that normal map entry records for defeat statistics.
        runState.CurrentMapPointHistoryEntry!.Rooms.Last().ModelId = runState.CurrentRoom!.ModelId;
        await CreatureCmd.Kill(creature, force: true);
        AssertPortrait(node, NativeAssetPaths.CharacterDeadPortrait);
        await SnapshotAsync("gameplay-defeat", cancellationToken);
        NativeSmokeTrace.StartingRoomInfo("death presentation passed. Combat=base+master, Revive=restored, LoadedDead=corpse, Architect=corpse+hp-preserved, OrdinaryBurst=preserved.");
    }

    private static void AssertPortrait(NCreature node, string expected) =>
        AssertSprite(node.Visuals.GetNode<Sprite2D>("%Visuals"), expected);

    private static void AssertSprite(Sprite2D sprite, string expected)
    {
        if (sprite.Texture?.ResourcePath != expected)
        {
            throw new InvalidOperationException($"Death presentation expected {expected}, got {sprite.Texture?.ResourcePath}.");
        }
        if (expected == NativeAssetPaths.CharacterDeadPortrait)
        {
            using Image pixels = sprite.Texture.GetImage();
            Rect2I used = pixels.GetUsedRect();
            float bottom = sprite.Position.Y + used.End.Y - sprite.Texture.GetHeight() * 0.5f;
            if (sprite.Scale != Vector2.One || Mathf.Abs(bottom) > 0.01f)
            {
                throw new InvalidOperationException("Corpse was stretched or detached from the ground pivot.");
            }
        }
    }

    private static async Task WaitFramesAsync(ulong milliseconds, CancellationToken cancellationToken)
    {
        ulong deadline = Time.GetTicksMsec() + milliseconds;
        while (Time.GetTicksMsec() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task SnapshotAsync(string name, CancellationToken cancellationToken)
    {
        if (DisplayServer.GetName() != "headless" && CommandLineHelper.GetValue("togawa-feedback-output") is { } output)
        {
            Directory.CreateDirectory(output);
            await FeedbackVisualDiagnostics.SnapshotAsync(Path.Combine(output, name + ".png"), cancellationToken);
        }
    }
}
