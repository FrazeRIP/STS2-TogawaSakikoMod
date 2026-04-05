using BaseLib.Abstracts;
using Godot;
using TogawaSakiko.TogawaSakikoCode.Extensions;

namespace TogawaSakiko.TogawaSakikoCode.Character;

public class TogawaSakikoRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => TogawaSakiko.CharacterId;
    public override Color LabOutlineColor => TogawaSakiko.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}