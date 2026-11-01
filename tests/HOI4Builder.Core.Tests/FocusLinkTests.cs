using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class FocusLinkTests
{
    private const string Chain =
        "focus = { id = A x = 0 y = 0 } " +
        "focus = { id = B x = 0 y = 1 prerequisite = { focus = A } } " +
        "focus = { id = C x = 0 y = 2 prerequisite = { focus = B } }";

    private static FocusTree Tree(string focuses) =>
        FocusTreeMapper.ReadTrees(PdxParser.Parse("focus_tree = { id = t " + focuses + " }"))[0];

    [Fact]
    public void Detects_loops()
    {
        var tree = Tree(Chain);

        Assert.True(FocusLinks.WouldCreateCycle(tree, "C", "A"));
        Assert.True(FocusLinks.WouldCreateCycle(tree, "B", "B"));
        Assert.False(FocusLinks.WouldCreateCycle(tree, "A", "C"));
    }

    [Fact]
    public void A_loop_is_refused_with_a_message()
    {
        var tree = Tree(Chain);

        var command = FocusLinkEditing.TogglePrerequisite(tree, tree.FindFocus("A")!, "C", out var error);

        Assert.Null(command);
        Assert.NotNull(error);
    }

    [Fact]
    public void Toggle_prerequisite_adds_removes_and_undoes()
    {
        var tree = Tree("focus = { id = A x = 0 y = 0 } focus = { id = B x = 0 y = 1 }");
        var b = tree.FindFocus("B")!;
        var stack = new UndoStack();

        stack.Execute(FocusLinkEditing.TogglePrerequisite(tree, b, "A", out var error)!);
        Assert.Null(error);
        Assert.Equal("A", Assert.Single(Assert.Single(b.Prerequisites)));

        stack.Execute(FocusLinkEditing.TogglePrerequisite(tree, b, "A", out error)!);
        Assert.Empty(b.Prerequisites);

        stack.Undo();
        Assert.Equal("A", Assert.Single(Assert.Single(b.Prerequisites)));
    }

    [Fact]
    public void Removing_one_member_of_an_or_group_keeps_the_rest_and_undo_restores_it()
    {
        var tree = Tree(
            "focus = { id = A x = 0 y = 0 } focus = { id = B x = 1 y = 0 } " +
            "focus = { id = C x = 0 y = 1 prerequisite = { focus = A focus = B } }");
        var c = tree.FindFocus("C")!;
        var stack = new UndoStack();

        stack.Execute(FocusLinkEditing.TogglePrerequisite(tree, c, "A", out _)!);
        Assert.Equal("B", Assert.Single(Assert.Single(c.Prerequisites)));

        stack.Undo();
        Assert.Equal(new List<string> { "A", "B" }, Assert.Single(c.Prerequisites));
    }

    [Fact]
    public void Exclusive_links_are_written_on_both_focuses_and_toggle_off()
    {
        var tree = Tree("focus = { id = A x = 0 y = 0 } focus = { id = B x = 1 y = 0 }");
        var a = tree.FindFocus("A")!;
        var b = tree.FindFocus("B")!;
        var stack = new UndoStack();

        stack.Execute(FocusLinkEditing.ToggleExclusive(a, b, out _)!);
        Assert.Equal("B", Assert.Single(a.MutuallyExclusive));
        Assert.Equal("A", Assert.Single(b.MutuallyExclusive));

        stack.Execute(FocusLinkEditing.ToggleExclusive(a, b, out _)!);
        Assert.Empty(a.MutuallyExclusive);
        Assert.Empty(b.MutuallyExclusive);

        stack.Undo();
        Assert.Single(a.MutuallyExclusive);
        Assert.Single(b.MutuallyExclusive);
    }

    [Fact]
    public void A_focus_cannot_be_exclusive_with_itself()
    {
        var tree = Tree("focus = { id = A x = 0 y = 0 }");
        var a = tree.FindFocus("A")!;

        Assert.Null(FocusLinkEditing.ToggleExclusive(a, a, out var error));
        Assert.NotNull(error);
    }
}