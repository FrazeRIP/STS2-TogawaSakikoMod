using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace TogawaSakiko.NativeCode.Presentation;

internal static class HairbandCardPreview
{
    internal const float PreviewScale = 0.6f;
    internal const double HoldSeconds = 0.8;

    public static void Show(CardPileAddResult result)
    {
        NGlobalUi? ui = NRun.Instance?.GlobalUi;
        if (TestMode.IsOn || CombatManager.Instance.IsEnding || !result.success ||
            !LocalContext.IsMine(result.cardAdded) || result.cardAdded.Pile is null || ui is null)
        {
            return;
        }

        NCard? card = NCard.Create(result.cardAdded);
        if (card is null)
        {
            return;
        }

        ui.TopBar.TrailContainer.AddChildSafely(card);
        card.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
        card.MouseFilter = Control.MouseFilterEnum.Ignore;

        Rect2 inventoryBounds = ui.RelicInventory.GetGlobalRect();
        float relicBottom = ui.RelicInventory.RelicNodes.Count > 0
            ? ui.RelicInventory.RelicNodes.Max(relic => relic.GetGlobalRect().End.Y)
            : inventoryBounds.End.Y;
        // Native cards are centered on their origin. Leave room for the scaled card below the relics.
        card.GlobalPosition = new Vector2(inventoryBounds.Position.X + 106f, relicBottom + 146f);
        foreach (RelicModel relic in result.modifyingModels?.OfType<RelicModel>() ?? [])
        {
            relic.Flash();
            card.FlashRelicOnCard(relic);
        }

        Tween tween = card.CreateTween();
        tween.TweenProperty(card, "scale", Vector2.One * PreviewScale, 0.12)
            .From(Vector2.Zero).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenInterval(HoldSeconds);
        tween.TweenCallback(Callable.From(() =>
        {
            PileType target = result.cardAdded.Pile?.Type ?? PileType.Deck;
            NCardFlyVfx? flight = NCardFlyVfx.Create(
                card, target, isAddingToPile: true, result.cardAdded.Owner.Character.TrailPath);
            if (flight is null)
            {
                card.QueueFreeSafely();
                return;
            }

            ui.TopBar.TrailContainer.AddChildSafely(flight);
        }));
    }
}
