using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class PhantomOfMutsumiCard : TogawaSakikoCard
{
    public PhantomOfMutsumiCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6, upgrade: 2);
        WithBlock(6, upgrade: 2);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await CommonActions.CardBlock(this, play);
        var protection = new ProtectionCard();
        await CardPileCmd.AddGeneratedCardToCombat(protection, PileType.Discard, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Phantom of Mutsumi",
            "Deal !Damage! damage. Gain !Block! Block. Add Protection to discard. Exhaust.");
}
