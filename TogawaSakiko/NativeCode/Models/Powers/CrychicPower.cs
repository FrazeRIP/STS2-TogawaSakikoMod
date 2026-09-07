using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class CrychicPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    internal static IReadOnlyList<CardModel> PhantomCanonicals =>
    [
        ModelDb.Card<PhantomOfMutsumiCard>(),
        ModelDb.Card<PhantomOfSakikoCard>(),
        ModelDb.Card<PhantomOfSoyoCard>(),
        ModelDb.Card<PhantomOfTakiCard>(),
        ModelDb.Card<PhantomOfTomoriCard>()
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
        {
            return;
        }

        Flash();
        CardModel? canonical = player.RunState.Rng.CombatCardGeneration.NextItem(PhantomCanonicals);
        await PowerCmd.Decrement(this);
        ICombatState? combatState = player.Creature.CombatState;
        if (canonical is null || combatState is null || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        CardModel phantom = combatState.CreateCard(canonical, player);
        phantom.EnergyCost.SetThisTurnOrUntilPlayed(0);
        await CardPileCmd.AddGeneratedCardToCombat(phantom, PileType.Hand, player);
    }
}
