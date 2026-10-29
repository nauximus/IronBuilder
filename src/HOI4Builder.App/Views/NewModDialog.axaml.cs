using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HOI4Builder.App.ViewModels;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.Views;

public partial class NewModDialog : Window
{
    public NewModDialog()
    {
        InitializeComponent();
        DataContext = new NewModDialogViewModel();
    }

    private NewModDialogViewModel Vm => (NewModDialogViewModel)DataContext!;

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose the export folder",
            AllowMultiple = false,
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (path != null)
            Vm.ExportFolder = path;
    }

    private void OnCreate(object? sender, RoutedEventArgs e)
    {
        try
        {
            Close(ModProject.CreateNew(Vm.Name, Vm.ModId, Vm.GameVersion, Vm.ExportFolder));
        }
        catch (ArgumentException ex)
        {
            Vm.ErrorText = ex.Message;
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}