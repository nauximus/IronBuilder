using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Projects;


namespace HOI4Builder.App.ViewModels;

public partial class CountriesViewModel : ViewModelBase
{
    private readonly ModProject _project;

    public CountriesViewModel(ModProject project, CountrySelection selection)
    {
        _project = project;
        Selection = selection;

        selection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(CountrySelection.Selected))
                return;
            
            RemoveError = null;
            OnPropertyChanged(nameof(HasSelection));
            RemoveCountryCommand.NotifyCanExecuteChanged();
        };
    }

    public CountrySelection Selection { get; }

    public bool HasSelection => Selection.Selected != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRemoveError))]
    private string? _removeError;
    public bool HasRemoveError => RemoveError != null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveCountry()
    {
        var country = Selection.Selected;
        if (country is null)
            return;
        
        if (_project.TreesFor(country.Tag).Any())
        {
            RemoveError = $"{country.Tag} still has focus trees. Delete them or assign them to another country first.";
            return;
        }

        _project.History.Execute(new RemoveCountryCommand(_project, country));
        RemoveError = null;
    }
}