using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>Native replay playback of the explicit Vine Shambler multiplayer contract fixture.</summary>
internal static class MultiplayerReplayDiagnostics
{
    internal static async Task RunAsync(string replayPath, int playerIndex)
    {
        string recordedPackage = (await System.IO.File.ReadAllTextAsync(replayPath + ".package")).Trim();
        Require(recordedPackage == NativePackageIdentity.HandshakeEntry,
            "replay was recorded with a different Togawa package");
        PacketReader reader = new();
        reader.Reset(await System.IO.File.ReadAllBytesAsync(replayPath));
        CombatReplay replay = reader.Read<CombatReplay>();
        Require(replay.version == ReleaseInfoManager.Instance.ReleaseInfo?.Version,
            "replay game version differs from the installed game");
        Require(replay.gitCommit == ReleaseInfoManager.Instance.ReleaseInfo?.Commit,
            "replay game commit differs from the installed game");
        Require(replay.modelIdHash == ModelIdSerializationCache.Hash,
            "replay model database differs from the loaded package");
        Require(replay.events.Count > 0 && replay.checksumData.Count > 0,
            "empty replay cannot establish gameplay playback");
        Require(playerIndex >= 0 && playerIndex < replay.serializableRun.Players.Count,
            "replay player index is outside the saved party");

        RunState state = RunState.FromSerializable(replay.serializableRun);
        RunManager manager = RunManager.Instance;
        ulong localId = state.Players[playerIndex].NetId;
        manager.SetUpReplay(state, replay, localId);
        // The base game's NMultiplayerTest.RunReplay uses this native replay policy: no live peers
        // supply room-state snapshots, while ChecksumTracker still verifies every recorded state.
        manager.CombatStateSynchronizer.IsDisabled = true;
        manager.ChecksumTracker.IsEnabled = true;
        Require(manager.NetService.Type == NetGameType.Replay && !TestMode.IsOn,
            "playback must use the real native replay service outside TestMode");
        await PreloadManager.LoadRunAssets(state.Players.Select(player => player.Character));
        await PreloadManager.LoadActAssets(state.Act);
        manager.Launch();
        NGame.Instance!.RootSceneContainer.SetCurrentScene(NRun.Create(state));
        await manager.GenerateMap();

        Dictionary<uint, ReplayChecksumData> expected = replay.checksumData.ToDictionary(entry => entry.checksumData.id);
        List<object> checksums = [];
        List<uint> observedIds = [];
        List<string> failures = [];
        List<GameAction> submitted = [];
        GameAction? lastFinishedAction = null;
        NetFullCombatState? lastObservedState = null;
        manager.ChecksumTracker.ChecksumGenerated += (checksum, context, fullState) =>
        {
            observedIds.Add(checksum.id);
            lastObservedState = fullState;
            if (!expected.TryGetValue(checksum.id, out ReplayChecksumData recorded))
            {
                failures.Add($"unexpected checksum {checksum.id} ({context})");
                return;
            }

            // Native files anonymize player IDs in fullState; the historical integer can therefore
            // differ. Recompute with the native algorithm, exactly as CheckAgainstReplayChecksum does.
            uint expectedValue = manager.ChecksumTracker.GenerateChecksum(recorded.fullState);
            bool matched = checksum.checksum == expectedValue;
            checksums.Add(new
            {
                Id = checksum.id,
                Actual = checksum.checksum,
                ExpectedAnonymized = expectedValue,
                OriginalRecorded = recorded.checksumData.checksum,
                Context = context,
                Matched = matched
            });
            if (!matched)
            {
                failures.Add($"checksum {checksum.id} differs: actual={checksum.checksum}, expected={expectedValue}");
                System.IO.File.WriteAllText(replayPath + $".player-{playerIndex}.checksum-{checksum.id}.txt",
                    "ACTUAL\n" + fullState + "\nEXPECTED\n" + recorded.fullState);
            }
        };
        manager.ActionExecutor.AfterActionExecuted += action =>
        {
            lastFinishedAction = action;
            if (action.Exception is { } failure)
            {
                failures.Add($"action {action.Id} failed: {failure}");
            }
        };

        manager.ActionQueueSet.FastForwardNextActionId(replay.nextActionId);
        manager.ActionQueueSynchronizer.FastForwardHookId(replay.nextHookId);
        manager.ChecksumTracker.LoadReplayChecksums(replay.checksumData, replay.nextChecksumId);
        manager.PlayerChoiceSynchronizer.FastForwardChoiceIds(replay.choiceIds);
        manager.RewardsSetSynchronizer.FastForwardRewardIds(replay.rewardIds);
        GD.Print($"Multiplayer replay: reconstructing player {playerIndex}, {replay.events.Count} events, {expected.Count} checksums.");
        // This harness records via EnterRoomDebug, whose initial save precedes appending the debug
        // room. Recreate that exact entry instead of inventing a normal map-coordinate visit.
        await manager.EnterRoomDebug(RoomType.Monster,
            model: ModelDb.Encounter<VineShamblerNormal>().ToMutable(), showTransition: false);
        await WaitAsync(() => !manager.ActionExecutor.IsPaused &&
                              manager.ActionQueueSynchronizer.CombatState == ActionSynchronizerCombatState.PlayPhase,
            "replay first play phase");

        foreach (CombatReplayEvent entry in replay.events)
        {
            switch (entry.eventType)
            {
                case CombatReplayEventType.GameAction:
                    await WaitAsync(() => !CombatManager.Instance.EndingPlayerTurnPhaseOne &&
                                          !CombatManager.Instance.EndingPlayerTurnPhaseTwo,
                        "replay turn boundary");
                    Player owner = state.GetPlayer(entry.playerId!.Value)
                        ?? throw new InvalidOperationException("Replay action owner is absent from the saved party.");
                    GameAction action = entry.action!.ToGameAction(owner);
                    if (action.ActionType == GameActionType.CombatPlayPhaseOnly)
                    {
                        await WaitAsync(() => CombatManager.Instance.DebugOnlyGetState()?.CurrentSide == CombatSide.Player,
                            "replay player side");
                    }
                    submitted.Add(action);
                    manager.ActionQueueSet.EnqueueWithoutSynchronizing(action);
                    if (action is EndPlayerTurnAction or ReadyToBeginEnemyTurnAction)
                    {
                        await manager.ActionExecutor.FinishedExecutingActions().WaitAsync(TimeSpan.FromSeconds(60));
                    }
                    break;
                case CombatReplayEventType.HookAction:
                    GameAction hook = manager.ActionQueueSynchronizer.GetHookActionForId(
                        entry.hookId!.Value, entry.playerId!.Value, entry.gameActionType!.Value);
                    submitted.Add(hook);
                    manager.ActionQueueSet.EnqueueWithoutSynchronizing(hook);
                    break;
                case CombatReplayEventType.ResumeAction:
                    manager.ActionQueueSet.ResumeActionWithoutSynchronizing(entry.actionId!.Value);
                    break;
                case CombatReplayEventType.PlayerChoice:
                    Player chooser = state.GetPlayer(entry.playerId!.Value)
                        ?? throw new InvalidOperationException("Replay chooser is absent from the saved party.");
                    manager.PlayerChoiceSynchronizer.ReceiveReplayChoice(chooser,
                        entry.choiceId!.Value, entry.playerChoiceResult!.Value);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported native replay event {entry.eventType}.");
            }
        }

        await Task.WhenAll(submitted.Select(action => action.CompletionTask)).WaitAsync(TimeSpan.FromSeconds(120));
        await manager.ActionExecutor.FinishedExecutingActions().WaitAsync(TimeSpan.FromSeconds(60));
        Require(observedIds.SequenceEqual(replay.checksumData.Select(entry => entry.checksumData.id)),
            "generated checksum IDs/order/count differ from the entire recording");
        Require(failures.Count == 0, string.Join("; ", failures));
        Require(lastObservedState is not null && lastFinishedAction is not null,
            "no final executed action/state was observed");
        NetFullCombatState finalState = NetFullCombatState.FromRun(state, lastFinishedAction);
        uint finalExpected = manager.ChecksumTracker.GenerateChecksum(replay.checksumData[^1].fullState);
        uint finalActual = manager.ChecksumTracker.GenerateChecksum(finalState);
        Require(finalActual == finalExpected, "final native combat state changed after the last recorded checksum");
        PacketWriter writer = new();
        finalState.Serialize(writer);
        writer.ZeroByteRemainder();
        string finalSha = Convert.ToHexString(SHA256.HashData(writer.Buffer.AsSpan(0, writer.BytePosition)));
        await System.IO.File.WriteAllTextAsync(replayPath + $".player-{playerIndex}.result.json",
            JsonSerializer.Serialize(new
            {
                Passed = true,
                PlayerIndex = playerIndex,
                LocalNetId = localId,
                PackageIdentity = recordedPackage,
                NativeReplayService = true,
                TestMode = TestMode.IsOn,
                RecordedEventCount = replay.events.Count,
                CompletedActionCount = submitted.Count,
                NativeChecksumCount = checksums.Count,
                Checksums = checksums,
                FinalNativeChecksum = finalActual,
                FinalNativeStateSha256 = finalSha,
                Scope = "Full playback of the recorded debug combat, including all custom game actions; excludes post-recording rewards and reconnect."
            }, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print($"Multiplayer replay PASSED: player {playerIndex}, {checksums.Count} native checksums, final={finalActual}.");
    }

    private static async Task WaitAsync(Func<bool> predicate, string stage)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        while (!predicate())
        {
            Require(timeout.Elapsed < TimeSpan.FromSeconds(60), "timed out during " + stage);
            await NGame.Instance!.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Multiplayer replay failed: " + message);
        }
    }
}
