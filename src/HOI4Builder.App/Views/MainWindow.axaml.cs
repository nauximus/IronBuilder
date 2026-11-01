using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using HOI4Builder.App.ViewModels;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.Views;

public partial class MainWindow : Window
{
    private bool _closeConfirmed;
    public MainWindow() => InitializeComponent();

    private async Task<bool> ConfirmDiscardAsync()
    {
        if (DataContext is not MainViewModel vm || vm.Project is not { } project || !vm.HasUnsavedChanges)
            return true;

        var choice = await new UnsavedChangesDialog(project.Name).ShowDialog<UnsavedChoice>(this);

        return choice switch
        {
            UnsavedChoice.Save => await SaveAsync(forcePicker: false),
            UnsavedChoice.Discard => true,
            _ => false,
        };
    }

    private async void OnNewMod(object? sender, RoutedEventArgs e)
    {
        var dialog = new NewModDialog();
        var project = await dialog.ShowDialog<ModProject?>(this);
        Console.WriteLine($"1. dialog returned: {project?.Name ?? "null"}");

        if (project is null)
            return;

        bool ok = await ConfirmDiscardAsync();
        Console.WriteLine($"2. confirm discard: {ok}");
        if (!ok)
            return;

        Console.WriteLine($"3. DataContext is MainViewModel: {DataContext is MainViewModel}");
        if (DataContext is MainViewModel vm)
        {
            vm.Project = project;
            Console.WriteLine($"4. vm.Project is now: {vm.Project?.Name ?? "null"}");
        }
    }

    private async void OnImportMod(object? sender, RoutedEventArgs e)
    {
        var folders  = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose the mod folder",
            AllowMultiple = false,
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (path is null || DataContext is not MainViewModel vm)
            return;
        
        if (!await ConfirmDiscardAsync())
            return;

        try
        {
            var result = ModImporter.ImportFolder(path);
            vm.Project = result.Project;

            int trees = result.Project.FocusTrees.Count;
            int unassigned = result.Project.UnassignedTrees().Count;
            string note = unassigned > 0 ? $"{unassigned} not assigned to a country" : "";
            vm.StatusText = result.Warnings.Count == 0
                ? $"Imported {trees} focus trees{note}"
                : $"Imported {trees} focus trees{note}, {result.Warnings.Count} warnings";
            if (result.Warnings.Count > 0)
            {
                await new MessageDialog("Import warnings", string.Join("\n\n", result.Warnings)).ShowDialog(this);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await new MessageDialog("Import failed", ex.Message).ShowDialog(this);
        }
    }

    private static readonly FilePickerFileType ProjectFileType =
        new("IronBuilder project") { Patterns = new[] { "*" + ProjectFile.Extension }};

    private async void OnSave(object? sender, RoutedEventArgs e) => await SaveAsync(forcePicker:  false);

    private async void OnSaveAs(object? sender, RoutedEventArgs e) => await SaveAsync(forcePicker: true);

    private async Task<bool> SaveAsync(bool forcePicker)
    {
        if (DataContext is not MainViewModel vm || vm.Project is not { } project)
            return false;
        
        var path = project.FilePath;

        if (path is null || forcePicker)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save project",
                SuggestedFileName = project.ModId + ProjectFile.Extension,
                DefaultExtension = ProjectFile.Extension.TrimStart('.'),
                FileTypeChoices = new[] { ProjectFileType },
            });

            path = file?.TryGetLocalPath();
            if (path is null)
                return false;
            
            if (!path.EndsWith(ProjectFile.Extension, StringComparison.OrdinalIgnoreCase))
                path += ProjectFile.Extension;
        }

        try
        {
            vm.Save(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await new MessageDialog("Save failed", ex.Message).ShowDialog(this);
            return false;
        }
    }

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open project",
            AllowMultiple = false,
            FileTypeFilter = new[] { ProjectFileType, FilePickerFileTypes.All },
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (path is null || DataContext is not MainViewModel vm)
            return;
        
        if (!await ConfirmDiscardAsync())
            return;
        
        try
        {
            vm.Project = ProjectFile.Load(path);
            vm.StatusText = $"Opened {Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is ProjectFileException or IOException or UnauthorizedAccessException)
        {
            await new MessageDialog("Could not open project", ex.Message).ShowDialog(this);
        }
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S)
        {
            e.Handled = true;
            await SaveAsync(forcePicker: e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }
    }

 

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_closeConfirmed)
            return;
        
        if (DataContext is MainViewModel vm && vm.HasUnsavedChanges)
        {
            e.Cancel = true;
            _ = CloseAfterConfirmAsync();
        }
    }

    private async Task CloseAfterConfirmAsync()
    {
        if (await ConfirmDiscardAsync())
        {
            _closeConfirmed = true;
            Close();
        }
    }

    private async void OnCloseProject(object? sender, RoutedEventArgs e)
    {
        if (await ConfirmDiscardAsync() && DataContext is MainViewModel vm)
            vm.Project = null;
    }
}