using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class MementoMoriCard : TogawaSakikoCard
{
    public MementoMoriCard() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(7, upgrade: 3);
        WithVar("RemoveCount", 7, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        //// Remove (exhaust) cards from draw pile
        //int count = DynamicVars["RemoveCount"].IntValue;
        //var drawPile = Owner.DrawPile.ToList();
        //int removed = 0;
        //foreach (var card in drawPile)
        //{
        //    if (removed >= count) break;
        //    await CardPileCmd.Add(card, PileType.Exhaust, skipVisuals: true);
        //    removed++;
        //}
        //await CommonActions.CardAttack(this, play).Execute(ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Memento Mori",
            "Exhaust !RemoveCount! cards from your draw pile. Deal !Damage! damage.");
}
