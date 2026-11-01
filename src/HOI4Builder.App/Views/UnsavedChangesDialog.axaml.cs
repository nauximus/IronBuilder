using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HOI4Builder.App.Views;

public enum UnsavedChoice
{
    Cancel,
    Save,
    Discard,
}

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog() : this("")
    {
    }

    public UnsavedChangesDialog(string projectName)
    {
        InitializeComponent();
        Message.Text = $"Save changes to \"{projectName}\" before continuing?";
    }

    private void OnSave(object? sender, RoutedEventArgs e) => Close(UnsavedChoice.Save);

    private void OnDiscard(object? sender, RoutedEventArgs e) => Close(UnsavedChoice.Discard);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(UnsavedChoice.Cancel);
}