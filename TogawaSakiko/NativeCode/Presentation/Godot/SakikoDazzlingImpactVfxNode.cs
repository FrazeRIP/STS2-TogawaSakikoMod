using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Presentation.Godot;

public partial class SakikoDazzlingImpactVfxNode : Node2D
{
    internal const float DurationSeconds = 0.6f;

    private Creature? _target;
    private bool _playSound;

    public static SakikoDazzlingImpactVfxNode? Create(Creature target, bool playSound = true)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
        {
            return null;
        }

        return new SakikoDazzlingImpactVfxNode
        {
            _target = target,
            _playSound = playSound
        };
    }

    public override void _Ready()
    {
        NCreature? targetNode = NCombatRoom.Instance?.GetCreatureNode(_target);
        if (targetNode is null)
        {
            QueueFree();
            return;
        }

        Texture2D texture = ResourceLoader.Load<Texture2D>(NativeAssetPaths.DazzlingImpactTexture)
            ?? throw new InvalidOperationException(
                $"Sakiko Dazzling texture is not loadable: {NativeAssetPaths.DazzlingImpactTexture}.");

        GlobalPosition = targetNode.VfxSpawnPosition;
        var sprite = new Sprite2D
        {
            Texture = texture,
            Centered = true
        };
        AddChild(sprite);

        targetNode.Visuals.TryApplyLiquidOverlay(Colors.Yellow);
        if (_playSound)
        {
            SakikoAudioCmd.TryPlayDazzlingImpact();
        }

        Tween tween = CreateTween();
        tween.TweenInterval(DurationSeconds * 0.5f);
        tween.TweenProperty(sprite, "modulate:a", 0f, DurationSeconds * 0.5f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
