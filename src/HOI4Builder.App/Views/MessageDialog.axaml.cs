using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HOI4Builder.App.Views;

public partial class MessageDialog : Window
{
    public MessageDialog() : this("", "")
    {
    }

    public MessageDialog(string title, string text)
    {
        InitializeComponent();
        Title = title;
        Body.Text = text;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}