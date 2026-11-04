using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SoftnessCard : CardModel
{
    public override bool GainsBlock => true;

    public override string PortraitPath => NativeAssetPaths.PassionPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    public SoftnessCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var ardor = Owner.Creature.GetPower<ArdorPower>();
        int amount = ardor?.Amount ?? 0;
        if (amount <= 0)
        {
            return;
        }

        await PowerCmd.Remove(ardor!);
        await NextTurnArdorPower.AddAsync(choiceContext, Owner.Creature, amount, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
