using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class EncoreCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];

    public override string PortraitPath => NativeAssetPaths.SilentFarewellPortrait;

    public EncoreCard()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int curses = Owner.Deck.Cards.Count(card => card.Type == CardType.Curse);
        if (curses > 0)
        {
            await PowerCmd.Apply<HypePower>(
                choiceContext,
                Owner.Creature,
                curses * DynamicVars["MagicNumber"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
