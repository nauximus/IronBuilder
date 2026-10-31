using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class UndoStackTests
{
    private sealed class LogCommand : IEditCommand
    {
        private readonly List<string> _log;
        private readonly string _name;

        public LogCommand(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public string Description => _name;
        public void Do() => _log.Add("do " + _name);
        public void Undo() => _log.Add("undo " + _name);
    }

    [Fact]
    public void Execute_applies_and_enables_undo()
    {
        var focus = new Focus { Id = "A", X = 1, Y = 1 };
        var stack = new UndoStack();

        stack.Execute(new MoveFocusCommand(focus, 4, 5));

        Assert.Equal(4, focus.X);
        Assert.Equal(5, focus.Y);
        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
        Assert.Equal("Move A", stack.UndoDescription);
    }

    [Fact]
    public void Undo_and_redo_round_trip()
    {
        var focus = new Focus { Id = "A", X = 1, Y = 1 };
        var stack = new UndoStack();
        stack.Execute(new MoveFocusCommand(focus, 4, 5));

        stack.Undo();
        Assert.Equal(1, focus.X);
        Assert.Equal(1, focus.Y);
        Assert.True(stack.CanRedo);

        stack.Redo();
        Assert.Equal(4, focus.X);
        Assert.Equal(5, focus.Y);
    }

    [Fact]
    public void New_edit_clears_redo()
    {
        var focus = new Focus { Id = "A" };
        var stack = new UndoStack();
        stack.Execute(new MoveFocusCommand(focus, 1, 1));
        stack.Undo();

        stack.Execute(new MoveFocusCommand(focus, 2, 2));

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Composite_undoes_in_reverse_order()
    {
        var log = new List<string>();
        var stack = new UndoStack();
        stack.Execute(new CompositeCommand("both", new IEditCommand[]
        {
            new LogCommand(log, "a"),
            new LogCommand(log, "b"),
        }));

        stack.Undo();

        Assert.Equal(new List<string> { "do a", "do b", "undo b", "undo a" }, log);
    }

    [Fact]
    public void Changed_fires_on_execute_undo_and_redo()
    {
        var log = new List<string>();
        var stack = new UndoStack();
        int count = 0;
        stack.Changed += () => count++;

        stack.Execute(new LogCommand(log, "a"));
        stack.Undo();
        stack.Redo();

        Assert.Equal(3, count);
    }

    [Fact]
    public void Max_depth_drops_the_oldest_commands()
    {
        var log = new List<string>();
        var stack = new UndoStack(maxDepth: 2);
        stack.Execute(new LogCommand(log, "a"));
        stack.Execute(new LogCommand(log, "b"));
        stack.Execute(new LogCommand(log, "c"));

        stack.Undo();
        stack.Undo();
        stack.Undo(); // nothing left to undo

        Assert.Contains("undo c", log);
        Assert.Contains("undo b", log);
        Assert.DoesNotContain("undo a", log);
    }
    
    [Fact]
    public void Dirty_tracking_follows_the_saved_point()
    {
        var focus = new Focus { Id = "A" };
        var stack = new UndoStack();
        Assert.False(stack.IsDirty);

        stack.Execute(new MoveFocusCommand(focus, 1, 1));
        Assert.True(stack.IsDirty);

        stack.MarkSaved();
        Assert.False(stack.IsDirty);

        stack.Undo();
        Assert.True(stack.IsDirty);

        stack.Redo();
        Assert.False(stack.IsDirty);
    }
}