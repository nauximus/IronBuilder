using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HOI4Builder.App.ViewModels;

namespace HOI4Builder.App.Views;

public partial class OverviewView : UserControl
{
    public OverviewView() => InitializeComponent();

    private OverviewViewModel? Vm => DataContext as OverviewViewModel;

    private async Task<string?> PickFolderAsync(string title)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null)
            return null;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private async void OnBrowseExport(object? sender, RoutedEventArgs e)
    {
        if (await PickFolderAsync("Choose the export folder") is { } path && Vm is { } vm)
            vm.ExportFolder = path;
    }

    private async void OnBrowseInstall(object? sender, RoutedEventArgs e)
    {
        if (await PickFolderAsync("Choose the Hearts of Iron IV install folder") is { } path && Vm is { } vm)
            vm.Hoi4InstallFolder = path;
    }

    private async void OnBrowseImported(object? sender, RoutedEventArgs e)
    {
        if (await PickFolderAsync("Choose the imported mod folder") is { } path && Vm is { } vm)
            vm.ImportedModFolder = path;
    }

    // Placeholder for features that are not built yet; the button's Tag names the feature.
    private async void OnNotBuilt(object? sender, RoutedEventArgs e)
    {
        var name = (sender as Button)?.Tag as string ?? "This feature";

        if (TopLevel.GetTopLevel(this) is Window owner)
            await new MessageDialog(name, $"{name} isn't built yet.").ShowDialog(owner);
    }
}