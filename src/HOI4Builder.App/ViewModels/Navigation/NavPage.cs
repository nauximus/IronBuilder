using CommunityToolkit.Mvvm.ComponentModel;

namespace HOI4Builder.App.ViewModels.Navigation;


public sealed partial class NavPage : ViewModelBase
{
    public NavPage(string id, string title, string subtitle, ViewModelBase viewModel)
    {
        Id = id;
        Title = title;
        Subtitle = subtitle;
        ViewModel = viewModel;
    }

    public string Id { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public ViewModelBase ViewModel { get; }

    public bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    private bool _isSelected;
}