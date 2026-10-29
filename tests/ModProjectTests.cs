using HOI4Builder.Core.Projects;
using Xunit;

namespace HOI4Builder.Core.Tests;

public class ModIdRulesTests
{
    [Theory]
    [InlineData("My HOI4 Mod", "my_hoi4_mod")]
    [InlineData("  Spaced   Out  ", "spaced_out")]
    [InlineData("2nd Mod", "mod_2nd_mod")]
    [InlineData("!!!", "my_mod")]
    [InlineData("\u015E\u00FCkr\u00FC's Mod!", "sukru_s_mod")]
    [InlineData("\u0130stanbul \u0131\u015F\u0131k", "istanbul_isik")]
    public void FromName_builds_a_valid_id(string name, string expected)
    {
        var id = ModIdRules.FromName(name);

        Assert.Equal(expected, id);
        Assert.Null(ModIdRules.Validate(id));
    }

    [Fact]
    public void FromName_truncates_long_names()
    {
        Assert.Equal(ModIdRules.MaxLength, ModIdRules.FromName(new string('a', 100)).Length);
    }

    [Theory]
    [InlineData("my_hoi4_mod")]
    [InlineData("a")]
    [InlineData("mod_2")]
    public void Validate_accepts_good_ids(string id)
    {
        Assert.Null(ModIdRules.Validate(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("My_mod")]
    [InlineData("1mod")]
    [InlineData("my-mod")]
    [InlineData("my mod")]
    [InlineData("_mod")]
    public void Validate_rejects_bad_ids(string id)
    {
        Assert.NotNull(ModIdRules.Validate(id));
    }

    [Fact]
    public void Validate_rejects_ids_that_are_too_long()
    {
        Assert.NotNull(ModIdRules.Validate(new string('a', ModIdRules.MaxLength + 1)));
    }
}

public class ModProjectTests
{
    [Fact]
    public void CreateNew_sets_and_trims_fields()
    {
        var project = ModProject.CreateNew("  My Mod  ", "my_mod", " 1.19.* ", "/tmp/export");

        Assert.Equal("My Mod", project.Name);
        Assert.Equal("my_mod", project.ModId);
        Assert.Equal("1.19.*", project.GameVersion);
        Assert.Equal("/tmp/export", project.ExportFolder);
        Assert.Empty(project.FocusTrees);
        Assert.False(project.History.CanUndo);
    }

    [Fact]
    public void CreateNew_allows_a_missing_export_folder()
    {
        var project = ModProject.CreateNew("My Mod", "my_mod", "1.19.*", null);
        Assert.Null(project.ExportFolder);
    }

    [Fact]
    public void CreateNew_rejects_an_empty_name()
    {
        Assert.Throws<ArgumentException>(() => ModProject.CreateNew("  ", "my_mod", "1.19.*", null));
    }

    [Fact]
    public void CreateNew_rejects_an_invalid_id()
    {
        Assert.Throws<ArgumentException>(() => ModProject.CreateNew("My Mod", "My Mod", "1.19.*", null));
    }
}