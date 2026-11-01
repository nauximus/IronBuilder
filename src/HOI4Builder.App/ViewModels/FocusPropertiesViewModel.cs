using System;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;

namespace HOI4Builder.App.ViewModels;

public partial class FocusPropertiesViewModel : ViewModelBase, IDisposable
{
    private readonly FocusTree _tree;
    private readonly Focus _focus;
    private readonly UndoStack _history;

    public FocusPropertiesViewModel(FocusTree tree, Focus focus, UndoStack history)
    {
        _tree = tree;
        _focus = focus;
        _history = history;
        _history.Changed += Refresh;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _error;

    public bool HasError => Error != null;

    public string Position => $"x= {_focus.X}, y = {_focus.Y}";

    public string Id
    {
        get => _focus.Id;
        set
        {
            value = value?.Trim() ?? "";
            if (value.Length == 0)
                return;
            if (value.Length == 0)
            {
                Error = "A focus id cannot be empty.";
                OnPropertyChanged(nameof(Id));
            }
            else if (_tree.FindFocus(value) != null)
            {
                Error = $"A focus named '{value}' already exists.";
                OnPropertyChanged(nameof(Id));
            }
            else
            {
                Error = null;
                _history.Execute(new RenameFocusCommand(_tree, _focus, value));
            }

        }
    }

    public string Cost
    {
        get => _focus.Cost?.ToString(CultureInfo.InvariantCulture) ?? "";
        set
        {
            value = value?.Trim() ?? "";
            double? parsed = null;

            if (value.Length > 0)
            {
                bool ok = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                    && double.IsFinite(d)
                    && d >= 0;
                
                if (!ok)
                {
                    Error = "The cost must be a number that is zero or more. Use a dot for decimals.";
                    OnPropertyChanged(nameof(Cost));
                    return;
                }

                parsed = d;
            }

            if (parsed == _focus.Cost)
                return;
            
            Error = null;
            _history.Execute(new PropertyChangeCommand<double?>(
                $"Set cost of {_focus.Id}", () => _focus.Cost, v => _focus.Cost = v, parsed));
        }
    }

    public string Icon
    {
        get => _focus.Icon ?? "";
        set
        {
            value = value?.Trim() ?? "";
            string? icon = value.Length == 0 ? null : value;

            if (icon == _focus.Icon)
                return;
            
            Error = null;
            _history.Execute(new PropertyChangeCommand<string?>(
                $"Set icon of {_focus.Id}", () => _focus.Icon, v => _focus.Icon = v, icon));
        }
    }

    public string Prerequisites => _focus.Prerequisites.Count == 0
        ? "none"
        : string.Join("  AND  ", _focus.Prerequisites.Select(group =>
            group.Count == 1 ? group[0] : "(" + string.Join(" OR ", group) + ")"));
            
    public string Exclusive => _focus.MutuallyExclusive.Count == 0
        ? "none"
        : string.Join(", ", _focus.MutuallyExclusive);

    // undo, redo and drags change the model from outside, so the fields re-read it.
    private void Refresh()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(Prerequisites));
        OnPropertyChanged(nameof(Exclusive));
    }

    public void Dispose() => _history.Changed -= Refresh;

}