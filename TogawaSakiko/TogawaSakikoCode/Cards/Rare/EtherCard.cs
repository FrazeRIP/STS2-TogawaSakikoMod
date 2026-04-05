using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Curse;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

/// <summary>
/// In STS1: hit 2 dmg per exhausted card (up to 12 hits); add Oblivionis.
/// TODO: complex exhaust-count mechanic. Simplified: deal 2 dmg x 12 hits.
/// </summary>
public class EtherCard : TogawaSakikoCard
{
    public EtherCard() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: 12).Execute(ctx);
        var oblivionis = new OblivionisCard();
        await CardPileCmd.AddGeneratedCardToCombat(oblivionis, PileType.Discard, addedByPlayer: false);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Ether",
            "Hit 12 times for !Damage! damage. Add Oblivionis (curse) to discard. (TODO: hits = exhausted cards)");
}
