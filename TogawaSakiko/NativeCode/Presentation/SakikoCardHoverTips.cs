using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using TogawaSakiko.NativeCode.Models.Cards;

namespace TogawaSakiko.NativeCode.Presentation;

internal sealed class RotatingSakikoCardHoverTip : CardHoverTip
{
    internal IReadOnlyList<CardModel> Variants { get; }

    internal RotatingSakikoCardHoverTip(IReadOnlyList<CardModel> variants) : base(variants[0])
    {
        Variants = variants;
    }
}

internal static class SakikoCardHoverTips
{
    internal static IEnumerable<IHoverTip> Append(CardModel card, IEnumerable<IHoverTip> current)
    {
        List<IHoverTip> tips = current.ToList();
        CardModel display = card.IsMutable ? card : card.ToMutable();
        string text = display.GetDescriptionForPile(PileType.Deck);
        if (Regex.IsMatch(text, @"\bPurg(?:e|ed|ing)\b|\bRemove(?:d)?\b[^.\n]*\bcards?\b|移除", RegexOptions.IgnoreCase))
        {
            tips.Add(Keyword("PURGE"));
        }
        if (Regex.IsMatch(text, @"\bScry\b|预见", RegexOptions.IgnoreCase))
        {
            tips.Add(Keyword("SCRY"));
        }
        if (text.Contains("[gold]Strength[/gold]", StringComparison.Ordinal) ||
            text.Contains("[gold]力量[/gold]", StringComparison.Ordinal))
        {
            tips.Add(HoverTipFactory.FromPower<StrengthPower>());
        }
        if (card is AveMujicaCard or CrychicCard)
        {
            bool elements = card is AveMujicaCard;
            string keyword = elements ? "SYMBOL" : "PHANTOMS";
            tips.Add(Keyword(keyword + (card.IsUpgraded ? "_PLUS" : "")));
            tips.Add(new RotatingSakikoCardHoverTip(CreateVariantPreviews(elements, card.IsUpgraded)));
        }
        if (card is PhantomOfSakikoCard or PhantomOfMutsumiCard or PhantomOfTomoriCard or PhantomOfSoyoCard or PhantomOfTakiCard)
        {
            // STS1 upgraded each Phantom's preview together with its generated card.
            tips = tips.Select(tip => tip is CardHoverTip preview && card.IsUpgraded
                ? HoverTipFactory.FromCard(preview.Card.CanonicalInstance, upgrade: true)
                : tip).ToList();
        }
        return IHoverTip.RemoveDupes(tips);
    }

    internal static IReadOnlyList<CardModel> CreateVariantPreviews(bool elements, bool upgraded)
    {
        CardModel[] canonicals = elements
            ? [ModelDb.Card<SymbolIFireCard>(), ModelDb.Card<SymbolIIAirCard>(), ModelDb.Card<SymbolIIIWaterCard>(), ModelDb.Card<SymbolIVEarthCard>(), ModelDb.Card<EtherCard>()]
            : [ModelDb.Card<PhantomOfMutsumiCard>(), ModelDb.Card<PhantomOfSakikoCard>(), ModelDb.Card<PhantomOfSoyoCard>(), ModelDb.Card<PhantomOfTakiCard>(), ModelDb.Card<PhantomOfTomoriCard>()];
        return canonicals.Select(canonical => ((CardHoverTip)HoverTipFactory.FromCard(canonical, upgraded)).Card).ToArray();
    }

    private static IHoverTip Keyword(string key) => new HoverTip(
        new LocString("card_keywords", "TOGAWASAKIKO-" + key + ".title"),
        new LocString("card_keywords", "TOGAWASAKIKO-" + key + ".description"));
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
internal static class SakikoCardHoverTipsPatch
{
    private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance.GetType().Namespace == "TogawaSakiko.NativeCode.Models.Cards")
        {
            __result = SakikoCardHoverTips.Append(__instance, __result);
        }
    }
}

[HarmonyPatch(typeof(NHoverTipCardContainer), nameof(NHoverTipCardContainer.Add))]
internal static class SakikoRotatingCardPreviewPatch
{
    private static void Postfix(NHoverTipCardContainer __instance, CardHoverTip cardTip)
    {
        if (cardTip is not RotatingSakikoCardHoverTip rotating || rotating.Variants.Count < 2)
        {
            return;
        }
        Control holder = __instance.GetChildren().OfType<Control>().Last();
        NCard card = holder.GetNode<NCard>("%Card");
        int index = 0;
        global::Godot.Timer timer = new() { WaitTime = 1.0, OneShot = false };
        timer.Timeout += () =>
        {
            index = (index + 1) % rotating.Variants.Count;
            card.Model = rotating.Variants[index];
            card.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
        };
        holder.AddChild(timer);
        timer.Start();
    }
}
