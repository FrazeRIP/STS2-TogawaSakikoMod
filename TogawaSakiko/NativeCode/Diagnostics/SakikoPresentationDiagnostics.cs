using System.Threading;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Cards;
using TogawaSakiko.NativeCode.Models.Pools;
using TogawaSakiko.NativeCode.Models.Powers;
using TogawaSakiko.NativeCode.Presentation;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static class SakikoPresentationDiagnostics
{
    internal static void Validate()
    {
        ValidateCurseFramesAndInlineEnergy();
        foreach (CardModel canonical in new CardModel[] { ModelDb.Card<AveMujicaCard>(), ModelDb.Card<CrychicCard>() })
        {
            foreach (bool upgrade in new[] { false, true })
            {
                CardModel card = CreateCard(canonical, upgrade);
                RotatingSakikoCardHoverTip preview = card.HoverTips.OfType<RotatingSakikoCardHoverTip>().Single();
                Require(preview.Variants.Count == 5 && preview.Variants.Select(variant => variant.Id).Distinct().Count() == 5 &&
                        preview.Variants.All(variant => variant.IsUpgraded == upgrade),
                    "Element and Phantom previews must contain all five variants at the matching upgrade level");
            }
        }
        foreach (CardModel canonical in new CardModel[]
                 {
                     ModelDb.Card<PhantomOfMutsumiCard>(), ModelDb.Card<PhantomOfSakikoCard>(),
                     ModelDb.Card<PhantomOfSoyoCard>(), ModelDb.Card<PhantomOfTakiCard>(), ModelDb.Card<PhantomOfTomoriCard>()
                 })
        {
            CardModel upgraded = CreateCard(canonical, true);
            Require(upgraded.HoverTips.OfType<CardHoverTip>().All(preview => preview.Card.IsUpgraded),
                "an upgraded Phantom still previewed its unupgraded generated card");
        }
        Require(HasKeywordTip(CreateCard(ModelDb.Card<MementoMoriCard>(), true), "PURGE"),
            "permanent removal was missing its keyword description");
        Require(HasKeywordTip(ModelDb.Card<TheMoonlightSonataCard>().ToMutable(), "PURGE") &&
                !HasKeywordTip(ModelDb.Card<BlackBirthdayCard>().ToMutable(), "PURGE"),
            "card removal tooltips must describe deck removal rather than power removal");
        Require(HasKeywordTip(ModelDb.Card<QuaerereLuminaCard>().ToMutable(), "SCRY"),
            "Scry was missing its keyword description");

        string avePreview = ModelDb.Card<AveMujicaCard>().ToMutable().GetDescriptionForUpgradePreview();
        string crychicPreview = ModelDb.Card<CrychicCard>().ToMutable().GetDescriptionForUpgradePreview();
        string aleaPreview = ModelDb.Card<AleaIactaEstCard>().ToMutable().GetDescriptionForUpgradePreview();
        string mementoPreview = ModelDb.Card<MementoMoriCard>().ToMutable().GetDescriptionForUpgradePreview();
        bool chinese = LocManager.Instance.Language.ToString() == "zhs";
        string playerDivinityName = chinese ? "旋律之主" : "Master of Melodia";
        Require(ModelDb.Power<MonsterDivinityPower>().Title.GetFormattedText() == playerDivinityName &&
                ModelDb.Card<MementoMoriCard>().ToMutable().HoverTips.OfType<HoverTip>().Any(tip => tip.Title == playerDivinityName),
            "player Divinity previews did not use Master of Melodia");
        Require(mementoPreview.Contains(playerDivinityName, StringComparison.Ordinal) &&
                !new LocString("cards", "TOGAWASAKIKO-I_WANT_TO_BE_YOUR_GOD_CARD.description").GetRawText()
                    .Contains(chinese ? "神格" : "Divinity", StringComparison.Ordinal),
            "a player buff source still used the old Divinity name");
        Require(avePreview.Contains(chinese ? "[green]元素+[/green]" : "[green]Element+[/green]", StringComparison.Ordinal) &&
                !avePreview.Contains(chinese ? "[green]从3张" : "[green]Choose 1", StringComparison.Ordinal),
            "Ave Mujica highlighted its whole description instead of Element+");
        Require(crychicPreview.Contains(chinese ? "[green]幻影+[/green]" : "[green]Phantoms+[/green]", StringComparison.Ordinal) &&
                !crychicPreview.Contains(chinese ? "[green]将X张" : "[green]Add X", StringComparison.Ordinal),
            "Crychic highlighted its whole description instead of Phantoms+");
        Require(!aleaPreview.Contains(chinese ? "[green]本场战斗中" : "[green]Upgrade all cards", StringComparison.Ordinal) &&
                !mementoPreview.Contains(chinese ? "[green]造成" : "[green]Deal ", StringComparison.Ordinal),
            "shared upgrade rendering still highlighted unchanged description text");
        if (!chinese)
        {
            Require(ModelDb.Card<MementoMoriCard>().Title == "Memento Mori", "Memento Mori was missing its title space");
        }
    }

    internal static async Task ValidateRotationAsync(Node host, CancellationToken cancellationToken)
    {
        NHoverTipCardContainer container = new() { Position = new Vector2(-4000f, -4000f) };
        host.AddChild(container);
        try
        {
            RotatingSakikoCardHoverTip tip = ModelDb.Card<AveMujicaCard>().ToMutable().HoverTips
                .OfType<RotatingSakikoCardHoverTip>().Single();
            container.Add(tip);
            NCard card = container.GetChildren().OfType<Control>().Single().GetNode<NCard>("%Card");
            ModelId initialId = (card.Model ?? throw new InvalidOperationException("The live hover card had no model.")).Id;
            Require(initialId == tip.Variants[0].Id, "the live hover card did not start on its first variant");
            ulong deadline = Time.GetTicksMsec() + 2500UL;
            while (card.Model?.Id == initialId && Time.GetTicksMsec() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            Require(card.Model?.Id == tip.Variants[1].Id,
                "the live hover card did not rotate to its next Element after one second");
        }
        finally
        {
            container.QueueFree();
        }
    }

    private static void ValidateCurseFramesAndInlineEnergy()
    {
        CardModel nativeCurse = ModelDb.CardPool<CurseCardPool>().AllCards.First();
        CardModel[] curses = ModelDb.CardPool<TogawaSakikoCardPool>().AllCards
            .Where(card => card.Type == CardType.Curse).ToArray();
        Require(curses.Length > 0 && curses.All(card => card.Frame.ResourcePath == nativeCurse.Frame.ResourcePath &&
                card.FrameMaterial.ResourcePath == nativeCurse.FrameMaterial.ResourcePath &&
                card.FrameMaterial.ResourcePath != NativeAssetPaths.CardFrameMaterial),
            "a Sakiko curse used the colored custom frame or material");

        Texture2D texture = ResourceLoader.Load<Texture2D>(NativeAssetPaths.RichTextEnergyIcon);
        using Image image = texture.GetImage();
        if (image.IsCompressed())
        {
            Require(image.Decompress() == Error.Ok, "the inline energy icon could not be read");
        }
        int coloredPixels = 0;
        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                Color pixel = image.GetPixel(x, y);
                if (pixel.A > 0.5f && Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) -
                    Math.Min(pixel.R, Math.Min(pixel.G, pixel.B)) > 0.08f)
                {
                    coloredPixels++;
                }
            }
        }
        Require(coloredPixels >= 8, "the inline energy icon was still monochrome");
    }

    private static bool HasKeywordTip(CardModel card, string key) => card.HoverTips.OfType<HoverTip>()
        .Any(tip => tip.Id.Contains("TOGAWASAKIKO-" + key, StringComparison.Ordinal));

    private static CardModel CreateCard(CardModel canonical, bool upgrade)
    {
        CardModel card = canonical.ToMutable();
        if (upgrade)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
        return card;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Sakiko presentation contract failed: " + message + ".");
        }
    }
}
