using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class CountingStarsCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("MagicNumber", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip, HoverTipFactory.FromPower<DazzlingPower>()];

    public override string PortraitPath => NativeAssetPaths.CountingStarsPortrait;

    public CountingStarsCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "CountingStars");
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Energy.BaseValue,
            Owner.Creature,
            this);

        int buffCount = Owner.Creature.Powers.Count(power => power.TypeForCurrentAmount == PowerType.Buff);
        if (buffCount > 0)
        {
            await PowerCmd.Apply<DazzlingPower>(
                choiceContext,
                Owner.Creature,
                buffCount * DynamicVars["MagicNumber"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
