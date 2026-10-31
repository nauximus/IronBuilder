using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;
using HOI4Builder.Core.Script;
using Xunit;

namespace HOI4Builder.Core.Tests;

public sealed class ProjectFileTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "hoi4builder-tests-" + Guid.NewGuid().ToString("N"));

    public ProjectFileTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static ModProject Sample()
    {
        var project = ModProject.CreateNew("Test Mod", "test_mod", "1.19.*", "/tmp/out");
        project.ModVersion = "1.2";
        project.Tags.Add("Gameplay");

        var tree = FocusTreeMapper.ReadTrees(PdxParser.Parse(
            "focus_tree = { id = t focus = { id = A x = 1 y = 2 cancel_if_invalid = yes } }"))[0];
        tree.SourceFile = "common/national_focus/t.txt";
        project.FocusTrees.Add(tree);

        return project;
    }

    [Fact]
    public void Round_trip_keeps_everything()
    {
        var loaded = ProjectFile.Deserialize(ProjectFile.Serialize(Sample()));

        Assert.Equal("Test Mod", loaded.Name);
        Assert.Equal("test_mod", loaded.ModId);
        Assert.Equal("1.19.*", loaded.GameVersion);
        Assert.Equal("1.2", loaded.ModVersion);
        Assert.Equal("/tmp/out", loaded.ExportFolder);
        Assert.Equal("Gameplay", Assert.Single(loaded.Tags));

        var tree = Assert.Single(loaded.FocusTrees);
        Assert.Equal("common/national_focus/t.txt", tree.SourceFile);

        var focus = Assert.Single(tree.Focuses);
        Assert.Equal(1, focus.X);
        Assert.Equal(2, focus.Y);
        Assert.Equal("cancel_if_invalid", Assert.Single(focus.ExtraEntries).Key);
    }

    [Fact]
    public void Save_and_load_set_the_file_path()
    {
        var path = Path.Combine(_root, "p" + ProjectFile.Extension);
        var project = Sample();

        ProjectFile.Save(project, path);

        Assert.Equal(path, project.FilePath);
        var loaded = ProjectFile.Load(path);
        Assert.Equal(path, loaded.FilePath);
        Assert.Equal("Test Mod", loaded.Name);
    }

    [Fact]
    public void Saving_again_keeps_the_previous_version_as_bak()
    {
        var path = Path.Combine(_root, "p" + ProjectFile.Extension);
        var project = Sample();
        ProjectFile.Save(project, path);

        project.Name = "Changed";
        ProjectFile.Save(project, path);

        Assert.True(File.Exists(path + ".bak"));
        Assert.Equal("Test Mod", ProjectFile.Load(path + ".bak").Name);
        Assert.Equal("Changed", ProjectFile.Load(path).Name);
    }

    [Fact]
    public void No_temp_file_is_left_behind()
    {
        var path = Path.Combine(_root, "p" + ProjectFile.Extension);
        ProjectFile.Save(Sample(), path);

        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void A_newer_format_is_rejected()
    {
        Assert.Throws<ProjectFileException>(() => ProjectFile.Deserialize("{ \"FormatVersion\": 99 }"));
    }

    [Fact]
    public void Invalid_json_is_rejected()
    {
        Assert.Throws<ProjectFileException>(() => ProjectFile.Deserialize("not json"));
    }

    [Fact]
    public void An_empty_object_is_not_a_project()
    {
        Assert.Throws<ProjectFileException>(() => ProjectFile.Deserialize("{}"));
    }
}