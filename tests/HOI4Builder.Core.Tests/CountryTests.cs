using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;
using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class CountryAssignmentTests
{
    private static FocusTree Tree(string country) =>
        FocusTreeMapper.ReadTrees(PdxParser.Parse("focus_tree = { id = t " + country + " }"))[0];

    [Fact]
    public void Reads_the_usual_vanilla_shape()
    {
        var tree = Tree("country = { factor = 0 modifier = { add = 10 tag = GER } }");
        Assert.Equal("GER", tree.CountryTag);
    }

    [Fact]
    public void No_country_block_means_unassigned()
    {
        Assert.Null(Tree("").CountryTag);
    }

    [Fact]
    public void A_modifier_that_adds_no_weight_does_not_assign()
    {
        var tree = Tree("country = { factor = 1 modifier = { factor = 0 tag = GER } }");
        Assert.Null(tree.CountryTag);
    }

    [Fact]
    public void Build_and_read_agree()
    {
        Assert.Equal("FRA", CountryAssignment.ReadTag(CountryAssignment.Build("FRA")));
    }

    [Fact]
    public void Build_writes_the_vanilla_layout()
    {
        Assert.Equal(
            "factor = 0\nmodifier = {\n\tadd = 10\n\ttag = FRA\n}\n",
            PdxWriter.Write(CountryAssignment.Build("FRA")));
    }
}

public class CountryTagRulesTests
{
    [Theory]
    [InlineData("GER")]
    [InlineData("D01")]
    [InlineData("A1B")]
    public void Accepts_good_tags(string tag)
    {
        Assert.Null(CountryTagRules.Validate(tag));
    }

    [Theory]
    [InlineData("")]
    [InlineData("GE")]
    [InlineData("GERM")]
    [InlineData("ger")]
    [InlineData("G-R")]
    [InlineData("G R")]
    public void Rejects_bad_tags(string tag)
    {
        Assert.NotNull(CountryTagRules.Validate(tag));
    }
}

public class CountryProjectTests
{
    private static ModProject ProjectWithTrees()
    {
        var project = ModProject.CreateNew("Mod", "mod", "1.19.*", null);
        project.FocusTrees.AddRange(FocusTreeMapper.ReadTrees(PdxParser.Parse(
            "focus_tree = { id = a country = { factor = 0 modifier = { add = 10 tag = GER } } } " +
            "focus_tree = { id = b }")));
        return project;
    }

    [Fact]
    public void Trees_are_found_by_tag_and_unassigned_ones_listed()
    {
        var project = ProjectWithTrees();

        Assert.Equal("a", Assert.Single(project.TreesFor("GER")).Id);
        Assert.Equal("b", Assert.Single(project.UnassignedTrees()).Id);
    }

    [Fact]
    public void EnsureCountriesForTrees_adds_only_missing_tags()
    {
        var project = ProjectWithTrees();
        project.Countries.Add(new Country("GER", "Germany"));
        project.FocusTrees.Add(new FocusTree { Id = "c", Country = CountryAssignment.Build("FRA") });

        project.EnsureCountriesForTrees();

        Assert.Equal(2, project.Countries.Count);
        Assert.Equal("Germany", project.FindCountry("GER")!.Name);
        Assert.Equal("FRA", project.FindCountry("FRA")!.Name);
    }

    [Fact]
    public void Adding_a_country_can_be_undone()
    {
        var project = ModProject.CreateNew("Mod", "mod", "1.19.*", null);
        var stack = new UndoStack();

        stack.Execute(new AddCountryCommand(project, new Country("TST", "Testland")));
        Assert.Single(project.Countries);

        stack.Undo();
        Assert.Empty(project.Countries);
    }
}