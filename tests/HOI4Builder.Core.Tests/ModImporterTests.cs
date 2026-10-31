using HOI4Builder.Core.Projects;
using Xunit;

namespace HOI4Builder.Core.Tests;

public sealed class ModImporterTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "hoi4builder-tests-" + Guid.NewGuid().ToString("N"));

    public ModImporterTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private void Write(string relativePath, string text)
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private const string Descriptor =
        "version=\"1.2\"\n" +
        "tags={\n\t\"Alternative History\"\n\t\"Gameplay\"\n}\n" +
        "name=\"Test Mod\"\n" +
        "supported_version=\"1.19.*\"\n";

    [Fact]
    public void Imports_descriptor_and_focus_trees()
    {
        Write("descriptor.mod", Descriptor);
        Write("common/national_focus/a.txt", "focus_tree = { id = tree_a focus = { id = A_one x = 0 y = 0 } }");
        Write("common/national_focus/b.txt", "focus_tree = { id = tree_b }");

        var result = ModImporter.ImportFolder(_root);
        var project = result.Project;

        Assert.Empty(result.Warnings);
        Assert.Equal("Test Mod", project.Name);
        Assert.Equal("1.19.*", project.GameVersion);
        Assert.Equal("1.2", project.ModVersion);
        Assert.Equal(2, project.Tags.Count);
        Assert.Equal(_root, project.ImportFromFolder);
        Assert.Null(ModIdRules.Validate(project.ModId));

        Assert.Equal(2, project.FocusTrees.Count);
        Assert.Equal("tree_a", project.FocusTrees[0].Id);
        Assert.Equal("common/national_focus/a.txt", project.FocusTrees[0].SourceFile);
        Assert.Single(project.FocusTrees[0].Focuses);
    }

    [Fact]
    public void Missing_descriptor_uses_the_folder_name_and_warns()
    {
        var result = ModImporter.ImportFolder(_root);

        Assert.Equal(Path.GetFileName(_root), result.Project.Name);
        Assert.Contains(result.Warnings, w => w.Contains("descriptor.mod"));
    }

    [Fact]
    public void A_broken_file_becomes_a_warning_and_the_rest_still_imports()
    {
        Write("descriptor.mod", Descriptor);
        Write("common/national_focus/bad.txt", "focus_tree = {\n id = broken\n");
        Write("common/national_focus/good.txt", "focus_tree = { id = fine }");

        var result = ModImporter.ImportFolder(_root);

        Assert.Equal("fine", Assert.Single(result.Project.FocusTrees).Id);
        Assert.Contains(result.Warnings, w => w.Contains("bad.txt") && w.Contains("line"));
    }

    [Fact]
    public void Unsupported_top_level_blocks_are_reported()
    {
        Write("descriptor.mod", Descriptor);
        Write("common/national_focus/mixed.txt",
            "shared_focus = { id = SHARED_ONE }\nfocus_tree = { id = main }");

        var result = ModImporter.ImportFolder(_root);

        Assert.Single(result.Project.FocusTrees);
        Assert.Contains(result.Warnings, w => w.Contains("mixed.txt") && w.Contains("shared_focus"));
    }

    [Fact]
    public void A_folder_without_focus_files_gives_an_empty_project()
    {
        Write("descriptor.mod", Descriptor);

        var result = ModImporter.ImportFolder(_root);

        Assert.Empty(result.Project.FocusTrees);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void A_missing_folder_throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            ModImporter.ImportFolder(Path.Combine(_root, "does-not-exist")));
    }
}