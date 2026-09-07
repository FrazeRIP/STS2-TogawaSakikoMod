using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class KillKiSSCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new DynamicVar("MagicNumber", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<DesireCard>()];

    public override string PortraitPath => NativeAssetPaths.KillKiSSPortrait;

    public KillKiSSCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    internal static CardModel[] SelectDesiresToRetrieve(
        IReadOnlyList<CardModel> drawPile,
        IReadOnlyList<CardModel> discardPile,
        int count)
    {
        ArgumentNullException.ThrowIfNull(drawPile);
        ArgumentNullException.ThrowIfNull(discardPile);
        if (count <= 0)
        {
            return [];
        }

        return drawPile
            .Concat(discardPile)
            .Where(card => card is DesireCard)
            .Take(count)
            .ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        CardModel[] desires = SelectDesiresToRetrieve(
            PileType.Draw.GetPile(Owner).Cards,
            PileType.Discard.GetPile(Owner).Cards,
            DynamicVars["MagicNumber"].IntValue);
        foreach (CardModel desire in desires)
        {
            await CardPileCmd.Add(desire, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
