using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Models.Powers;

public sealed class AllWillsBecomeOnePower : PowerModel
{
    private sealed class Data
    {
        public readonly HashSet<string> Names = new();

        public readonly List<string> Titles = new();
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override int DisplayAmount => IsMutable ? GetInternalData<Data>().Names.Count : 0;

    public override LocString Description => PopulateDescription(base.Description);

    protected override object InitInternalData()
    {
        return new Data();
    }

    internal LocString PopulateDescription(LocString text)
    {
        text.Add("Count", DisplayAmount);
        text.Add(
            "Cards",
            IsMutable && GetInternalData<Data>().Titles.Count > 0
                ? string.Join("、", GetInternalData<Data>().Titles)
                : "—");
        return text;
    }

    internal static async Task AddAsync(PlayerChoiceContext context, CardModel source)
    {
        AllWillsBecomeOnePower? existing = source.Owner.Creature.GetPower<AllWillsBecomeOnePower>();
        if (existing is not null)
        {
            return;
        }

        await PowerCmd.Apply<AllWillsBecomeOnePower>(
            context,
            source.Owner.Creature,
            1,
            source.Owner.Creature,
            source);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner ||
            cardPlay.Resources.EnergyValue != 0 ||
            cardPlay.Card is AllWillsBecomeOneCard)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (!data.Names.Add(cardPlay.Card.Id.ToString()))
        {
            return;
        }

        data.Titles.Add(cardPlay.Card.Title);
        InvokeDisplayAmountChanged();
        if (data.Names.Count < 10)
        {
            return;
        }

        data.Names.Clear();
        data.Titles.Clear();
        var player = Owner.Player!;
        var combatState = Owner.CombatState!;
        await PowerCmd.Remove(this);
        var card = combatState.CreateCard<MelodyCard>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
    }
}
