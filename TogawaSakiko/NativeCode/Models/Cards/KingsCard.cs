using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class KingsCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.KingsPortrait;

    public KingsCard()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    // Only a real draw of this card adds Tiredness; generation and moves into Hand do not.
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (!ReferenceEquals(card, this))
        {
            return;
        }

        PersistentDeckAndCombatAddResult result =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TirednessCard>(Owner);
        CardCmd.PreviewCardPileAdd(result.PersistentResult);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
