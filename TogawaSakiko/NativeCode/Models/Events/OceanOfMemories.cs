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

    private bool _choiceStarted;

    public override string LocTable => "events";
    public override EventLayoutType LayoutType => base.LayoutType;
    public override string AmbientBgm => base.AmbientBgm;
    public override Color ButtonColor => base.ButtonColor;
    public override LocString InitialDescription => new("ancients", "NEOW.pages.INITIAL.description");
    public override IEnumerable<EventOption> AllPossibleOptions => GenerateInitialOptions();

    public override IEnumerable<string> GetAssetPaths(IRunState runState) =>
        base.GetAssetPaths(runState);

    protected override AncientDialogueSet DefineDialogues() => new()
    {
        FirstVisitEverDialogue = null,
        CharacterDialogues = [],
        AgnosticDialogues = []
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, () => ChooseAsync("ANOTHER_MASK"), OptionKey("ANOTHER_MASK"))
            .WithRelic<AnotherMask>(Owner),
        new EventOption(this, () => ChooseAsync("THE_THIRD_MOVEMENT"), OptionKey("THE_THIRD_MOVEMENT"))
            .WithRelic<TheThirdMovement>(Owner),
        new EventOption(this, () => ChooseAsync("BLAZING_HAIRBAND"), OptionKey("BLAZING_HAIRBAND"))
            .WithRelic<BlazingHairband>(Owner)
    ];

    internal async Task ChooseAsync(string optionKey)
    {
        AssertMutable();
        if (_choiceStarted || IsFinished)
        {
            return;
        }

        if (optionKey is not ("ANOTHER_MASK" or "THE_THIRD_MOVEMENT" or "BLAZING_HAIRBAND"))
        {
            throw new ArgumentOutOfRangeException(nameof(optionKey), optionKey, "Unknown ocean starting choice.");
        }

        _choiceStarted = true;
        switch (optionKey)
        {
            case "ANOTHER_MASK":
                await RelicCmd.Obtain(ModelDb.Relic<AnotherMask>().ToMutable(), Owner!);
                break;
            case "THE_THIRD_MOVEMENT":
                await RelicCmd.Obtain(ModelDb.Relic<TheThirdMovement>().ToMutable(), Owner!);
                break;
            case "BLAZING_HAIRBAND":
                // Its native obtain hook removes Monochrome Hairband.
                await RelicCmd.Obtain(ModelDb.Relic<BlazingHairband>().ToMutable(), Owner!);
                break;
        }

        Done();
    }

    private static string OptionKey(string option) => Entry + ".pages.INITIAL.options." + option;
}
