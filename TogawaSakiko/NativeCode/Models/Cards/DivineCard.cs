using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class DivineCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ArdorPower>(),
        HoverTipFactory.FromPower<MelodiaPower>(),
        HoverTipFactory.FromPower<MonsterDivinityPower>()
    ];

    // Temporary portrait until dedicated artwork is supplied.
    public override string PortraitPath => NativeAssetPaths.RaiseTheBetPortrait;

    public DivineCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int ardor = Owner.Creature.GetPower<ArdorPower>()?.Amount ?? 0;
        if (ardor > 0)
        {
            await PowerCmd.Apply<MelodiaPower>(choiceContext, Owner.Creature, ardor, Owner.Creature, this);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars["MagicNumber"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
