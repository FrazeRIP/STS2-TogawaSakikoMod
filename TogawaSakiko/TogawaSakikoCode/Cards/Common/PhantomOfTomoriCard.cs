using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class PhantomOfTomoriCard : TogawaSakikoCard
{
    public PhantomOfTomoriCard() : base(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
        WithDamage(14, upgrade: 4);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        var voice = new VoiceCard();
        await CardPileCmd.AddGeneratedCardToCombat(voice, PileType.Discard, addedByPlayer: true);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Phantom of Tomori", "Deal !Damage! damage to a random enemy. Add Voice to discard. Exhaust.");
}
