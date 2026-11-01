using HOI4Builder.Core.Focuses;

namespace HOI4Builder.Core.Editing;

public sealed class AddPrerequisiteCommand : IEditCommand
{
    private readonly Focus _target;
    private readonly string _prerequisiteId;
    private List<string>? _group;

    public AddPrerequisiteCommand(Focus target, string prerequisiteId)
    {
        _target = target;
        _prerequisiteId = prerequisiteId;
    }


    public string Description => $"Require {_prerequisiteId} for {_target.Id}";

    public void Do()
    {
        _group = new List<string> { _prerequisiteId };
        _target.Prerequisites.Add(_group);
    }

    public void Undo()
    {
        if (_group != null)
            _target.Prerequisites.Remove(_group);
    }
}

public sealed class RemovePrerequisiteCommand : IEditCommand
{
    private readonly Focus _target;
    private readonly string _prerequisiteId;
    private List<List<string>> _before = new();

    public RemovePrerequisiteCommand(Focus target, string prerequisiteId)
    {
        _target = target;
        _prerequisiteId = prerequisiteId;
    }
    public string Description => $"Remove {_prerequisiteId} from the prerequisites of {_target.Id}";

    public void Do()
    {
        _before = _target.Prerequisites.Select(group => new List<string>(group)).ToList();

        foreach (var group in _target.Prerequisites)
            group.RemoveAll(id => id == _prerequisiteId);

        _target.Prerequisites.RemoveAll(group => group.Count == 0);
    }

    public void Undo()
    {
        _target.Prerequisites.Clear();
        foreach (var group in _before)
            _target.Prerequisites.Add(new List<string>(group));
    }
}

public sealed class ToggleExclusiveCommand : IEditCommand
{
    private readonly Focus _a;
    private readonly Focus _b;
    private readonly List<string> _aBefore;
    private readonly List<string> _bBefore;
    private readonly bool _linked;

    public ToggleExclusiveCommand(Focus a, Focus b)
    {
        _a = a;
        _b = b;
        _aBefore = new List<string>(a.MutuallyExclusive);
        _bBefore = new List<string>(b.MutuallyExclusive);
        _linked = a.MutuallyExclusive.Contains(b.Id) || b.MutuallyExclusive.Contains(a.Id);
    }

    public string Description => _linked
        ? $"Remove exclusive link between {_a.Id} and {_b.Id}"
        : $"Make {_a.Id} and {_b.Id} mutually exclusive";

    public void Do()
    {
        if (_linked)
        {
            _a.MutuallyExclusive.RemoveAll(id => id == _b.Id);
            _b.MutuallyExclusive.RemoveAll(id => id == _a.Id);
        }
        else
        {
            if (!_a.MutuallyExclusive.Contains(_b.Id))
                _a.MutuallyExclusive.Add(_b.Id);
            if (!_b.MutuallyExclusive.Contains(_a.Id))
                _b.MutuallyExclusive.Add(_a.Id);
        }
    }
    
    public void Undo()
    {
        _a.MutuallyExclusive.Clear();
        _a.MutuallyExclusive.AddRange(_aBefore);
        _b.MutuallyExclusive.Clear();
        _b.MutuallyExclusive.AddRange(_bBefore);
    }
}

// decided what a click means and refuses invalid ones
public static class FocusLinkEditing
{
    public static IEditCommand? TogglePrerequisite(FocusTree tree, Focus target, string prerequisiteId, out string? error)
    {
        error = null;
         
        if (target.Id == prerequisiteId)
        {
            error = "A focus cannot require itself.";
            return null;
        }

        if (FocusLinks.HasPrerequisite(target, prerequisiteId))
            return new RemovePrerequisiteCommand(target, prerequisiteId);

        if (FocusLinks.WouldCreateCycle(tree, prerequisiteId, target.Id))
        {
            error = $"{prerequisiteId} already depends on {target.Id}, so this would make a loop.";
            return null;
        }

        return new AddPrerequisiteCommand(target, prerequisiteId);
    }

    public static IEditCommand? ToggleExclusive(Focus a, Focus b, out string? error)
    {
        error = null;
        if (ReferenceEquals(a, b))
        {
            error = "A focus cannot be exclusive with itself.";
            return null;
        }

        return new ToggleExclusiveCommand(a, b);
    }
}