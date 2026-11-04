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

        project.Countries.Add(new Country("TST", "Testland"));

        var tree = FocusTreeMapper.ReadTrees(PdxParser.Parse(
            "focus_tree = { id = t focus = { id = A x = 1 y = 2 cancel_if_invalid = yes } }"))[0];
        tree.SourceFile = "common/national_focus/t.txt";
        project.FocusTrees.Add(tree);

        tree.Focuses[0].Name = "Alpha";
        project.ProtectedFiles.Add("common/national_focus/x.txt");

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

        Assert.Equal("Testland", loaded.FindCountry("TST")!.Name);
        var focus = Assert.Single(tree.Focuses);
        Assert.Equal(1, focus.X);
        Assert.Equal(2, focus.Y);
        Assert.Equal("cancel_if_invalid", Assert.Single(focus.ExtraEntries).Key);
        Assert.Equal("Alpha", focus.Name);
        Assert.Contains("common/national_focus/x.txt", loaded.ProtectedFiles);
    }

    [Fact]
    public void Files_without_countries_still_load()
    {
        var project = ProjectFile.Deserialize(
            "{ \"FormatVersion\": 1, \"Name\": \"x\", \"ModId\": \"x\", \"GameVersion\": \"1.19.*\" }");

        Assert.Empty(project.Countries);
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

        [Fact]
    public void Country_details_and_ideologies_round_trip()
    {
        var project = Sample();
        project.Ideologies.Clear();
        var group = new IdeologyGroup("monarchism");
        group.SubIdeologies.Add("absolutism");
        project.Ideologies.Add(group);

        var country = project.FindCountry("TST")!;
        country.DefinitieName = "the Republic of Testland";
        country.Adjective = "Testlandic";
        country.CapitalStateId = 64;
        country.RulingIdeology = "monarchism";
        country.InitialLeaderSubIdeology = "absolutism";
        country.ElectionsAllowed = false;
        country.ElectionFrequency = 60;
        country.LastElection = new DateOnly(1936, 1, 1);
        country.PartyPopularity["monarchism"] = 100;
        country.PartyNames["monarchism"] = new PartyName { Name = "RP", LongName = "Royal Party" };
        country.IdeologyNames["monarchism"] = "Royalism";
        country.Stability = 72.5;
        country.WarSupport = 15;
        country.ResearchSlots = 4;
        country.Convoys = 200;
        country.Subjects.Add(new SubjectRelation { TargetTag = "SUB", AutonomyType = "autonomy_puppet", FreedomPercent = 30 });
        country.StartingTechnologies.Add("infantry_weapons");
        country.StartingLaws.Add("civilian_economy");
        country.Stockpile["infantry_equipment_1"] = 500;
        country.CosmeticIdentities.Add(new CosmeticIdentity
        {
            Id = "TST_fascist", DisplayName = "Test Reich", FlagFile = "TST_fascist.tga", Color = new Rgb(200, 10, 10)
        });
        country.HistoryScript = "1936.1.1 = { }";
        country.GraphicalCulture = "western_european_gfx";
        country.GraphicalCulture2d = "western_european_2d";
        country.MapColor = new Rgb(10, 20, 30);

        var loaded = ProjectFile.Deserialize(ProjectFile.Serialize(project));

        var ideology = Assert.Single(loaded.Ideologies);
        Assert.Equal("monarchism", ideology.Id);
        Assert.Equal("absolutism", Assert.Single(ideology.SubIdeologies));

        var c = loaded.FindCountry("TST")!;
        Assert.Equal("the Republic of Testland", c.DefinitieName);
        Assert.Equal("Testlandic", c.Adjective);
        Assert.Equal(64, c.CapitalStateId);
        Assert.Equal("monarchism", c.RulingIdeology);
        Assert.Equal("absolutism", c.InitialLeaderSubIdeology);
        Assert.False(c.ElectionsAllowed);
        Assert.Equal(60, c.ElectionFrequency);
        Assert.Equal(new DateOnly(1936, 1, 1), c.LastElection);
        Assert.Equal(100, c.PartyPopularity["monarchism"]);
        Assert.Equal("Royal Party", c.PartyNames["monarchism"].LongName);
        Assert.Equal("Royalism", c.IdeologyNames["monarchism"]);
        Assert.Equal(72.5, c.Stability);
        Assert.Equal(15, c.WarSupport);
        Assert.Equal(4, c.ResearchSlots);
        Assert.Equal(200, c.Convoys);
        var subject = Assert.Single(c.Subjects);
        Assert.Equal("SUB", subject.TargetTag);
        Assert.Equal("autonomy_puppet", subject.AutonomyType);
        Assert.Equal(30, subject.FreedomPercent);
        Assert.Equal("infantry_weapons", Assert.Single(c.StartingTechnologies));
        Assert.Equal("civilian_economy", Assert.Single(c.StartingLaws));
        Assert.Equal(500, c.Stockpile["infantry_equipment_1"]);
        var identity = Assert.Single(c.CosmeticIdentities);
        Assert.Equal("Test Reich", identity.DisplayName);
        Assert.Equal(new Rgb(200, 10, 10), identity.Color);
        Assert.Equal("1936.1.1 = { }", c.HistoryScript);
        Assert.Equal("western_european_gfx", c.GraphicalCulture);
        Assert.Equal("western_european_2d", c.GraphicalCulture2d);
        Assert.Equal(new Rgb(10, 20, 30), c.MapColor);
    }

    [Fact]
    public void Older_files_get_the_vanilla_ideologies_and_country_defaults()
    {
        var project = ProjectFile.Deserialize(
            "{ \"FormatVersion\": 1, \"Name\": \"x\", \"ModId\": \"x\", \"GameVersion\": \"1.19.*\"," +
            " \"Countries\": [ { \"Tag\": \"OLD\", \"Name\": \"Oldland\" } ] }");

        Assert.Equal(
            new[] { "democratic", "communism", "fascism", "neutrality" },
            project.Ideologies.Select(i => i.Id));

        var c = project.FindCountry("OLD")!;
        Assert.Equal(3, c.ResearchSlots);
        Assert.Equal(48, c.ElectionFrequency);
        Assert.True(c.ElectionsAllowed);
    }
}