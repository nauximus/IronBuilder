using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;
using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public sealed class ModExporterTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "hoi4builder-tests-" + Guid.NewGuid().ToString("N"));

    public ModExporterTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    // The export folder is named "mod", like the game's own, so the launcher path comes out as mod/<id>.
    private string ExportDir => Path.Combine(_root, "mod");

    private string ModFile(string relative) =>
        Path.Combine(ExportDir, "test_mod", relative.Replace('/', Path.DirectorySeparatorChar));

    private ModProject Sample()
    {
        var project = ModProject.CreateNew("Test Mod", "test_mod", "1.19.*", ExportDir);
        project.ModVersion = "1.2";
        project.Tags.Add("Gameplay");

        var tree = FocusTreeMapper.ReadTrees(PdxParser.Parse(
            "focus_tree = { id = ger_tree_1 country = { factor = 0 modifier = { add = 10 tag = GER } } " +
            "focus = { id = GER_one x = 1 y = 0 cost = 10 } " +
            "focus = { id = GER_two x = 1 y = 1 prerequisite = { focus = GER_one } } }"))[0];
        tree.Focuses[0].Name = "First Focus";
        tree.Focuses[0].Description = "Line one\nLine two";

        project.FocusTrees.Add(tree);
        project.EnsureCountriesForTrees();
        return project;
    }

    [Fact]
    public void Writes_all_the_expected_files()
    {
        var result = ModExporter.Export(Sample());

        Assert.Empty(result.Warnings);
        Assert.Equal(4, result.Written.Count);
        Assert.Equal(Path.Combine(ExportDir, "test_mod"), result.ModFolder);

        var launcher = File.ReadAllText(Path.Combine(ExportDir, "test_mod.mod"));
        Assert.Contains("name=\"Test Mod\"", launcher);
        Assert.Contains("version=\"1.2\"", launcher);
        Assert.Contains("supported_version=\"1.19.*\"", launcher);
        Assert.Contains("path=\"mod/test_mod\"", launcher);
        Assert.Contains("\"Gameplay\"", launcher);

        var descriptorPath = ModFile("descriptor.mod");
        Assert.DoesNotContain("path=", File.ReadAllText(descriptorPath));
        Assert.NotEqual(0xEF, File.ReadAllBytes(descriptorPath)[0]);

        var tree = File.ReadAllText(ModFile("common/national_focus/ger_tree_1.txt"));
        Assert.Contains("id = GER_one", tree);
        Assert.Contains("tag = GER", tree);
    }

    [Fact]
    public void Localisation_has_a_bom_and_the_right_lines()
    {
        ModExporter.Export(Sample());

        var path = ModFile("localisation/english/test_mod_focuses_l_english.yml");

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(path).Take(3).ToArray());

        var text = File.ReadAllText(path);
        Assert.StartsWith("l_english:", text);
        Assert.Contains(" GER_one:0 \"First Focus\"", text);
        Assert.Contains(" GER_one_desc:0 \"Line one\\nLine two\"", text);
        Assert.DoesNotContain("GER_two", text);
    }

    [Fact]
    public void Unchanged_files_are_skipped_and_replaced_ones_keep_a_backup()
    {
        var project = Sample();
        ModExporter.Export(project);

        var second = ModExporter.Export(project);
        Assert.Empty(second.Written);
        Assert.Equal(4, second.Unchanged);

        project.FocusTrees[0].Focuses[0].X = 5;
        var third = ModExporter.Export(project);

        Assert.Equal("test_mod/common/national_focus/ger_tree_1.txt", Assert.Single(third.Written));
        Assert.True(File.Exists(ModFile("common/national_focus/ger_tree_1.txt") + ".bak"));
    }

    [Fact]
    public void Protected_files_are_not_written_and_reported()
    {
        var project = Sample();
        project.ProtectedFiles.Add("common/national_focus/ger_tree_1.txt");

        var result = ModExporter.Export(project);

        Assert.False(File.Exists(ModFile("common/national_focus/ger_tree_1.txt")));
        Assert.Contains(result.Warnings, w => w.Contains("ger_tree_1.txt"));
    }

    [Fact]
    public void Export_needs_an_export_folder()
    {
        var project = ModProject.CreateNew("Mod", "mod", "1.19.*", null);
        Assert.Throws<InvalidOperationException>(() => ModExporter.Export(project));
    }

    [Fact]
    public void A_source_path_outside_the_mod_folder_is_refused()
    {
        var project = Sample();
        project.FocusTrees[0].SourceFile = "../evil.txt";

        Assert.Throws<InvalidDataException>(() => ModExporter.Export(project));
        Assert.False(File.Exists(Path.Combine(ExportDir, "evil.txt")));
    }

    [Fact]
    public void An_exported_mod_imports_back_the_same()
    {
        ModExporter.Export(Sample());

        var result = ModImporter.ImportFolder(Path.Combine(ExportDir, "test_mod"));
        var imported = result.Project;

        Assert.Empty(result.Warnings);
        Assert.Equal("Test Mod", imported.Name);
        Assert.Equal("test_mod", imported.ModId);
        Assert.Equal("1.2", imported.ModVersion);
        Assert.Equal("1.19.*", imported.GameVersion);
        Assert.Equal("Gameplay", Assert.Single(imported.Tags));

        var tree = Assert.Single(imported.FocusTrees);
        Assert.Equal("ger_tree_1", tree.Id);
        Assert.Equal("GER", tree.CountryTag);
        Assert.Equal(2, tree.Focuses.Count);
        Assert.Equal("GER_one", Assert.Single(Assert.Single(tree.Focuses[1].Prerequisites)));
    }
}