namespace HOI4Builder.Core.Editing;

public interface IEditCommand
{
    string Description { get; }
    void Do();
    void Undo();
}

public sealed class CompositeCommand : IEditCommand
{
    private readonly List<IEditCommand> _commands;

    public CompositeCommand(string description, IEnumerable<IEditCommand> commands)
    {
        Description = description;
        _commands = commands.ToList();
    }

    public string Description { get; }

    public void Do()
    {
        foreach (var command in _commands)
            command.Do();
    }

    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}

public sealed class UndoStack
{
    private readonly List<IEditCommand> _undo = new();
    private readonly List<IEditCommand> _redo = new();
    private IEditCommand? _savedAt;
    private IEditCommand? CurrentTop => _undo.Count > 0 ? _undo[^1] : null;
    public bool IsDirty => !ReferenceEquals(CurrentTop, _savedAt);
    public void MarkSaved()
    {
        _savedAt = CurrentTop;
        Changed?.Invoke();
    }
    private readonly int _maxDepth;

    public UndoStack(int maxDepth = 500) => _maxDepth = maxDepth;

    // raised after every execute, undo, redo or clear.
    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoDescription => CanUndo ? _undo[^1].Description : null;
    public string? RedoDescription => CanRedo ? _redo[^1].Description : null;

    public void Execute(IEditCommand command)
    {
        command.Do();
        _undo.Add(command);
        _redo.Clear();

        if (_undo.Count > _maxDepth)
            _undo.RemoveAt(0);
        
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo)
            return;
        
        var command = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        command.Undo();
        _redo.Add(command);
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo)
            return;
        
        var command = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        command.Do();
        _undo.Add(command);
        Changed?.Invoke();
    }
}