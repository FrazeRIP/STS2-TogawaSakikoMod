using BaseLib.Abstracts;
using BaseLib.Utils;
using TogawaSakiko.TogawaSakikoCode.Character;

namespace TogawaSakiko.TogawaSakikoCode.Potions;

[Pool(typeof(TogawaSakikoPotionPool))]
public abstract class TogawaSakikoPotion : CustomPotionModel;