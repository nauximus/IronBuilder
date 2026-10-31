using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using HOI4Builder.App.ViewModels;

namespace HOI4Builder.App.Views;


public partial class FocusEditorView : UserControl
{
    private FocusEditorViewModel? _vm;
    public FocusEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as FocusEditorViewModel;
        if (_vm is null)
            return;
        
        Surface.History = _vm.Project.History;
        Surface.Tree = _vm.SelectedTree;
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
       if (e.PropertyName == nameof(FocusEditorViewModel.SelectedTree) && _vm != null)
            Surface.Tree = _vm.SelectedTree;

    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_vm != null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        Surface.History = null;
    }
}