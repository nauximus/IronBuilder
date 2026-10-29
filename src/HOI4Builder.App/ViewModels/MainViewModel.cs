using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;
using HOI4Builder.Core.Script;

namespace HOI4Builder.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Project = CreateDemoProject();

        Editors = new ObservableCollection<EditorEntry>
        {
            new("Focus trees", new FocusEditorViewModel(Project)),
            new("Countries", new PlaceholderEditorViewModel("Countries")),
            new("Characters", new PlaceholderEditorViewModel("Characters")),
            new("Events", new PlaceholderEditorViewModel("Events")),
            new("Decisions", new PlaceholderEditorViewModel("Decisions")),
            new("National Spirits", new PlaceholderEditorViewModel("National Spirits")),
            new("Units", new PlaceholderEditorViewModel("Units")),
            new("Bookmarks", new PlaceholderEditorViewModel("Bookmarks")),
        };

        SelectedEditor = Editors[0];
        Project.History.Changed += OnHistoryChanged;
    }

    public ModProject Project { get; }

    public ObservableCollection<EditorEntry> Editors { get; }

    [ObservableProperty]
    private EditorEntry? _selectedEditor;

    [ObservableProperty]
    private string _statusText = "Ready";
    
    [RelayCommand (CanExecute = nameof(CanUndo))]
    private void Undo() => Project.History.Undo();

    private bool CanUndo() => Project.History.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => Project.History.Redo();

    private bool CanRedo() => Project.History.CanRedo;

    private void OnHistoryChanged()
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        StatusText = Project.History.UndoDescription is { } next ? $"Next undo: {next}" : "Ready";
    }

    private static ModProject CreateDemoProject()
    {
        const string sample = """
            focus_tree = {
                id = demo
                focus = { id = DEMO_start x = 2 y = 0 }
                focus = { id = DEMO_left x = 1 y = 1 prerequisite = { focus = DEMO_start } mutually_exclusive = { focus = DEMO_right } }
                focus = { id = DEMO_right x = 3 y = 1 prerequisite = { focus = DEMO_start } mutually_exclusive = { focus = DEMO_left } }
                focus = { id = DEMO_join x = 0 y = 2 relative_position_id = DEMO_right prerequisite = { focus = DEMO_left focus = DEMO_right } }
            }
            """;

        var project = new ModProject();
        project.FocusTrees.AddRange(FocusTreeMapper.ReadTrees(PdxParser.Parse(sample)));
        return project;
    }
}
