using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HOI4Builder.App.Views;

public partial class SidebarView : UserControl
{
    public SidebarView()
    {
        InitializeComponent();
    }

    private async void OnQuickSave(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is MainWindow window)
            await window.SaveAsync(forcePicker: false);
    }

    private async void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is MainWindow window)
            await window.SaveAsync(forcePicker: true);
    }
}