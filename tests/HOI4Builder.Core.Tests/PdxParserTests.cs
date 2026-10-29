using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class PdxParserTests
{
    private const string FocusSample = """
        # Sample focus tree
        focus_tree = {
            id = test_tree
            country = {
                factor = 0
                modifier = {
                    add = 10
                    tag = TST # the test country
                }
            }
            focus = {
                id = TST_one
                icon = GFX_goal_generic_construction
                x = 1
                y = 0
                cost = 10
                prerequisite = { focus = TST_zero }
                mutually_exclusive = { focus = TST_two focus = TST_three }
                available = { has_political_power > 50 }
                completion_reward = {
                    add_political_power = 25
                    add_ideas = { idea_a idea_b }
                    # trailing note
                }
            }
        }
        """;

    [Fact]
    public void Parses_simple_scalars()
    {
        var root = PdxParser.Parse("a = 1\nb = \"hello world\"\nc = yes\n");

        Assert.Equal(3, root.Entries.Count);
        Assert.Equal("1", root.GetString("a"));
        var b = Assert.IsType<PdxScalar>(root.Find("b")!.Value);
        Assert.True(b.Quoted);
        Assert.Equal("hello world", b.Text);
        Assert.True(((PdxScalar)root.Find("c")!.Value).IsYes);
    }

    [Fact]
    public void Parses_nested_blocks()
    {
        var root = PdxParser.Parse(FocusSample);

        var tree = root.GetBlock("focus_tree");
        Assert.NotNull(tree);
        Assert.Equal("test_tree", tree!.GetString("id"));

        var focus = tree.GetBlock("focus");
        Assert.NotNull(focus);
        Assert.Equal("TST_one", focus!.GetString("id"));
        Assert.Equal(1, focus.GetInt("x"));
        Assert.Equal(10, focus.GetInt("cost"));
        Assert.Equal("TST_zero", focus.GetBlock("prerequisite")!.GetString("focus"));
    }

    [Fact]
    public void Keeps_duplicate_keys_in_order()
    {
        var root = PdxParser.Parse("m = { focus = A focus = B }");
        var focuses = root.GetBlock("m")!.FindAll("focus")
            .Select(e => ((PdxScalar)e.Value).Text).ToList();

        Assert.Equal(2, focuses.Count);
        Assert.Equal("A", focuses[0]);
        Assert.Equal("B", focuses[1]);
    }

    [Fact]
    public void Parses_bare_values()
    {
        var root = PdxParser.Parse("tag_list = { FRA GER ENG }");
        var items = root.GetBlock("tag_list")!.BareScalars().ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal("FRA", items[0]);
        Assert.Equal("ENG", items[2]);
    }

    [Fact]
    public void Parses_all_operators()
    {
        var root = PdxParser.Parse("a > 1\nb < 2\nc >= 3\nd <= 4\ne != 5\nf ?= 6\ng = 7");
        var ops = root.Entries.Select(e => e.Operator).ToList();

        Assert.Equal(PdxOperator.Greater, ops[0]);
        Assert.Equal(PdxOperator.Less, ops[1]);
        Assert.Equal(PdxOperator.GreaterOrEqual, ops[2]);
        Assert.Equal(PdxOperator.LessOrEqual, ops[3]);
        Assert.Equal(PdxOperator.NotEqual, ops[4]);
        Assert.Equal(PdxOperator.QuestionEqual, ops[5]);
        Assert.Equal(PdxOperator.Equal, ops[6]);
    }

    [Fact]
    public void Parses_tagged_block()
    {
        var root = PdxParser.Parse("color = rgb { 255 0 0 }");
        var color = root.GetBlock("color");

        Assert.NotNull(color);
        Assert.Equal("rgb", color!.Tag);
        Assert.Equal(3, color.BareScalars().Count());
        Assert.Equal("color = rgb { 255 0 0 }\n", PdxWriter.Write(root));
    }

    [Fact]
    public void Parses_special_atoms()
    {
        var root = PdxParser.Parse("x = -0.5\nscope = ROOT.owner\nv = var:foo\nk = @const");

        Assert.Equal("-0.5", root.GetString("x"));
        Assert.Equal("ROOT.owner", root.GetString("scope"));
        Assert.Equal("var:foo", root.GetString("v"));
        Assert.Equal("@const", root.GetString("k"));
    }

    [Fact]
    public void Preserves_comments()
    {
        const string src = "# header\na = 1 # one\n# lead b\nb = 2\nc = {\n x = 1\n # dangling\n}\n";
        var root = PdxParser.Parse(src);

        Assert.Equal(" header", Assert.Single(root.Entries[0].LeadingComments));
        Assert.Equal(" one", root.Entries[0].TrailingComment);
        Assert.Equal(" lead b", Assert.Single(root.Entries[1].LeadingComments));
        Assert.Equal(" dangling", Assert.Single(root.GetBlock("c")!.DanglingComments));
    }

    [Fact]
    public void Handles_crlf_and_bom()
    {
        var root = PdxParser.Parse("\uFEFFa = 1 # one\r\nb = 2\r\n");

        Assert.Equal("1", root.GetString("a"));
        Assert.Equal(" one", root.Entries[0].TrailingComment);
        Assert.Equal("2", root.GetString("b"));
    }

    [Fact]
    public void Reports_missing_closing_brace_with_line()
    {
        var ex = Assert.Throws<PdxParseException>(() => PdxParser.Parse("a = {\n  b = 1\n"));

        Assert.Contains("missing '}'", ex.Message);
        Assert.Equal(3, ex.Line);
    }

    [Fact]
    public void Reports_stray_closing_brace()
    {
        var ex = Assert.Throws<PdxParseException>(() => PdxParser.Parse("a = 1 }"));
        Assert.Equal(1, ex.Line);
    }

    [Fact]
    public void Reports_missing_value()
    {
        Assert.Throws<PdxParseException>(() => PdxParser.Parse("a ="));
    }

    [Fact]
    public void TryParse_returns_false_on_error()
    {
        Assert.False(PdxParser.TryParse("a = {", out var result, out var error));
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void Writer_produces_expected_layout()
    {
        var root = PdxParser.Parse("focus = { id = TEST x = 1 }");
        Assert.Equal("focus = {\n\tid = TEST\n\tx = 1\n}\n", PdxWriter.Write(root));
    }

    [Fact]
    public void Writer_keeps_comments_and_quotes()
    {
        const string src = "# h\nname = \"Hello World\" # t\n";
        Assert.Equal(src, PdxWriter.Write(PdxParser.Parse(src)));
    }

    [Fact]
    public void Writer_quotes_values_with_spaces_when_building_in_code()
    {
        var root = new PdxBlock();
        root.AddBlock("focus")
            .Add("id", "TST_one")
            .Add("x", "1")
            .Add("name", "My Focus");

        Assert.Equal("focus = {\n\tid = TST_one\n\tx = 1\n\tname = \"My Focus\"\n}\n", PdxWriter.Write(root));
    }

    [Fact]
    public void Round_trip_is_stable()
    {
        var first = PdxWriter.Write(PdxParser.Parse(FocusSample));
        var second = PdxWriter.Write(PdxParser.Parse(first));

        Assert.Equal(first, second);
    }
}