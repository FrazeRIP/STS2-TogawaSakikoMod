using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class RhinocerosBeetleCard : CardModel
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DazzlingPower>()];

    public override string PortraitPath => NativeAssetPaths.RhinocerosBeetlePortrait;

    public RhinocerosBeetleCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "RhinocerosBeetle");
        await PowerCmd.Apply<DazzlingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            Owner.Creature,
            this);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            Owner.Creature.GetPower<DazzlingPower>()?.Amount ?? 0,
            ValueProp.Unpowered,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
