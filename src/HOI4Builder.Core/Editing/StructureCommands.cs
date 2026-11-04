using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.Core.Editing;

public sealed class AddFocusCommand : IEditCommand
{
    private readonly FocusTree _tree;
    private readonly Focus _focus;

    public AddFocusCommand(FocusTree tree, Focus focus)
    {
        _tree = tree;
        _focus = focus;
    }

    public string Description => $"Add {_focus.Id}";

    public void Do() => _tree.Focuses.Add(_focus);

    public void Undo() => _tree.Focuses.Remove(_focus);
}

public sealed class DeleteFocusCommand : IEditCommand
{
    private sealed record Snapshot(
        Focus Owner,
        List<List<string>> Prerequisites,
        List<string> MutuallyExclusive,
        string? RelativeId,
        int X,
        int Y);

    private readonly FocusTree _tree;
    private readonly Focus _focus;
    private readonly List<Snapshot> _snapshots = new();
    private int _index;

    public DeleteFocusCommand(FocusTree tree, Focus focus)
    {
        _tree = tree;
        _focus = focus;
    }

    public string Description => $"Delete {_focus.Id}";

    public void Do()
    {
        _snapshots.Clear();
        _index = _tree.Focuses.IndexOf(_focus);

        // Absolute positions BEFORE anything changes, so anchored focuses can keep their place.
        var positions = FocusLayout.ResolvePositions(_tree);

        foreach (var other in _tree.Focuses)
        {
            if (ReferenceEquals(other, _focus))
                continue;

            bool touched =
                other.Prerequisites.Any(group => group.Contains(_focus.Id))
                || other.MutuallyExclusive.Contains(_focus.Id)
                || other.RelativePositionId == _focus.Id;

            if (!touched)
                continue;

            _snapshots.Add(new Snapshot(
                other,
                other.Prerequisites.Select(group => new List<string>(group)).ToList(),
                new List<string>(other.MutuallyExclusive),
                other.RelativePositionId,
                other.X,
                other.Y));

            foreach (var group in other.Prerequisites)
                group.RemoveAll(id => id == _focus.Id);
            other.Prerequisites.RemoveAll(group => group.Count == 0);

            other.MutuallyExclusive.RemoveAll(id => id == _focus.Id);

            if (other.RelativePositionId == _focus.Id)
            {
                other.RelativePositionId = null;
                if (positions.TryGetValue(other.Id, out var absolute))
                {
                    other.X = absolute.X;
                    other.Y = absolute.Y;
                }
            }
        }

        _tree.Focuses.Remove(_focus);
    }

    public void Undo()
    {
        _tree.Focuses.Insert(Math.Min(_index, _tree.Focuses.Count), _focus);

        foreach (var snapshot in _snapshots)
        {
            var owner = snapshot.Owner;

            owner.Prerequisites.Clear();
            foreach (var group in snapshot.Prerequisites)
                owner.Prerequisites.Add(new List<string>(group));

            owner.MutuallyExclusive.Clear();
            owner.MutuallyExclusive.AddRange(snapshot.MutuallyExclusive);

            owner.RelativePositionId = snapshot.RelativeId;
            owner.X = snapshot.X;
            owner.Y = snapshot.Y;
        }
    }
}

public sealed class RenameFocusCommand : IEditCommand
{
    private readonly FocusTree _tree;
    private readonly Focus _focus;
    private readonly string _oldId;
    private readonly string _newId;

    public RenameFocusCommand(FocusTree tree, Focus focus, string newId)
    {
        if (string.IsNullOrWhiteSpace(newId))
            throw new ArgumentException("A focus id cannot be empty.", nameof(newId));
        if (tree.FindFocus(newId) != null)
            throw new ArgumentException($"A focus named '{newId}' already exists.", nameof(newId));

        _tree = tree;
        _focus = focus;
        _oldId = focus.Id;
        _newId = newId;
    }

    public string Description => $"Rename {_oldId} to {_newId}";

    public void Do() => Replace(_oldId, _newId);

    public void Undo() => Replace(_newId, _oldId);

    private void Replace(string from, string to)
    {
        _focus.Id = to;

        foreach (var focus in _tree.Focuses)
        {
            foreach (var group in focus.Prerequisites)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    if (group[i] == from)
                        group[i] = to;
                }
            }

            for (int i = 0; i < focus.MutuallyExclusive.Count; i++)
            {
                if (focus.MutuallyExclusive[i] == from)
                    focus.MutuallyExclusive[i] = to;
            }

            if (focus.RelativePositionId == from)
                focus.RelativePositionId = to;
        }
    }
}

// undoable change of any single value: cost, icon, and so on.</summary>
public sealed class PropertyChangeCommand<T> : IEditCommand
{
    private readonly Action<T> _set;
    private readonly T _oldValue;
    private readonly T _newValue;

    public PropertyChangeCommand(string description, Func<T> get, Action<T> set, T newValue)
    {
        Description = description;
        _set = set;
        _oldValue = get();
        _newValue = newValue;
    }

    public string Description { get; }

    public void Do() => _set(_newValue);

    public void Undo() => _set(_oldValue);
}

public sealed class AddFocusTreeCommand : IEditCommand
{
    private readonly ModProject _project;
    private readonly FocusTree _tree;

    public AddFocusTreeCommand(ModProject project, FocusTree tree)
    {
        _project = project;
        _tree = tree;
    }

    public string Description => $"Add focus tree {_tree.Id}";

    public void Do() => _project.FocusTrees.Add(_tree);

    public void Undo() => _project.FocusTrees.Remove(_tree);
}

public sealed class AddCountryCommand : IEditCommand
{
    private readonly ModProject _project;
    private readonly Country _country;

    public AddCountryCommand(ModProject project, Country country)
    {
        _project = project;
        _country = country;
    }

    public string Description => $"Add country {_country.Tag}";

    public void Do() => _project.Countries.Add(_country);
    public void Undo() => _project.Countries.Remove(_country);
}

public sealed class RemoveCountryCommand : IEditCommand
{
    private readonly ModProject _project;
    private readonly Country _country;
    private int _index = -1;

    public RemoveCountryCommand(ModProject project, Country country)
    {
        _project = project;
        _country = country;
    }

    public string Description => $"Remove country {_country.Tag}";

    public void Do()
    {
        _index = _project.Countries.IndexOf(_country);
        if (_index >= 0 )
            _project.Countries.RemoveAt(_index);
    }

    public void Undo()
    {
        if (_index >= 0)
            _project.Countries.Insert(_index, _country);
    }

}