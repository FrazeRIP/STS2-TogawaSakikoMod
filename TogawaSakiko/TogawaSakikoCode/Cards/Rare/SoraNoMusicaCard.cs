using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Rare;

public class SoraNoMusicaCard : TogawaSakikoCard
{
    public SoraNoMusicaCard() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(12, upgrade: 4);
        WithCards(5, upgrade: 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(ctx);
        await CommonActions.Draw(this, ctx);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Sora no Musica", "Deal !Damage! damage. Draw !Cards! cards. Exhaust.");
}
