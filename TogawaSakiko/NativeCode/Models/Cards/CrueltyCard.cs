using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;
using SakikoCrueltyPower = TogawaSakiko.NativeCode.Models.Powers.CrueltyPower;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class CrueltyCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 8m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<DesireCard>()];

    public override string PortraitPath => NativeAssetPaths.CrueltyPortrait;

    public CrueltyCard()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SakikoAudioCmd.TryPlayCardVoice(Owner, "Cruelty");
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["MagicNumber"].BaseValue,
            ValueProp.Unpowered,
            Owner.Creature);
        if (Owner.Creature.IsDead)
        {
            return;
        }

        await PowerCmd.Apply<SakikoCrueltyPower>(
            choiceContext,
            Owner.Creature,
            2,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
