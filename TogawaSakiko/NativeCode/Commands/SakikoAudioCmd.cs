using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using TogawaSakiko.NativeCode.Content;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Commands;

internal static class SakikoAudioCmd
{
    private const ulong CardVoiceCooldownMilliseconds = 5_000;
    private const ulong HurtVoiceCooldownMilliseconds = 1_000;
    private static readonly Dictionary<ulong, ulong> LastCardVoiceAtByPlayer = [];
    private static readonly Dictionary<ulong, ulong> LastHurtVoiceAtByPlayer = [];
    private static readonly Dictionary<ulong, int> LastHurtVariantByPlayer = [];
    private static Player? _voiceOwner;

    public static bool TryPlayCardVoice(Player player, string sourceKey)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        if (!ShouldPlayOwnerVoice(player.Character is SakikoCharacter, player.NetId, LocalContext.NetId))
        {
            return false;
        }

        EnsureVoiceOwner(player);
        ulong now = Time.GetTicksMsec();
        if (LastCardVoiceAtByPlayer.TryGetValue(player.NetId, out ulong lastPlayedAt) &&
            now - lastPlayedAt < CardVoiceCooldownMilliseconds)
        {
            return false;
        }

        string path = GetCardVoicePath(sourceKey);
        if (!TryPlayResourcePath(path))
        {
            return false;
        }

        LastCardVoiceAtByPlayer[player.NetId] = now;
        return true;
    }

    public static bool TryPlayCharacterSelectVoice()
    {
        return TryPlayResourcePath(NativeAssetPaths.CharacterSelectVoice);
    }

    public static bool TryPlayHurtVoice(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!ShouldPlayOwnerVoice(player.Character is SakikoCharacter, player.NetId, LocalContext.NetId))
        {
            return false;
        }

        EnsureVoiceOwner(player);
        ulong now = Time.GetTicksMsec();
        if (LastHurtVoiceAtByPlayer.TryGetValue(player.NetId, out ulong lastPlayedAt) &&
            now - lastPlayedAt < HurtVoiceCooldownMilliseconds)
        {
            return false;
        }

        int previousVariant = LastHurtVariantByPlayer.GetValueOrDefault(player.NetId);
        int variant = SelectNextHurtVariant(previousVariant, Random.Shared.Next(2));
        if (!TryPlayResourcePath(GetHurtVoicePath(variant)))
        {
            return false;
        }

        LastHurtVariantByPlayer[player.NetId] = variant;
        LastHurtVoiceAtByPlayer[player.NetId] = now;
        return true;
    }

    public static bool TryPlayDazzlingImpact()
    {
        return TryPlayResourcePath(NativeAssetPaths.DazzlingImpactSfx);
    }

    // Card and hurt voices belong to the local Sakiko. Remote voices are silent;
    // shared Dazzling impact presentation still plays once in each peer's scene.
    internal static bool ShouldPlayOwnerVoice(bool isSakiko, ulong ownerId, ulong? localId) =>
        isSakiko && localId.HasValue && ownerId == localId.Value;

    internal static void ResetVoiceState()
    {
        LastCardVoiceAtByPlayer.Clear();
        LastHurtVoiceAtByPlayer.Clear();
        LastHurtVariantByPlayer.Clear();
        _voiceOwner = null;
    }

    private static void EnsureVoiceOwner(Player player)
    {
        if (!ReferenceEquals(_voiceOwner, player))
        {
            ResetVoiceState();
            _voiceOwner = player;
        }
    }

    public static bool TryPlayResourcePath(string path, float volume = 1f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
        {
            return false;
        }

        AudioStream stream = ResourceLoader.Load<AudioStream>(path)
            ?? throw new InvalidOperationException($"Sakiko audio resource is not loadable: {path}.");
        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            return false;
        }

        var playerNode = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "SFX",
            VolumeDb = Mathf.LinearToDb(Mathf.Max(volume, 0.0001f))
        };
        playerNode.Finished += playerNode.QueueFree;
        sceneTree.Root.AddChild(playerNode);
        playerNode.Play();
        return true;
    }

    public static string GetCardVoicePath(string sourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        return $"{NativeAssetPaths.Root}/audio/sakiko/{sourceKey.ToLowerInvariant()}.wav";
    }

    public static string GetHurtVoicePath(int variant)
    {
        return variant switch
        {
            1 => NativeAssetPaths.CharacterHurtVoice1,
            2 => NativeAssetPaths.CharacterHurtVoice2,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant,
                "STS1's reachable hurt-voice range is 1 through 2.")
        };
    }

    public static int SelectNextHurtVariant(int previousVariant, int randomBit)
    {
        if (randomBit is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(randomBit));
        }

        return previousVariant switch
        {
            1 => 2,
            2 => 1,
            _ => randomBit + 1
        };
    }

    public static bool IsSakikoResourceAudioPath(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               path.StartsWith(NativeAssetPaths.Root + "/audio/", StringComparison.Ordinal);
    }
}
