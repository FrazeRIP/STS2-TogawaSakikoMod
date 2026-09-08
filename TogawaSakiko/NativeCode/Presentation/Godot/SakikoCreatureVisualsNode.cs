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
    private bool _showDeadPortrait;

    public override void _Ready()
    {
        base._Ready();
        _portrait = GetNode<Sprite2D>("%Visuals");
        _creature = (GetParent() as NCreature)?.Entity;
        if (_creature?.Player is { } player)
        {
            _creature.PowerApplied += OnPowerChanged;
            _creature.PowerRemoved += OnPowerChanged;
            _creature.Revived += OnRevived;
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
            _creature.Revived -= OnRevived;
            player.RelicObtained -= OnRelicChanged;
            player.RelicRemoved -= OnRelicChanged;
        }
        _creature = null;
        base._ExitTree();
    }

    private void OnPowerChanged(PowerModel power) => RefreshPortrait();

    private void OnRelicChanged(RelicModel relic) => RefreshPortrait();

    private void OnRevived(Creature creature) => SetDeadPortrait(false);

    internal void SetDeadPortrait(bool dead)
    {
        _showDeadPortrait = dead;
        RefreshPortrait();
    }

    internal static string SelectPortraitPath(Creature? creature)
    {
        if (creature?.IsDead == true)
        {
            return NativeAssetPaths.CharacterDeadPortrait;
        }
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
        if (_showDeadPortrait)
        {
            path = NativeAssetPaths.CharacterDeadPortrait;
        }
        if (_portrait is null || path == _portraitPath)
        {
            return;
        }
        Texture2D texture = ResourceLoader.Load<Texture2D>(path);
        _portrait.Texture = texture;
        // All combat variants share the same ground pivot and visible body height.
        _portrait.Scale = Vector2.One * (320f / texture.GetHeight());
        _portrait.Position = new Vector2(0f, -160f);
        if (path == NativeAssetPaths.CharacterDeadPortrait)
        {
            // The prone source is already at combat scale; do not stretch it to standing height.
            // Center its visible pixels over the existing pivot and place its bottom on the floor.
            using Image pixels = texture.GetImage();
            Rect2I used = pixels.GetUsedRect();
            _portrait.Scale = Vector2.One;
            _portrait.Position = new Vector2(
                texture.GetWidth() * 0.5f - used.Position.X - used.Size.X * 0.5f,
                texture.GetHeight() * 0.5f - used.End.Y);
        }
        _portraitPath = path;
    }
}
