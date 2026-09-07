using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Potions;

public sealed class ChocolateMilkJelly : SakikoPotionModel
{
    internal override string AssetFolder => "chocolatemilkjelly";

    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Potency", 1m)];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<FreeAttackPower>(
            choiceContext,
            target,
            DynamicVars["Potency"].BaseValue,
            Owner.Creature,
            null);
    }
}
