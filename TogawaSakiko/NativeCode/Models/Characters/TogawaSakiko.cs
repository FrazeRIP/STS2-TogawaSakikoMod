using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Models.Characters;

public sealed class TogawaSakiko : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Feminine;

    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override Color NameColor => new("8295A8");

    public override int StartingHp => 72;

    public override int StartingGold => 50;

    public override CardPoolModel CardPool => ModelDb.CardPool<TogawaSakikoCardPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TogawaSakikoPotionPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TogawaSakikoRelicPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<StrikeTogawaSakiko>(),
        ModelDb.Card<TheMoonlightSonataCard>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>(),
        ModelDb.Card<DefendTogawaSakiko>()
    };

    public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
    {
        ModelDb.Relic<StarterRelicTogawaSakiko>()
    };

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.25f;

    public override Color EnergyLabelOutlineColor => new("1D5673FF");

    public override Color DialogueColor => new("283D4F");

    public override VfxColor SpeechBubbleColor => VfxColor.Cyan;

    public override Color MapDrawingColor => new("8295A8");

    public override Color RemoteTargetingLineColor => new("AFC3D7FF");

    public override Color RemoteTargetingLineOutline => new("1D5673FF");

    protected override string IconPath => NativeAssetPaths.CharacterIconScene;

    protected override string CharacterSelectIconPath => NativeAssetPaths.CharacterSelectIcon;

    protected override string CharacterSelectLockedIconPath => NativeAssetPaths.CharacterSelectLockedIcon;

    protected override string MapMarkerPath => NativeAssetPaths.CharacterMapMarker;

    public override string CharacterSelectSfx => NativeAssetPaths.CharacterSelectVoice;

    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_defect";

    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
