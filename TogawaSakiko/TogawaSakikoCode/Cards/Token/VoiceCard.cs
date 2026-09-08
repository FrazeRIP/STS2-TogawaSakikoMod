using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Token;

public class VoiceCard : TogawaSakikoCard
{
    public VoiceCard() : base(1, CardType.Power, CardRarity.Token, TargetType.Self, autoAdd: false)
    {
        WithPower<StrengthPower>(4, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.ApplySelf<StrengthPower>(this);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Voice", "Gain !StrengthPower! Strength.");
}
