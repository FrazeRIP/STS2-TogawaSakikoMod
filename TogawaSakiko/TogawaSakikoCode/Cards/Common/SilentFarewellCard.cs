using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class SilentFarewellCard : TogawaSakikoCard
{
    public SilentFarewellCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
        WithPower<VulnerablePower>(1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var enemies = CombatState?.Enemies ?? Enumerable.Empty<Creature>();
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<VulnerablePower>(enemy, DynamicVars.Power<VulnerablePower>().IntValue, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Silent Farewell", "Apply !VulnerablePower! Vulnerable to ALL enemies.");
}
