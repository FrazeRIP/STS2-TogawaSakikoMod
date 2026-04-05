using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

/// <summary>
/// @CardEnable=false in STS1. Simplified: remove 1 Vulnerable from all enemies.
/// </summary>
public class AuthorityRestorationCard : TogawaSakikoCard
{
    public AuthorityRestorationCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithKeywords(CardKeyword.Exhaust);
        // Upgrade: cost → 0
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var enemies = CombatState?.Enemies ?? Enumerable.Empty<Creature>();
        foreach (var enemy in enemies)
        {
            var vuln = enemy.Powers.FirstOrDefault(p => p is VulnerablePower);
            if (vuln != null)
                await PowerCmd.ModifyAmount(vuln, -1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 1 → 0
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Authority Restoration", "Remove 1 Vulnerable from all enemies. Exhaust.");
}
