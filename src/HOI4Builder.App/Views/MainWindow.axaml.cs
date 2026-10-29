using Avalonia.Controls;
using Avalonia.Interactivity;
using HOI4Builder.App.ViewModels;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void OnNewMod(object? sender, RoutedEventArgs e)
    {
        var dialog = new NewModDialog();
        var project = await dialog.ShowDialog<ModProject?>(this);

        if (project != null && DataContext is MainViewModel vm)
            vm.Project = project;
    }
}