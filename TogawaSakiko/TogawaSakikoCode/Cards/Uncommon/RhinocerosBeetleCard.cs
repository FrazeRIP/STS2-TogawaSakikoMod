using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.TogawaSakikoCode.Powers;

namespace TogawaSakiko.TogawaSakikoCode.Cards.Uncommon;

public class RhinocerosBeetleCard : TogawaSakikoCard
{
    public RhinocerosBeetleCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(4,
            (card, target) =>
            {
                var dazzling = card.Owner?.Creature.Powers.FirstOrDefault(p => p is DazzlingPower);
                return dazzling != null ? dazzling.Amount / 2m : 0m;
            },
            BlockProps.card, upgrade: 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }

    public override List<(string, string)>? Localization =>
        new CardLoc("Rhinoceros Beetle", "Gain Block equal to 4 + half your Dazzling stacks.");
}
