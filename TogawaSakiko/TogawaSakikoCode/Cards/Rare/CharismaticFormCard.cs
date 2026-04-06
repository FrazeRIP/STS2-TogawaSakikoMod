using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class CharismaticFormCard : TogawaSakikoCard
{
    public CharismaticFormCard() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<VulnerablePower>(2, upgrade: 0);
        // Upgrade adds Innate keyword
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Apply Vulnerable to all enemies
        var enemies = CombatState?.Enemies ?? Enumerable.Empty<Creature>();
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<VulnerablePower>(enemy, DynamicVars.Power<VulnerablePower>().IntValue, Owner.Creature, this);
        }
    }
    // TODO: OnUpgrade not supported in STS2. Upgrade: WithKeywords(CardKeyword.Innate);

    public override List<(string, string)>? Localization =>
        new CardLoc("Charismatic Form",
            "Apply !VulnerablePower! Vulnerable to ALL enemies. Upgraded: Innate.");
}
