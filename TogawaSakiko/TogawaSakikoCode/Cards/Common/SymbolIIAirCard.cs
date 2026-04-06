using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Curse;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class SymbolIIAirCard : TogawaSakikoCard
{
    public SymbolIIAirCard() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(14, upgrade: 4);
        WithCards(3, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await CommonActions.Draw(this, ctx);
        var amoris = new AmorisCard();
        await CardPileCmd.AddGeneratedCardToCombat(amoris, PileType.Discard, addedByPlayer: false);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Symbol II: Air", "Deal !Damage! damage. Draw !Cards! cards. Add Amoris (curse) to discard.");
}
