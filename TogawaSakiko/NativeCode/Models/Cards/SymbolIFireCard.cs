using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class SymbolIFireCard : CardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(20m, ValueProp.Move), new DynamicVar("MagicNumber", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<TimorisCard>()];

    public override string PortraitPath => NativeAssetPaths.SymbolIFirePortrait;

    public SymbolIFireCard()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    internal static CardModel? SelectPreviousAttack(
        IEnumerable<CardPlayFinishedEntry> entries,
        Player owner)
    {
        return entries.LastOrDefault(entry =>
                entry.CardPlay.Player == owner &&
                entry.CardPlay.Card.Type == CardType.Attack &&
                entry.CardPlay.Card is not SymbolIFireCard &&
                !entry.CardPlay.Card.IsDupe)
            ?.CardPlay.Card;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Symbol I: Fire requires an active combat.");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_fire_burst")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);

        CardModel? previousAttack = SelectPreviousAttack(
            CombatManager.Instance.History.CardPlaysFinished,
            Owner);
        if (previousAttack is not null)
        {
            for (int i = 0; i < DynamicVars["MagicNumber"].IntValue; i++)
            {
                if (CombatManager.Instance.IsOverOrEnding)
                {
                    break;
                }

                await CardCmd.AutoPlay(choiceContext, previousAttack.CreateDupe(Owner), null);
            }
        }

        PersistentDeckAndCombatAddResult addition =
            await PersistentDeckMutation.AddCanonicalWithCombatCopyAsync<TimorisCard>(
                Owner,
                combatState,
                PileType.Discard);
        CardCmd.PreviewCardPileAdd(addition.PersistentResult);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
