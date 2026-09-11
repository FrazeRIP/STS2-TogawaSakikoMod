using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;
using TogawaSakiko.NativeCode.Content;
using TogawaSakiko.NativeCode.Models.Events;
using TogawaSakiko.NativeCode.Models.Pools;
using SakikoCharacter = TogawaSakiko.NativeCode.Models.Characters.TogawaSakiko;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
internal static class SakikoCardLibraryPatch
{
    internal const string FilterName = "TogawaSakikoPool";
    internal const string FilterPath = "Sidebar/MarginContainer/TopVBox/PoolFilters/" + FilterName;

    private const string FilterScenePath = "res://scenes/screens/card_library/library_pool_toggle.tscn";
    private static readonly FieldInfo CharacterFiltersField = AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters");
    private static readonly FieldInfo PoolFiltersField = AccessTools.Field(typeof(NCardLibrary), "_poolFilters");
    private static readonly FieldInfo LastHoveredField = AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl");
    private static readonly MethodInfo UpdatePoolFilterMethod = AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");

    private static void Postfix(NCardLibrary __instance)
    {
        var characterFilters = (Dictionary<CharacterModel, NCardPoolFilter>)CharacterFiltersField.GetValue(__instance)!;
        CharacterModel character = ModelDb.Character<SakikoCharacter>();
        if (characterFilters.ContainsKey(character))
        {
            return;
        }

        var poolFilters = (Dictionary<NCardPoolFilter, Func<CardModel, bool>>)PoolFiltersField.GetValue(__instance)!;
        NCardPoolFilter filter = ResourceLoader.Load<PackedScene>(FilterScenePath).Instantiate<NCardPoolFilter>();
        filter.Name = FilterName;
        filter.Loc = character.Title;
        Texture2D texture = ResourceLoader.Load<Texture2D>(NativeAssetPaths.CharacterIcon);
        filter.GetNode<TextureRect>("Image").Texture = texture;
        filter.GetNode<TextureRect>("Image/Shadow").Texture = texture;

        GridContainer container = __instance.GetNode<NCardPoolFilter>("%IroncladPool").GetParent<GridContainer>();
        container.AddChildSafely(filter);
        container.MoveChildSafely(filter, __instance.GetNode<NCardPoolFilter>("%ColorlessPool").GetIndex());
        filter.Owner = __instance;
        filter.UniqueNameInOwner = true;
        filter.IsSelected = false;
        filter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(selected =>
            UpdatePoolFilterMethod.Invoke(__instance, [selected])));
        filter.Connect(Control.SignalName.FocusEntered, Callable.From(() => LastHoveredField.SetValue(__instance, filter)));

        // The native submenu uses both maps when opening during a run and when applying radio-button filters.
        characterFilters.Add(character, filter);
        poolFilters.Add(filter, card => card.Pool is TogawaSakikoCardPool);
        ConfigureNavigation(__instance, container);
    }

    private static void ConfigureNavigation(NCardLibrary library, GridContainer container)
    {
        NCardPoolFilter[] filters = container.GetChildren().OfType<NCardPoolFilter>().Where(filter => filter.Visible).ToArray();
        Control search = library.GetNode<Control>("Sidebar/MarginContainer/TopVBox/SearchBar/TextArea");
        Control nextSection = library.GetNode<Control>("%CardTypeSorter");
        for (int index = 0; index < filters.Length; index++)
        {
            NCardPoolFilter filter = filters[index];
            int rowStart = index / container.Columns * container.Columns;
            int rowEnd = Math.Min(rowStart + container.Columns, filters.Length) - 1;
            filter.FocusNeighborLeft = filters[index > rowStart ? index - 1 : rowEnd].GetPath();
            filter.FocusNeighborRight = filters[index < rowEnd ? index + 1 : rowStart].GetPath();
            filter.FocusNeighborTop = index >= container.Columns ? filters[index - container.Columns].GetPath() : search.GetPath();
            filter.FocusNeighborBottom = index + container.Columns < filters.Length
                ? filters[index + container.Columns].GetPath()
                : nextSection.GetPath();
        }
    }
}

internal static class SakikoCompendiumVisibility
{
    internal static void DiscoverCards(ProgressState progress)
    {
        // Native card inspection reads progress directly, so grid-only visibility would leave known cards unclickable.
        foreach (CardModel card in ModelDb.CardPool<TogawaSakikoCardPool>().AllCards)
        {
            progress.MarkCardAsSeen(card.Id);
        }
    }

    internal static void DiscoverRelics(ProgressState progress)
    {
        foreach (RelicModel relic in SakikoTemporarilyHiddenRelicPatch.VisibleRelics(ModelDb.RelicPool<TogawaSakikoRelicPool>().AllRelics))
        {
            progress.MarkRelicAsSeen(relic.Id);
        }
    }
}

[HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.RefreshVisibility))]
internal static class SakikoCardLibraryVisibilityPatch
{
    private static readonly FieldInfo UnlockedCardsField = AccessTools.Field(typeof(NCardLibraryGrid), "_unlockedCards");

    private static void Prefix() => SakikoCompendiumVisibility.DiscoverCards(SaveManager.Instance.Progress);

    private static void Postfix(NCardLibraryGrid __instance)
    {
        // Include starters and generated cards for inspection without adding them to normal card reward pools.
        ((HashSet<CardModel>)UnlockedCardsField.GetValue(__instance)!).UnionWith(ModelDb.CardPool<TogawaSakikoCardPool>().AllCards);
    }
}

[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
internal static class SakikoRelicCollectionVisibilityPatch
{
    private static readonly FieldInfo SubcategoriesField = AccessTools.Field(typeof(NRelicCollectionCategory), "_subCategories");
    private static readonly MethodInfo LoadSubcategoryMethod = AccessTools.Method(typeof(NRelicCollectionCategory), "LoadSubcategory");
    private static readonly MethodInfo LoadIconMethod = AccessTools.Method(typeof(NRelicCollectionCategory), "LoadIcon");

    private static void Prefix(HashSet<RelicModel> seenRelics, HashSet<RelicModel> allUnlockedRelics)
    {
        SakikoCompendiumVisibility.DiscoverRelics(SaveManager.Instance.Progress);
        IEnumerable<RelicModel> relics = SakikoTemporarilyHiddenRelicPatch.VisibleRelics(ModelDb.RelicPool<TogawaSakikoRelicPool>().AllRelics);
        seenRelics.UnionWith(relics);
        allUnlockedRelics.UnionWith(relics);
    }

    private static void Postfix(
        NRelicCollectionCategory __instance,
        RelicRarity relicRarity,
        NRelicCollection collection,
        HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (relicRarity != RelicRarity.Ancient)
        {
            return;
        }

        // Native ancient sections enumerate random-map ancients. Our fixed starting event stays outside those pools.
        RelicModel[] relics = ModelDb.RelicPool<TogawaSakikoRelicPool>().AllRelics
            .Where(relic => relic.Rarity == RelicRarity.Ancient)
            .OrderBy(relic => relic.Title.GetFormattedText(), LocManager.Instance.StringComparer)
            .ToArray();
        NRelicCollectionCategory category = ResourceLoader.Load<PackedScene>(NRelicCollectionCategory.scenePath)
            .Instantiate<NRelicCollectionCategory>();
        category.Name = "TogawaSakikoAncientRelics";
        var subcategories = (List<NRelicCollectionCategory>)SubcategoriesField.GetValue(__instance)!;
        subcategories.Add(category);
        __instance.AddChildSafely(category);
        __instance.MoveChildSafely(category, subcategories.Count);
        AncientEventModel ancient = ModelDb.Event<OceanOfMemories>();
        LocString title = new("relic_collection", "ANCIENT_SUBCATEGORY");
        title.Add("Ancient", ancient.Title);
        LoadSubcategoryMethod.Invoke(category, [collection, title, relics, seenRelics, allUnlockedRelics]);
        LoadIconMethod.Invoke(category, [ancient.RunHistoryIcon]);
    }
}
