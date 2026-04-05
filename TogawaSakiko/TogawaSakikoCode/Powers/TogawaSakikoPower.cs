using BaseLib.Abstracts;
using BaseLib.Extensions;
using TogawaSakiko.TogawaSakikoCode.Extensions;
using Godot;

namespace TogawaSakiko.TogawaSakikoCode.Powers;

public abstract class TogawaSakikoPower : CustomPowerModel
{
    //Loads from TogawaSakiko/images/powers/your_power.png
    public override string CustomPackedIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
            return ResourceLoader.Exists(path) ? path : "power.png".PowerImagePath();
        }
    }

    public override string CustomBigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
            return ResourceLoader.Exists(path) ? path : "power.png".BigPowerImagePath();
        }
    }
}