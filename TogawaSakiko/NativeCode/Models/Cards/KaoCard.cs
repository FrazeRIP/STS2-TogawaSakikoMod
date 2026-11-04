using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class KaoCard : CardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.KaoPortrait;

    public KaoCard()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    internal CardModel[] GetDesires()
    {
        return SakikoPurgeCommand.GetCandidates(Owner).Where(card => card is DesireCard).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = GetDesires().TakeRandom(1, Owner.RunState.Rng.CombatCardSelection).FirstOrDefault();
        if (selected is not null)
        {
            await SakikoPurgeCommand.RemoveAsync(selected);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState ?? throw new InvalidOperationException("Kao requires combat."))
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
