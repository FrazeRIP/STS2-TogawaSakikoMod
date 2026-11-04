using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class BaddyBaddyCard : CardModel
{
    private bool _returnToHand;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 4m)];

    public override string PortraitPath => NativeAssetPaths.EdgeOfBreakdownPortrait;

    public BaddyBaddyCard()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        var normal = base.GetResultLocationForCardPlay();
        _returnToHand = DynamicVars["MagicNumber"].IntValue > 1;
        return _returnToHand && normal.pileType == PileType.Discard
            ? new CardLocation(Owner, PileType.Hand, CardPilePosition.Bottom)
            : normal;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int gain = Math.Max(0, DynamicVars["MagicNumber"].IntValue);
        if (gain > 0)
        {
            await PowerCmd.Apply<ArdorPower>(choiceContext, Owner.Creature, gain, Owner.Creature, this);
        }

        DynamicVars["MagicNumber"].BaseValue = Math.Max(0, gain - 1);
        // Native result location is captured before OnPlay; replays can reduce the amount further.
        if (_returnToHand && DynamicVars["MagicNumber"].IntValue == 0 && Pile?.Type == PileType.Play)
        {
            await CardPileCmd.Add(this, PileType.Discard);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
