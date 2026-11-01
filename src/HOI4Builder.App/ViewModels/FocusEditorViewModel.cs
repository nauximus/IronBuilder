using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;
using HOI4Builder.Core.Script;

namespace HOI4Builder.App.ViewModels;

public partial class FocusEditorViewModel : ViewModelBase
{
    public FocusEditorViewModel(ModProject project, CountrySelection selection)
    {
        Project = project;
        Selection = selection;

        project.History.Changed += RefreshTrees;
        selection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CountrySelection.Selected))
                RefreshTrees();
        };

        RefreshTrees();
    }

    public ModProject Project { get; }

    public CountrySelection Selection { get; }

    // The trees shown in the dropdown: the selected country's, plus unassigned ones if asked for.
    public ObservableCollection<FocusTree> Trees { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTree))]
    private FocusTree? _selectedTree;

    public bool HasTree => SelectedTree != null;

    [ObservableProperty]
    private bool _showUnassigned;

    [ObservableProperty]
    private string? _notice;

    public void ShowNotice(string? text) => Notice = text;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProperties))]
    private FocusPropertiesViewModel? _properties;

    public bool HasProperties => Properties != null;

    partial void OnShowUnassignedChanged(bool value) => RefreshTrees();

    partial void OnSelectedTreeChanged(FocusTree? value)
    {
        SelectFocus(null);
        AssignToCountryCommand.NotifyCanExecuteChanged();
    }

    public void SelectFocus(Focus? focus)
    {
        Notice = null;
        Properties?.Dispose();

        Properties = focus is null || SelectedTree is null
            ? null
            : new FocusPropertiesViewModel(SelectedTree, focus, Project.History);
    }

    private bool CanAddTree() =>
        Selection.Selected is { } country && !Project.TreesFor(country.Tag).Any();

    [RelayCommand(CanExecute = nameof(CanAddTree))]
    private void AddTree()
    {
        if (Selection.Selected is not { } country)
            return;

        var tree = new FocusTree
        {
            Id = Project.MakeUniqueTreeId(country.Tag.ToLowerInvariant() + "_tree_"),
            Country = CountryAssignment.Build(country.Tag),
        };

        Project.History.Execute(new AddFocusTreeCommand(Project, tree));
        SelectedTree = tree;
    }

    private bool CanAssign() =>
        SelectedTree is { } tree
        && Selection.Selected is { } country
        && tree.CountryTag != country.Tag
        && !Project.TreesFor(country.Tag).Any();

    [RelayCommand(CanExecute = nameof(CanAssign))]
    private void AssignToCountry()
    {
        if (SelectedTree is not { } tree || Selection.Selected is not { } country)
            return;

        Project.History.Execute(new PropertyChangeCommand<PdxBlock?>(
            $"Assign {tree.Id} to {country.Tag}",
            () => tree.Country,
            v => tree.Country = v,
            CountryAssignment.Build(country.Tag)));
    }

    private void RefreshTrees()
    {

        var tag = Selection.Selected?.Tag;

        var wanted = Project.FocusTrees
            .Where(t => (tag != null && t.CountryTag == tag) || (ShowUnassigned && t.CountryTag is null))
            .ToList();

        for (int i = Trees.Count - 1; i >= 0; i--)
        {
            if (!wanted.Contains(Trees[i]))
                Trees.RemoveAt(i);
        }

        for (int i = 0; i < wanted.Count; i++)
        {
            if (i >= Trees.Count || !ReferenceEquals(Trees[i], wanted[i]))
                Trees.Insert(i, wanted[i]);
        }

        if (SelectedTree is null || !Trees.Contains(SelectedTree))
            SelectedTree = Trees.FirstOrDefault();

        AddTreeCommand.NotifyCanExecuteChanged();
        AssignToCountryCommand.NotifyCanExecuteChanged();
    }
}