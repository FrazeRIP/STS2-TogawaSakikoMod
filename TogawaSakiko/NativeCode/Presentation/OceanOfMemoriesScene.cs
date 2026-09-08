using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using TogawaSakiko.NativeCode.Models.Events;

namespace TogawaSakiko.NativeCode.Presentation;

public partial class OceanOfMemoriesScene : Control, ICustomEventNode
{
    private OceanOfMemories _event = null!;
    private VBoxContainer _options = null!;
    private MegaRichTextLabel _description = null!;
    private bool _selectionPending;

    public IScreenContext CurrentScreenContext => this;
    public Control? DefaultFocusedControl => _options?.GetChildren().OfType<NEventOptionButton>().FirstOrDefault();

    public void Initialize(EventModel eventModel)
    {
        _event = (OceanOfMemories)eventModel;
    }

    public override void _EnterTree()
    {
        ActiveScreenContext.Instance.Updated += OnActiveScreenUpdated;
    }

    public override void _Ready()
    {
        GetNode<TextureRect>("Artwork").Texture = ResourceLoader.Load<Texture2D>(OceanOfMemories.ArtworkPath);
        _options = GetNode<VBoxContainer>("Content/Options");
        Control content = GetNode<Control>("Content");

        MegaLabel title = new() { Name = "Title", CustomMinimumSize = new Vector2(0, 64), MouseFilter = MouseFilterEnum.Ignore };
        title.AddThemeFontOverride("font", ResourceLoader.Load<Font>("res://themes/spectral_bold_shared.tres"));
        title.AddThemeColorOverride("font_color", new Color("eedbb5"));
        title.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.7f));
        title.AddThemeConstantOverride("shadow_offset_x", 2);
        title.AddThemeConstantOverride("shadow_offset_y", 2);
        title.AddThemeFontSizeOverride("font_size", 42);
        title.MinFontSize = 28;
        title.MaxFontSize = 42;
        content.AddChild(title);
        content.MoveChild(title, 0);
        title.SetTextAutoSize(_event.Title.GetFormattedText());

        _description = new MegaRichTextLabel
        {
            Name = "Description", CustomMinimumSize = new Vector2(0, 112),
            BbcodeEnabled = true, FitContent = true, ScrollActive = false,
            MouseFilter = MouseFilterEnum.Ignore, AutoSizeEnabled = false, MinFontSize = 22, MaxFontSize = 26
        };
        _description.AddThemeFontOverride("normal_font", ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres"));
        _description.AddThemeFontOverride("bold_font", ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres"));
        _description.AddThemeFontSizeOverride("normal_font_size", 26);
        _description.AddThemeColorOverride("default_color", new Color("e0e9e8"));
        content.AddChild(_description);
        content.MoveChild(_description, 1);

        _event.StateChanged += Refresh;
        Refresh(_event);
        _event.OnRoomEnter();
    }

    public override void _ExitTree()
    {
        _event.StateChanged -= Refresh;
        ActiveScreenContext.Instance.Updated -= OnActiveScreenUpdated;
    }

    internal void OnOptionClicked(EventOption option, int index)
    {
        if (_selectionPending || option.IsLocked)
        {
            return;
        }

        if (option.IsProceed)
        {
            TaskHelper.RunSafely(option.Chosen());
            return;
        }

        if (_event.IsFinished || index < 0 || index >= _event.CurrentOptions.Count ||
            !ReferenceEquals(option, _event.CurrentOptions[index]))
        {
            return;
        }

        _selectionPending = true;
        foreach (NEventOptionButton button in _options.GetChildren().OfType<NEventOptionButton>())
        {
            button.Disable();
        }
        RunManager.Instance.EventSynchronizer.ChooseLocalOption(index);
    }

    private void Refresh(EventModel eventModel)
    {
        _selectionPending = false;
        LocString description = eventModel.Description ?? eventModel.InitialDescription;
        eventModel.Owner!.Character.AddDetailsTo(description);
        eventModel.DynamicVars.AddTo(description);
        _description.SetTextAutoSize(description.GetFormattedText());

        foreach (Node child in _options.GetChildren())
        {
            _options.RemoveChild(child);
            child.QueueFree();
        }

        IReadOnlyList<EventOption> choices = eventModel.IsFinished
            ? [new EventOption(eventModel, NEventRoom.Proceed, "PROCEED", false, true)]
            : eventModel.CurrentOptions;
        for (int index = 0; index < choices.Count; index++)
        {
            NEventOptionButton button = NEventOptionButton.Create(eventModel, choices[index], index);
            button.CustomMinimumSize = new Vector2(0, 126);
            _options.AddChild(button);
            HBoxContainer row = button.GetNode<HBoxContainer>("HBoxContainer");
            row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            row.OffsetLeft = 22;
            row.OffsetTop = 13;
            row.OffsetRight = -22;
            row.OffsetBottom = -13;
            MegaRichTextLabel text = button.GetNode<MegaRichTextLabel>("%Text");
            text.CustomMinimumSize = new Vector2(0, 74);
            text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.RefreshVotes();
            button.EnableButton();
        }

        NEventOptionButton[] buttons = _options.GetChildren().OfType<NEventOptionButton>().ToArray();
        for (int index = 0; index < buttons.Length; index++)
        {
            NEventOptionButton button = buttons[index];
            button.FocusNeighborLeft = button.GetPath();
            button.FocusNeighborRight = button.GetPath();
            button.FocusNeighborTop = buttons[(index + buttons.Length - 1) % buttons.Length].GetPath();
            button.FocusNeighborBottom = buttons[(index + 1) % buttons.Length].GetPath();
        }
        DefaultFocusedControl?.TryGrabFocus();
    }

    private void OnActiveScreenUpdated()
    {
        this.UpdateControllerNavEnabled();
    }
}
