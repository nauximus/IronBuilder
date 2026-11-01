using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;
using HOI4Builder.Core.Projects;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class StructureCommandTests
{
    private static FocusTree Tree(string focuses) =>
        FocusTreeMapper.ReadTrees(PdxParser.Parse("focus_tree = { id = t " + focuses + " }"))[0];

    private const string Linked =
        "focus = { id = A x = 2 y = 3 } " +
        "focus = { id = B x = 0 y = 1 prerequisite = { focus = A } mutually_exclusive = { focus = A } } " +
        "focus = { id = C x = 1 y = 1 relative_position_id = A prerequisite = { focus = A focus = B } }";

    [Fact]
    public void Add_and_undo()
    {
        var tree = Tree("focus = { id = A x = 0 y = 0 }");
        var stack = new UndoStack();

        stack.Execute(new AddFocusCommand(tree, new Focus { Id = "B" }));
        Assert.Equal(2, tree.Focuses.Count);

        stack.Undo();
        Assert.Single(tree.Focuses);
    }

    [Fact]
    public void Delete_cleans_references_and_keeps_anchored_positions()
    {
        var tree = Tree(Linked);
        var stack = new UndoStack();

        stack.Execute(new DeleteFocusCommand(tree, tree.FindFocus("A")!));

        Assert.Null(tree.FindFocus("A"));

        var b = tree.FindFocus("B")!;
        Assert.Empty(b.Prerequisites);
        Assert.Empty(b.MutuallyExclusive);

        var c = tree.FindFocus("C")!;
        Assert.Null(c.RelativePositionId);
        Assert.Equal(3, c.X);
        Assert.Equal(4, c.Y);
        Assert.Equal(new List<string> { "B" }, Assert.Single(c.Prerequisites));
    }

    [Fact]
    public void Delete_undo_restores_everything()
    {
        var tree = Tree(Linked);
        var stack = new UndoStack();
        stack.Execute(new DeleteFocusCommand(tree, tree.FindFocus("A")!));

        stack.Undo();

        Assert.Equal("A", tree.Focuses[0].Id);

        var b = tree.FindFocus("B")!;
        Assert.Equal("A", Assert.Single(Assert.Single(b.Prerequisites)));
        Assert.Equal("A", Assert.Single(b.MutuallyExclusive));

        var c = tree.FindFocus("C")!;
        Assert.Equal("A", c.RelativePositionId);
        Assert.Equal(1, c.X);
        Assert.Equal(1, c.Y);
        Assert.Equal(new List<string> { "A", "B" }, Assert.Single(c.Prerequisites));
    }

    [Fact]
    public void Rename_updates_references_and_undoes()
    {
        var tree = Tree(Linked);
        var stack = new UndoStack();

        stack.Execute(new RenameFocusCommand(tree, tree.FindFocus("A")!, "A2"));

        Assert.Null(tree.FindFocus("A"));
        Assert.NotNull(tree.FindFocus("A2"));

        var b = tree.FindFocus("B")!;
        Assert.Equal("A2", Assert.Single(Assert.Single(b.Prerequisites)));
        Assert.Equal("A2", Assert.Single(b.MutuallyExclusive));
        Assert.Equal("A2", tree.FindFocus("C")!.RelativePositionId);

        stack.Undo();

        Assert.NotNull(tree.FindFocus("A"));
        Assert.Equal("A", tree.FindFocus("C")!.RelativePositionId);
        Assert.Equal("A", Assert.Single(tree.FindFocus("B")!.MutuallyExclusive));
    }

    [Fact]
    public void Rename_rejects_an_existing_id()
    {
        var tree = Tree(Linked);
        Assert.Throws<ArgumentException>(() => new RenameFocusCommand(tree, tree.FindFocus("A")!, "B"));
    }

    [Fact]
    public void Property_change_round_trip()
    {
        var focus = new Focus { Id = "A" };
        var stack = new UndoStack();

        stack.Execute(new PropertyChangeCommand<double?>("Set cost", () => focus.Cost, v => focus.Cost = v, 45));
        Assert.Equal(45, focus.Cost);

        stack.Undo();
        Assert.Null(focus.Cost);
    }

    [Fact]
    public void MakeUniqueId_skips_taken_ids()
    {
        var tree = Tree("focus = { id = NEW_1 x = 0 y = 0 }");
        Assert.Equal("NEW_2", tree.MakeUniqueId("NEW_"));
    }

    [Fact]
    public void Add_tree_and_undo()
    {
        var project = ModProject.CreateNew("Mod", "mod", "1.19.*", null);
        var stack = new UndoStack();
        var tree = new FocusTree { Id = project.MakeUniqueTreeId("new_tree_") };

        stack.Execute(new AddFocusTreeCommand(project, tree));
        Assert.Single(project.FocusTrees);

        stack.Undo();
        Assert.Empty(project.FocusTrees);

        stack.Redo();
        Assert.Same(tree, Assert.Single(project.FocusTrees));
    }

    [Fact]
    public void MakeUniqueTreeId_skips_taken_ids()
    {
        var project = ModProject.CreateNew("Mod", "mod", "1.19.*", null);
        project.FocusTrees.Add(new FocusTree { Id = "new_tree_1" });

        Assert.Equal("new_tree_2", project.MakeUniqueTreeId("new_tree_"));
    }
}