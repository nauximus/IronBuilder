using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class FocusLayoutTests
{
    private static Dictionary<string, GridPoint> Resolve(string focuses)
    {
        var tree = FocusTreeMapper.ReadTrees(PdxParser.Parse("focus_tree = { id = t " + focuses + " }"))[0];
        return FocusLayout.ResolvePositions(tree);
    }

    [Fact]
    public void Absolute_positions_pass_through()
    {
        var pos = Resolve("focus = { id = A x = 2 y = 3 }");
        Assert.Equal(new GridPoint(2, 3), pos["A"]);
    }

    [Fact]
    public void Relative_position_adds_the_anchor()
    {
        var pos = Resolve("focus = { id = A x = 2 y = 3 } focus = { id = B x = 1 y = 1 relative_position_id = A }");
        Assert.Equal(new GridPoint(3, 4), pos["B"]);
    }

    [Fact]
    public void Chains_of_relative_positions_resolve()
    {
        var pos = Resolve(
            "focus = { id = C x = 1 y = 1 relative_position_id = B } " +
            "focus = { id = B x = 1 y = 1 relative_position_id = A } " +
            "focus = { id = A x = 5 y = 5 }");
        Assert.Equal(new GridPoint(7, 7), pos["C"]);
    }

    [Fact]
    public void Missing_anchor_falls_back_to_own_position()
    {
        var pos = Resolve("focus = { id = A x = 2 y = 3 relative_position_id = NOPE }");
        Assert.Equal(new GridPoint(2, 3), pos["A"]);
    }

    [Fact]
    public void Cycle_does_not_hang()
    {
        var pos = Resolve(
            "focus = { id = A x = 1 y = 0 relative_position_id = B } " +
            "focus = { id = B x = 0 y = 1 relative_position_id = A }");
        Assert.Equal(2, pos.Count);
    }

    [Fact]
    public void ToOwnCoordinates_subtracts_the_anchor()
    {
        var tree = FocusTreeMapper.ReadTrees(PdxParser.Parse(
            "focus_tree = { id = t focus = { id = A x = 5 y = 5 } focus = { id = B x = 1 y = 1 relative_position_id = A } }"))[0];
        var positions = FocusLayout.ResolvePositions(tree);

        var own = FocusLayout.ToOwnCoordinates(tree.FindFocus("B")!, positions, new GridPoint(8, 4));

        Assert.Equal(new GridPoint(3, -1), own);
    }

    [Fact]
    public void ToOwnCoordinates_without_an_anchor_is_unchanged()
    {
        var tree = FocusTreeMapper.ReadTrees(PdxParser.Parse("focus_tree = { id = t focus = { id = A x = 5 y = 5 } }"))[0];
        var positions = FocusLayout.ResolvePositions(tree);

        var own = FocusLayout.ToOwnCoordinates(tree.FindFocus("A")!, positions, new GridPoint(2, 3));

        Assert.Equal(new GridPoint(2, 3), own);
    }
}