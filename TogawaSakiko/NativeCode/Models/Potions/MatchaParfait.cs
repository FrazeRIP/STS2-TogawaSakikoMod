using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Potions;

public sealed class MatchaParfait : SakikoPotionModel
{
    internal const int MinimumDamage = 6;
    internal const int MaximumDamage = 30;

    internal override string AssetFolder => "matchaparfait";

    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Potency", 1m)];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        ICombatState? combatState = target.CombatState;
        if (combatState is null)
        {
            return;
        }

        for (int index = 0; index < DynamicVars["Potency"].IntValue; index++)
        {
            MelodyCard melody = combatState.CreateCard<MelodyCard>(Owner);
            melody.DynamicVars.Damage.BaseValue = Owner.RunState.Rng.CombatCardGeneration.NextInt(
                MinimumDamage,
                MaximumDamage + 1);
            await CardPileCmd.AddGeneratedCardToCombat(melody, PileType.Hand, Owner);
        }
    }

    internal static bool IsDamageInRange(decimal damage)
    {
        return damage >= MinimumDamage && damage <= MaximumDamage;
    }
}
