using global::Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Presentation.Godot;

public partial class SakikoCreatureVisualsNode : NCreatureVisuals
{
    private Creature? _creature;
    private Sprite2D? _portrait;
    private string? _portraitPath;

    public override void _Ready()
    {
        base._Ready();
        _portrait = GetNode<Sprite2D>("%Visuals");
        _creature = (GetParent() as NCreature)?.Entity;
        if (_creature?.Player is { } player)
        {
            _creature.PowerApplied += OnPowerChanged;
            _creature.PowerRemoved += OnPowerChanged;
            player.RelicObtained += OnRelicChanged;
            player.RelicRemoved += OnRelicChanged;
        }
        RefreshPortrait();
    }

    public override void _ExitTree()
    {
        if (_creature?.Player is { } player)
        {
            _creature.PowerApplied -= OnPowerChanged;
            _creature.PowerRemoved -= OnPowerChanged;
            player.RelicObtained -= OnRelicChanged;
            player.RelicRemoved -= OnRelicChanged;
        }
        _creature = null;
        base._ExitTree();
    }

    private void OnPowerChanged(PowerModel power) => RefreshPortrait();

    private void OnRelicChanged(RelicModel relic) => RefreshPortrait();

    internal static string SelectPortraitPath(Creature? creature)
    {
        if (creature?.HasPower<MonsterDivinityPower>() == true)
        {
            return NativeAssetPaths.CharacterMasterOfMelodiaPortrait;
        }
        return creature?.Player?.GetRelic<AnotherMask>() is { IsMelted: false }
            ? NativeAssetPaths.CharacterMaskedPortrait
            : NativeAssetPaths.CharacterPortrait;
    }

    private void RefreshPortrait()
    {
        string path = SelectPortraitPath(_creature);
        if (_portrait is null || path == _portraitPath)
        {
            return;
        }
        Texture2D texture = ResourceLoader.Load<Texture2D>(path);
        _portrait.Texture = texture;
        // All combat variants share the same ground pivot and visible body height.
        _portrait.Scale = Vector2.One * (320f / texture.GetHeight());
        _portrait.Position = new Vector2(0f, -160f);
        _portraitPath = path;
    }
}
