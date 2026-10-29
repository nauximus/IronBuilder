using Avalonia.Controls;
using HOI4Builder.App.ViewModels;

namespace HOI4Builder.App.Views;


public partial class FocusEditorView : UserControl
{
    public FocusEditorView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is FocusEditorViewModel vm)
            {
                Surface.History = vm.Project.History;
                Surface.Tree = vm.SelectedTree;
            }
        };
    }
}