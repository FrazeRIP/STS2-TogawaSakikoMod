using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.TogawaSakikoCode.Cards.Curse;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class SymbolIFireCard : TogawaSakikoCard
{
    public SymbolIFireCard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(20, upgrade: 5);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var timoris = new TimorisCard();
        await CardPileCmd.AddGeneratedCardToCombat(timoris, PileType.Discard, addedByPlayer: false);
        if (IsUpgraded)
        {
            var enemies = CombatState?.Enemies ?? Enumerable.Empty<Creature>();
            foreach (var enemy in enemies)
            {
                await PowerCmd.Apply<VulnerablePower>(enemy, 1, Owner.Creature, this);
            }
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Symbol I: Fire",
            "Deal !Damage! damage to ALL enemies. Add Timoris (curse) to discard. Upgraded: apply 1 Vulnerable to all.");
}
