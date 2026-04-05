using BaseLib.Abstracts;
using TogawaSakiko.TogawaSakikoCode.Extensions;
using Godot;

namespace TogawaSakiko.TogawaSakikoCode.Character;

public class TogawaSakikoRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => TogawaSakiko.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}