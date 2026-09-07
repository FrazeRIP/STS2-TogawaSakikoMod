using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Potions;

public sealed class HallucinationPotion : SakikoPotionModel
{
    internal override string AssetFolder => "hallucinationpotion";

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Potency", 5m)];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        decimal potency = DynamicVars["Potency"].BaseValue;
        await PowerCmd.Apply<DazzlingPower>(choiceContext, target, potency, Owner.Creature, null);
        await PowerCmd.Apply<DazzlingDownPower>(choiceContext, target, potency, Owner.Creature, null);
    }
}
