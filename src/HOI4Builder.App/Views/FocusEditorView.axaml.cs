using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using HOI4Builder.App.ViewModels;

namespace HOI4Builder.App.Views;


public partial class FocusEditorView : UserControl
{
    private FocusEditorViewModel? _vm;
    public FocusEditorView()
    {
        InitializeComponent();
        Surface.SelectionChanged += focus => _vm?.SelectFocus(focus);
        Surface.Notice += text => _vm?.ShowNotice(text); 
        DataContextChanged += OnDataContextChanged;
    }

    private void OnAddFocus(object? sender, RoutedEventArgs e) => Surface.AddFocusInView();
    private void OnDeleteFocus(object? sender, RoutedEventArgs e) => Surface.DeleteSelected();
    private void OnAddPrerequisite(object? sender, RoutedEventArgs e) => Surface.BeginLink(LinkMode.AddPrerequisite);
    private void OnMakePrerequisiteOf(object? sender, RoutedEventArgs e) => Surface.BeginLink(LinkMode.MakePrerequisiteOf);
    private void OnExclusive(object? sender, RoutedEventArgs e) => Surface.BeginLink(LinkMode.Exclusive);
    private void OnResetView(object? sender, RoutedEventArgs e) => Surface.ResetView();

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