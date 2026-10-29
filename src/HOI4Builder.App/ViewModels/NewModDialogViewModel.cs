using CommunityToolkit.Mvvm.ComponentModel;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.ViewModels;

public partial class NewModDialogViewModel : ViewModelBase
{
    private bool _idEditedManually;
    private bool _settingIdFromName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _name = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _modId = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _gameVersion = ModProject.DefaultGameVersion;

    [ObservableProperty]
    private string _exportFolder = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasModIdError))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string? _modIdError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorText))]
    private string? _errorText;

    public bool HasModIdError => ModIdError != null;
    public bool HasErrorText => ErrorText != null;

    public bool CanCreate =>
        !string.IsNullOrWhiteSpace(Name)
        && ModId.Length > 0
        && ModIdError == null
        && !string.IsNullOrWhiteSpace(GameVersion);

    partial void OnNameChanged(string value)
    {
        if (!_idEditedManually)
        {
            _settingIdFromName = true;
            ModId = string.IsNullOrWhiteSpace(value) ? "" : ModIdRules.FromName(value);
            _settingIdFromName = false;
        }
    }

    partial void OnModIdChanged(string value)
    {
        if (!_settingIdFromName)
            _idEditedManually = true;
        
        ModIdError = value.Length == 0 ? null : ModIdRules.Validate(value);
    }
}