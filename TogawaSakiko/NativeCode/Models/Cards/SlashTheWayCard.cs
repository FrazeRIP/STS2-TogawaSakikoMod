using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Powers;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SlashTheWayCard : CardModel
{
    public override string PortraitPath => NativeAssetPaths.BlackKeysPortrait;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3m, ValueProp.Move), new RepeatVar(3)];

    public SlashTheWayCard()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        if (!CombatManager.Instance.IsOverOrEnding)
        {
            await PowerCmd.Apply<ArdorPower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1);
    }
}
