using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using TogawaSakiko.NativeCode.Commands;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Models.Cards;

public sealed class AveMujicaCard : CardModel
{
    public const int ChoiceCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    public override string PortraitPath => NativeAssetPaths.AveMujicaPortrait;

    public AveMujicaCard()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    internal static CardModel[] SelectOptionCanonicals(Rng rng)
    {
        CardModel[] symbols =
        [
            ModelDb.Card<SymbolIFireCard>(),
            ModelDb.Card<SymbolIIAirCard>(),
            ModelDb.Card<SymbolIIIWaterCard>(),
            ModelDb.Card<SymbolIVEarthCard>(),
            ModelDb.Card<EtherCard>()
        ];
        return symbols.TakeRandom(ChoiceCount, rng).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState
            ?? throw new InvalidOperationException("Ave Mujica requires an active combat.");
        bool upgradeOptions = IsUpgraded || Owner.GetRelic<MoltenEgg>() is not null;
        List<CardModel> options = SelectOptionCanonicals(Owner.RunState.Rng.CombatCardGeneration)
            .Select(canonical => combatState.CreateCard(canonical, Owner))
            .ToList();
        if (upgradeOptions)
        {
            foreach (CardModel option in options)
            {
                CardCmd.Upgrade(option);
            }
        }

        SakikoAudioCmd.TryPlayCardVoice(Owner, "AveMujica");
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            Owner);
        if (selected is null)
        {
            return;
        }

        CardPileAddResult persistent = await PersistentDeckMutation.AddStatEquivalentAsync(
            Owner,
            selected);
        CardCmd.PreviewCardPileAdd(persistent);
        await CardCmd.AutoPlay(choiceContext, selected, cardPlay.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
