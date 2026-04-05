using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class OurSongCard : TogawaSakikoCard
{
    public OurSongCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<OurSongPower>(this, 1m);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.Upgrade(); // 1 → 0
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Our Song", "Apply 1 Our Song: whenever you deal attack damage, gain equal Block.");
}
