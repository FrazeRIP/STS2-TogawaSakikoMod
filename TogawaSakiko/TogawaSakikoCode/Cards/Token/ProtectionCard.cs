using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class ProtectionCard : TogawaSakikoCard
{
    public ProtectionCard() : base(0, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<ArtifactPower>(2, upgrade: 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<ArtifactPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Protection", "Gain !ArtifactPower! Artifact.");
}
