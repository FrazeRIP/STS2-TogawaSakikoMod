using Godot;
using System.Text.Json;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Cards;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;
using GameLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class N4PresentationDiagnostics
{
    public const string CatalogPath = "res://TogawaSakiko/diagnostics/n4_presentation_catalog.json";
    public const string CharacterManifestPath = "res://TogawaSakiko/diagnostics/n4_character_presentation_manifest.json";
    public const int ExpectedTextureCount = 355;
    public const int ExpectedAudioCount = 52;
    public const int ExpectedResourceCount = 11;
    public const int ExpectedLocalizationFileCount = 17;

    private static readonly GameLogger Logger = new(Bootstrap.ModEntryPoint.ModId, LogType.Generic);

    public static void RunIfRequested()
    {
        if (!NativeSmokeTrace.CatalogEnabled)
        {
            return;
        }

        using Godot.FileAccess? file = Godot.FileAccess.Open(CatalogPath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            throw new InvalidOperationException($"Phase N4 presentation catalog is missing: {CatalogPath}.");
        }

        using JsonDocument document = JsonDocument.Parse(file.GetAsText());
        JsonElement root = document.RootElement;
        int textureCount = ValidateTextures(root.GetProperty("textures"));
        int audioCount = ValidateAudio(root.GetProperty("audio"));
        int resourceCount = ValidateResources(root.GetProperty("resources"));
        int localizationFileCount = ValidateFiles(root.GetProperty("localizationFiles"));

        AssertCount("textures", textureCount, ExpectedTextureCount);
        AssertCount("audio resources", audioCount, ExpectedAudioCount);
        AssertCount("standalone resources", resourceCount, ExpectedResourceCount);
        AssertCount("localization files", localizationFileCount, ExpectedLocalizationFileCount);
        ValidateCharacterPresentation();

        Logger.Info(
            $"Phase N4 presentation catalog passed. Textures={textureCount}, Audio={audioCount}, Resources={resourceCount}, LocalizationFiles={localizationFileCount}, CharacterSceneContracts=5, CharacterTextureContracts=12");
    }

    private static int ValidateTextures(JsonElement records)
    {
        int count = 0;
        foreach (JsonElement record in records.EnumerateArray())
        {
            string path = record.GetProperty("path").GetString()
                ?? throw new InvalidOperationException("A Phase N4 texture path is null.");
            int expectedWidth = record.GetProperty("width").GetInt32();
            int expectedHeight = record.GetProperty("height").GetInt32();
            string owner = record.GetProperty("owner").GetString() ?? "unknown";
            if (!ResourceLoader.Exists(path, "Texture2D"))
            {
                throw new InvalidOperationException($"Phase N4 texture is not loadable for {owner}: {path}.");
            }

            Texture2D texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse)
                ?? throw new InvalidOperationException($"Phase N4 texture loaded as null for {owner}: {path}.");
            if (texture.GetWidth() != expectedWidth || texture.GetHeight() != expectedHeight)
            {
                throw new InvalidOperationException(
                    $"Phase N4 texture dimensions changed for {owner}: {path}; expected {expectedWidth}x{expectedHeight}, found {texture.GetWidth()}x{texture.GetHeight()}.");
            }
            count++;
        }
        return count;
    }

    private static int ValidateAudio(JsonElement records)
    {
        int count = 0;
        foreach (JsonElement record in records.EnumerateArray())
        {
            string path = record.GetProperty("path").GetString()
                ?? throw new InvalidOperationException("A Phase N4 audio path is null.");
            if (!ResourceLoader.Exists(path, "AudioStream"))
            {
                throw new InvalidOperationException($"Phase N4 audio resource is not loadable: {path}.");
            }

            AudioStream stream = ResourceLoader.Load<AudioStream>(path, null, ResourceLoader.CacheMode.Reuse)
                ?? throw new InvalidOperationException($"Phase N4 audio resource loaded as null: {path}.");
            if (stream.GetLength() <= 0d)
            {
                throw new InvalidOperationException($"Phase N4 audio resource has no playable duration: {path}.");
            }
            count++;
        }
        return count;
    }

    private static int ValidateResources(JsonElement records)
    {
        int count = 0;
        foreach (JsonElement record in records.EnumerateArray())
        {
            string path = record.GetProperty("path").GetString()
                ?? throw new InvalidOperationException("A Phase N4 resource path is null.");
            if (!ResourceLoader.Exists(path))
            {
                throw new InvalidOperationException($"Phase N4 standalone resource is not loadable: {path}.");
            }

            Resource resource = ResourceLoader.Load(path, null, ResourceLoader.CacheMode.Reuse)
                ?? throw new InvalidOperationException($"Phase N4 standalone resource loaded as null: {path}.");
            count++;
        }
        return count;
    }

    private static int ValidateFiles(JsonElement paths)
    {
        int count = 0;
        foreach (JsonElement element in paths.EnumerateArray())
        {
            string path = element.GetString()
                ?? throw new InvalidOperationException("A Phase N4 localization file path is null.");
            if (!Godot.FileAccess.FileExists(path))
            {
                throw new InvalidOperationException($"Phase N4 localization file is not mounted: {path}.");
            }
            count++;
        }
        return count;
    }

    private static void AssertCount(string label, int actual, int expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Phase N4 presentation catalog expected {expected} {label}, found {actual}.");
        }
    }

    private static void ValidateCharacterPresentation()
    {
        if (!Godot.FileAccess.FileExists(CharacterManifestPath))
        {
            throw new InvalidOperationException($"Phase N4 character presentation manifest is not mounted: {CharacterManifestPath}.");
        }

        SakikoCharacter character = ModelDb.Character<SakikoCharacter>();
        AssertPath("combat visuals", character.AssetPaths.First(), NativeAssetPaths.CharacterVisualsPreload);
        AssertPath("energy counter", character.EnergyCounterPath, NativeAssetPaths.CharacterEnergyCounter);
        AssertPath("merchant", character.MerchantAnimPath, NativeAssetPaths.CharacterMerchant);
        AssertPath("rest site", character.RestSiteAnimPath, NativeAssetPaths.CharacterRestSite);
        AssertPath("card trail", character.TrailPath, NativeAssetPaths.CharacterTrail);
        AssertPath("character-select background", character.CharacterSelectBg, NativeAssetPaths.CharacterSelectBackground);
        AssertPath("character-select transition", character.CharacterSelectTransitionPath, NativeAssetPaths.CharacterTransitionMaterial);

        TogawaSakikoCardPool cardPool = ModelDb.CardPool<TogawaSakikoCardPool>();
        AssertPath("energy icon", EnergyIconHelper.GetPath(cardPool), NativeAssetPaths.EnergyIcon);
        AssertPath("card-frame material", cardPool.FrameMaterialPath, NativeAssetPaths.CardFrameMaterial);
        AssertPath("attack card frame", ModelDb.Card<StrikeTogawaSakiko>().Frame.ResourcePath, NativeAssetPaths.AttackCardFrame);
        AssertPath("skill card frame", ModelDb.Card<DefendTogawaSakiko>().Frame.ResourcePath, NativeAssetPaths.SkillCardFrame);
        AssertPath("power card frame", ModelDb.Card<WorldviewCard>().Frame.ResourcePath, NativeAssetPaths.PowerCardFrame);

        ValidateScene<NCreatureVisuals>(NativeAssetPaths.CharacterVisualsPreload, root =>
        {
            RequireNode<Sprite2D>(root, "%Visuals");
            RequireNode<Control>(root, "%FormVfx");
            RequireNode<Control>(root, "%Bounds");
            RequireNode<Marker2D>(root, "%CenterPos");
            RequireNode<Marker2D>(root, "%IntentPos");
            RequireNode<Marker2D>(root, "%TalkPos");
        });

        ValidateScene<NEnergyCounter>(NativeAssetPaths.CharacterEnergyCounter, root =>
        {
            RequireNode<Control>(root, "%Layers");
            RequireNode<Control>(root, "%RotationLayers");
            RequireNode<Node>(root, "%EnergyVfxBack");
            RequireNode<Node>(root, "%EnergyVfxFront");
            RequireNode<Label>(root, "Label");
        });

        ValidateScene<NRestSiteCharacter>(NativeAssetPaths.CharacterRestSite, root =>
        {
            RequireNode<Sprite2D>(root, "%Visuals");
            RequireNode<Control>(root, "ControlRoot");
            RequireNode<NSelectionReticle>(root, "%SelectionReticle");
            RequireNode<Control>(root, "%Hitbox");
            RequireNode<Control>(root, "%ThoughtBubbleLeft");
            RequireNode<Control>(root, "%ThoughtBubbleRight");
        });

        ValidateScene<NMerchantCharacter>(NativeAssetPaths.CharacterMerchant, root =>
        {
            if (root.GetChildCount() == 0 || root.GetChild(0) is not Sprite2D)
            {
                throw new InvalidOperationException("Sakiko's static merchant scene must have its portrait as the first child.");
            }
        });

        ValidateScene<NCardTrailVfx>(NativeAssetPaths.CharacterTrail, root =>
        {
            RequireNode<Node2D>(root, "Trails");
            RequireNode<Node2D>(root, "Sprites");
        });

        _ = LoadRequired<ShaderMaterial>(NativeAssetPaths.CharacterTransitionMaterial);
        _ = LoadRequired<ShaderMaterial>(NativeAssetPaths.CardFrameMaterial);

        AssertTextureSize(NativeAssetPaths.CharacterIcon, 88, 88);
        AssertTextureSize(NativeAssetPaths.CharacterIconOutline, 88, 88);
        AssertTextureSize(NativeAssetPaths.CharacterSelectIcon, 132, 195);
        AssertTextureSize(NativeAssetPaths.CharacterSelectLockedIcon, 132, 195);
        AssertTextureSize(NativeAssetPaths.CharacterMapMarker, 49, 64);
        AssertTextureSize(NativeAssetPaths.EnergyIcon, 71, 72);
        AssertVisibleExtent(NativeAssetPaths.EnergyIcon, 64);
        AssertTextureSize(NativeAssetPaths.AttackCardFrame, 598, 844);
        AssertTextureSize(NativeAssetPaths.SkillCardFrame, 598, 844);
        AssertTextureSize(NativeAssetPaths.PowerCardFrame, 598, 844);
        AssertVisibleExtent(NativeAssetPaths.MonochromeHairbandIcon, 100);
        AssertVisibleExtent(NativeAssetPaths.MonochromeHairbandBigIcon, 235);
        AssertTextureSize(NativeAssetPaths.RichTextEnergyIcon, 24, 24);
        AssertTextureSize(NativeAssetPaths.CharacterTransitionTexture, 2560, 1200);
        AssertTextureSize(NativeAssetPaths.CharacterArmPoint, 422, 1200);
        AssertTextureSize(NativeAssetPaths.CharacterArmRock, 422, 1200);
        AssertTextureSize(NativeAssetPaths.CharacterArmPaper, 422, 1200);
        AssertTextureSize(NativeAssetPaths.CharacterArmScissors, 422, 1200);
    }

    private static void ValidateScene<T>(string path, Action<T> assertion) where T : Node
    {
        PackedScene scene = LoadRequired<PackedScene>(path);
        T root = scene.Instantiate<T>(PackedScene.GenEditState.Disabled);
        try
        {
            assertion(root);
        }
        finally
        {
            root.Free();
        }
    }

    private static T RequireNode<T>(Node root, string path) where T : Node
    {
        return root.GetNodeOrNull<T>(path)
            ?? throw new InvalidOperationException($"Phase N4 scene {root.Name} is missing required {typeof(T).Name} node {path}.");
    }

    private static T LoadRequired<T>(string path) where T : Resource
    {
        return ResourceLoader.Load<T>(path, null, ResourceLoader.CacheMode.Reuse)
            ?? throw new InvalidOperationException($"Phase N4 resource did not load as {typeof(T).Name}: {path}.");
    }

    private static void AssertTextureSize(string path, int width, int height)
    {
        Texture2D texture = LoadRequired<Texture2D>(path);
        if (texture.GetWidth() != width || texture.GetHeight() != height)
        {
            throw new InvalidOperationException(
                $"Phase N4 character texture dimensions changed: {path}; expected {width}x{height}, found {texture.GetWidth()}x{texture.GetHeight()}.");
        }
    }

    private static void AssertPath(string label, string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Phase N4 {label} path mismatch: expected {expected}, found {actual}.");
        }
    }

    private static void AssertVisibleExtent(string path, int minimumExtent)
    {
        using Image image = LoadRequired<Texture2D>(path).GetImage();
        Rect2I bounds = image.GetUsedRect();
        if (Math.Max(bounds.Size.X, bounds.Size.Y) < minimumExtent)
        {
            throw new InvalidOperationException($"Presentation texture is visibly undersized: {path}; bounds={bounds}, required extent={minimumExtent}.");
        }
    }
}
