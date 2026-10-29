using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class FocusTreeMapperTests
{
    private const string Sample = """
        focus_tree = {
            id = test_tree
            default = yes
            shared_focus = SHARED_ONE
            focus = {
                id = TST_one
                icon = GFX_goal_generic_construction
                x = 1
                y = 0
                cost = 1.5
                prerequisite = { focus = TST_a focus = TST_b }
                prerequisite = { focus = TST_c }
                mutually_exclusive = { focus = TST_two }
                search_filters = { FOCUS_FILTER_POLITICAL }
                available = { has_political_power > 50 }
                cancel_if_invalid = yes
                completion_reward = { add_political_power = 25 }
            }
        }
        """;

    private static List<FocusTree> Import(string text) =>
        FocusTreeMapper.ReadTrees(PdxParser.Parse(text));

    private static string Export(IEnumerable<FocusTree> trees) =>
        PdxWriter.Write(FocusTreeMapper.ToDocument(trees));

    [Fact]
    public void Imports_tree_and_focus_fields()
    {
        var tree = Assert.Single(Import(Sample));

        Assert.Equal("test_tree", tree.Id);
        Assert.True(tree.IsDefault);
        Assert.Equal("SHARED_ONE", Assert.Single(tree.SharedFocusIds));

        var focus = Assert.Single(tree.Focuses);
        Assert.Equal("TST_one", focus.Id);
        Assert.Equal("GFX_goal_generic_construction", focus.Icon);
        Assert.Equal(1, focus.X);
        Assert.Equal(0, focus.Y);
        Assert.Equal(1.5, focus.Cost!.Value);
    }

    [Fact]
    public void Keeps_prerequisite_groups_separate()
    {
        var focus = Import(Sample)[0].Focuses[0];

        Assert.Equal(2, focus.Prerequisites.Count);
        Assert.Equal(new List<string> { "TST_a", "TST_b" }, focus.Prerequisites[0]);
        Assert.Equal(new List<string> { "TST_c" }, focus.Prerequisites[1]);
        Assert.Equal("TST_two", Assert.Single(focus.MutuallyExclusive));
        Assert.Equal("FOCUS_FILTER_POLITICAL", Assert.Single(focus.SearchFilters));
    }

    [Fact]
    public void Keeps_raw_blocks_and_unknown_keys()
    {
        var focus = Import(Sample)[0].Focuses[0];

        Assert.NotNull(focus.Available);
        Assert.Equal(25, focus.CompletionReward!.GetInt("add_political_power"));
        Assert.Equal("cancel_if_invalid", Assert.Single(focus.ExtraEntries).Key);
    }

    [Fact]
    public void Missing_cost_stays_missing()
    {
        var focus = Import("focus_tree = { id = t focus = { id = F x = 0 y = 0 } }")[0].Focuses[0];

        Assert.Null(focus.Cost);
        Assert.DoesNotContain("cost", Export(Import("focus_tree = { id = t focus = { id = F x = 0 y = 0 } }")));
    }

    [Fact]
    public void Exports_a_tree_built_in_code()
    {
        var tree = new FocusTree { Id = "t" };
        var focus = new Focus { Id = "TST_one", X = 2, Y = 1 };
        focus.Prerequisites.Add(new List<string> { "TST_zero" });
        tree.Focuses.Add(focus);

        const string expected =
            "focus_tree = {\n" +
            "\tid = t\n" +
            "\tfocus = {\n" +
            "\t\tid = TST_one\n" +
            "\t\tx = 2\n" +
            "\t\ty = 1\n" +
            "\t\tprerequisite = {\n" +
            "\t\t\tfocus = TST_zero\n" +
            "\t\t}\n" +
            "\t}\n" +
            "}\n";

        Assert.Equal(expected, Export(new[] { tree }));
    }

    [Fact]
    public void Round_trip_is_stable_and_loses_nothing()
    {
        var first = Export(Import(Sample));
        var second = Export(Import(first));

        Assert.Equal(first, second);
        Assert.Contains("cancel_if_invalid = yes", first);
        Assert.Contains("cost = 1.5", first);
        Assert.Contains("shared_focus = SHARED_ONE", first);
    }
}