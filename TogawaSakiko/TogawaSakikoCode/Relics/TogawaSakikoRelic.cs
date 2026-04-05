using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using TogawaSakiko.TogawaSakikoCode.Character;
using TogawaSakiko.TogawaSakikoCode.Extensions;
using Godot;

namespace TogawaSakiko.TogawaSakikoCode.Relics;

[Pool(typeof(TogawaSakikoRelicPool))]
public abstract class TogawaSakikoRelic : CustomRelicModel
{
    public override string PackedIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
            return ResourceLoader.Exists(path) ? path : "relic.png".RelicImagePath();
        }
    }

    protected override string PackedIconOutlinePath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
            return ResourceLoader.Exists(path) ? path : "relic_outline.png".RelicImagePath();
        }
    }

    protected override string BigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
            return ResourceLoader.Exists(path) ? path : "relic.png".BigRelicImagePath();
        }
    }
}