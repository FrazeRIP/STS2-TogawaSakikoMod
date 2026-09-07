using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Commands;

internal static class SakikoAudioCmd
{
    private const ulong CardVoiceCooldownMilliseconds = 5_000;
    private static readonly Dictionary<ulong, ulong> LastCardVoiceAtByPlayer = [];

    public static bool TryPlayCardVoice(Player player, string sourceKey)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        if (NonInteractiveMode.IsActive || player.Character is not SakikoCharacter)
        {
            return false;
        }

        ulong now = Time.GetTicksMsec();
        if (LastCardVoiceAtByPlayer.TryGetValue(player.NetId, out ulong lastPlayedAt) &&
            now - lastPlayedAt < CardVoiceCooldownMilliseconds)
        {
            return false;
        }

        string path = GetCardVoicePath(sourceKey);
        AudioStream stream = ResourceLoader.Load<AudioStream>(path)
            ?? throw new InvalidOperationException($"Sakiko card voice is not loadable: {path}.");
        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            return false;
        }

        var playerNode = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "SFX"
        };
        playerNode.Finished += playerNode.QueueFree;
        sceneTree.Root.AddChild(playerNode);
        playerNode.Play();
        LastCardVoiceAtByPlayer[player.NetId] = now;
        return true;
    }

    public static string GetCardVoicePath(string sourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        return $"{Content.NativeAssetPaths.Root}/audio/sakiko/{sourceKey.ToLowerInvariant()}.wav";
    }
}
