using Godot;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Presentation.Godot;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N7AudioPureContractTests
{
    private static int _assertionCount;

    public static int AssertionCount => _assertionCount;

    public static void Run()
    {
        _assertionCount = 0;

        Require(SakikoAudioCmd.IsSakikoResourceAudioPath(NativeAssetPaths.CharacterSelectVoice),
            "character-select route is recognized");
        Require(SakikoAudioCmd.IsSakikoResourceAudioPath(NativeAssetPaths.DazzlingImpactSfx),
            "Dazzling route is recognized");
        Require(!SakikoAudioCmd.IsSakikoResourceAudioPath("event:/sfx/characters/defect/defect_select"),
            "native FMOD events are not intercepted");

        Require(SakikoAudioCmd.GetHurtVoicePath(1) == NativeAssetPaths.CharacterHurtVoice1,
            "Hurt1 route");
        Require(SakikoAudioCmd.GetHurtVoicePath(2) == NativeAssetPaths.CharacterHurtVoice2,
            "Hurt2 route");
        Require(SakikoAudioCmd.SelectNextHurtVariant(0, 0) == 1,
            "initial Hurt1 selection");
        Require(SakikoAudioCmd.SelectNextHurtVariant(0, 1) == 2,
            "initial Hurt2 selection");
        Require(SakikoAudioCmd.SelectNextHurtVariant(1, 0) == 2 &&
                SakikoAudioCmd.SelectNextHurtVariant(1, 1) == 2,
            "Hurt1 never repeats");
        Require(SakikoAudioCmd.SelectNextHurtVariant(2, 0) == 1 &&
                SakikoAudioCmd.SelectNextHurtVariant(2, 1) == 1,
            "Hurt2 never repeats");

        string[] requiredAudio =
        [
            NativeAssetPaths.CharacterSelectVoice,
            NativeAssetPaths.CharacterHurtVoice1,
            NativeAssetPaths.CharacterHurtVoice2,
            NativeAssetPaths.CharacterHurtVoice3,
            NativeAssetPaths.DazzlingImpactSfx
        ];
        foreach (string path in requiredAudio)
        {
            Require(ResourceLoader.Exists(path, "AudioStream"), $"loadable audio resource {path}");
        }

        Require(NativeAssetPaths.CharacterHurtVoice3.EndsWith("/hurt3.wav", StringComparison.Ordinal),
            "Hurt3 is preserved as an intentionally unreachable STS1 asset");
        Require(NativeAssetPaths.MusicPulseImpactSfx.EndsWith("/MusicPulseAttackEffect.wav", StringComparison.Ordinal),
            "inactive Music Pulse source asset remains inventoried");
        Require(ResourceLoader.Exists(NativeAssetPaths.DazzlingImpactTexture, "Texture2D"),
            "Dazzling impact texture is loadable");
        Require(SakikoDazzlingImpactVfxNode.DurationSeconds == 0.6f,
            "Dazzling impact keeps the STS1 0.6-second lifetime");
    }

    private static void Require(bool condition, string label)
    {
        _assertionCount++;
        if (!condition)
        {
            throw new InvalidOperationException($"Phase N7 audio contract failed: {label}.");
        }
    }
}
