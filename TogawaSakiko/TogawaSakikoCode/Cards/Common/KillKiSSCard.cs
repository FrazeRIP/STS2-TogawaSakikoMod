using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Cards.Token;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Common;

public class KillKiSSCard : TogawaSakikoCard
{
    public KillKiSSCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(9, upgrade: 3);
        WithVar("DesireCount", 1, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        int count = DynamicVars["DesireCount"].IntValue;
        for (int i = 0; i < count; i++)
        {
            var desire = new DesireCard();
            await CardPileCmd.AddGeneratedCardToCombat(desire, PileType.Hand, addedByPlayer: true);
        }
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Kill Ki$$", "Deal !Damage! damage. Add !DesireCount! Desire card(s) to your hand.");
}
