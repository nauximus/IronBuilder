using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.ViewModels;

public partial class CountrySelection : ObservableObject
{
    public CountrySelection(ModProject project)
    {
        Project = project;
        Sync();
        project.History.Changed += Sync;
    }
    
    public ModProject Project { get; }

    public ObservableCollection<Country> All { get; } = new();

    [ObservableProperty]
    private Country? _selected;

    [ObservableProperty]
    private string _newTag = "";

    [ObservableProperty]
    private string _newName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAddError))]
    private string? _addError;

    public bool HasAddError => AddError != null;

    [RelayCommand]
    private void AddCountry()
    {
        var tag = NewTag.Trim().ToUpperInvariant();

        var error = CountryTagRules.Validate(tag);
        if (error is null && Project.FindCountry(tag) != null)
            error = $"The tag {tag} already exists.";

        if (error != null)
        {
            AddError = error;
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewName) ? tag : NewName.Trim();
        var country = new Country(tag, name);
        Project.History.Execute(new AddCountryCommand(Project, country));

        AddError = null;
        NewTag = "";
        NewName = "";
        Selected = country;
    }

    // undo and redo can add or remove countries, so the sorted list is rebuilt in place.
    private void Sync()
    {
        var wanted = Project.Countries.OrderBy(c => c.Tag, StringComparer.Ordinal).ToList();

        for (int i = All.Count - 1; i >= 0; i--)
        {
            if (!wanted.Contains(All[i]))
                All.RemoveAt(i);
        }

        for (int i = 0; i < wanted.Count; i++)
        {
            if (i >= All.Count || !ReferenceEquals(All[i], wanted[i]))
                All.Insert(i, wanted[i]);
        }

        if (Selected is null || !All.Contains(Selected))
            Selected = All.FirstOrDefault();
    }
}
