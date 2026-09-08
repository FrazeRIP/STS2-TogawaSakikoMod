using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Diagnostics;
using TogawaSakiko.NativeCode.Presentation.Godot;

namespace TogawaSakiko.NativeCode.Presentation;

internal static class NativeCharacterVisualFactory
{
    public static NCreatureVisuals Create()
    {
        SakikoCreatureVisualsNode root = new()
        {
            Name = "TogawaSakiko"
        };

        AddOwnedChild(root, new Control
        {
            Name = "FormVfx",
            UniqueNameInOwner = true,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });

        AddOwnedChild(root, new Sprite2D
        {
            Name = "Visuals",
            UniqueNameInOwner = true,
            Position = new Vector2(0f, -160f),
            Texture = ResourceLoader.Load<Texture2D>(NativeAssetPaths.CharacterPortrait)
        });

        AddOwnedChild(root, new Control
        {
            Name = "Bounds",
            UniqueNameInOwner = true,
            Position = new Vector2(-84f, -320f),
            Size = new Vector2(168f, 320f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        });

        AddOwnedChild(root, new Marker2D
        {
            Name = "CenterPos",
            UniqueNameInOwner = true,
            Position = new Vector2(0f, -160f)
        });

        AddOwnedChild(root, new Marker2D
        {
            Name = "IntentPos",
            UniqueNameInOwner = true,
            Position = new Vector2(20f, -340f)
        });

        AddOwnedChild(root, new Marker2D
        {
            Name = "TalkPos",
            UniqueNameInOwner = true,
            Position = new Vector2(0f, -350f)
        });

        NativeSmokeTrace.Info("created the static combat portrait.");
        return root;
    }

    private static void AddOwnedChild(Node root, Node child)
    {
        root.AddChild(child);
        child.Owner = root;
    }
}
