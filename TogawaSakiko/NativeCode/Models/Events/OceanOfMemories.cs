using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Relics;

namespace TogawaSakiko.NativeCode.Models.Events;

// Retaining Neow's lifecycle preserves native starting health, ancient history, and pre-finished saves.
public sealed class OceanOfMemories : Neow
{
    public const string Entry = "TOGAWASAKIKO-OCEAN_OF_MEMORIES";
    public const string ScenePath = "res://TogawaSakiko/scenes/events/ocean_of_memories.tscn";
    public const string ArtworkPath = "res://TogawaSakiko/images/events/ocean_of_memories.png";
    public const string NativeBackgroundScenePath = "res://scenes/events/background_scenes/neow.tscn";

    public override string LocTable => "events";
    public override EventLayoutType LayoutType => base.LayoutType;
    public override string AmbientBgm => base.AmbientBgm;
    public override Color ButtonColor => base.ButtonColor;
    public override LocString InitialDescription => new("ancients", "NEOW.pages.INITIAL.description");
    public override IEnumerable<EventOption> AllPossibleOptions => GenerateInitialOptions();

    public override IEnumerable<string> GetAssetPaths(IRunState runState) =>
        base.GetAssetPaths(runState).Append(SakikoStartingRewards.SpeechIconPath);

    protected override AncientDialogueSet DefineDialogues() => new()
    {
        FirstVisitEverDialogue = null,
        CharacterDialogues = [],
        AgnosticDialogues = []
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        SakikoStartingRewards.CreateOptions(this);

    internal IReadOnlyList<EventOption> GenerateNormalOptions() => base.GenerateInitialOptions();

    internal Task ChooseAsync(string optionKey) => SakikoStartingRewards.ChooseAsync(this, optionKey);
}
