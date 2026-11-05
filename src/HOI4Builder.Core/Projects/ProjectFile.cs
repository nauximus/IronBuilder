using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;

namespace HOI4Builder.Core.Projects;

public sealed class ProjectFileException : Exception
{
    public ProjectFileException(string message) : base(message)
    {
        
    }

    public ProjectFileException(string message, Exception inner) : base(message, inner)
    {
        
    }
}

internal sealed class ProjectDto
{
    public int FormatVersion { get; set; }
    public string Name { get; set; } = "";
    public string ModId { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public string? ModVersion { get; set; }
    public string? ExportFolder { get; set; }
    public string? ImportFromFolder { get; set; }
    public string? GameInstallFolder { get; set; }
    public bool isSubmod { get; set; }
    public WorldMode WorldMode { get; set; } = WorldMode.StandardMod;
    public List<string>? Tags { get; set; }
    public List<CountryDto>? Countries { get; set; }
    public List<IdeologyDto>? Ideologies { get; set; }
    public List<TreeDto>? FocusTrees { get; set; }
    public List<string>? ProtectedFiles { get; set; }
}

internal sealed class TreeDto
{
    public string? SourceFile { get; set; }
    public string Script { get; set; } = "";

    public List<FocusTextDto>? Texts { get; set; }
}

internal sealed class CountryDto
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DefinitieName { get; set;}
    public string? Adjective { get; set; }
    public int? CapitalStateId { get; set;}

    public string? RulingIdeology { get; set; }
    public string? InitialLeaderSubIdeology { get; set; }
    public bool? ElectionsAllowed { get; set; }
    public int? ElectionFrequency { get; set; }
    public DateOnly? LastElection { get; set; }

    public Dictionary<string, int>? PartyPopularity { get; set; }
    public Dictionary<string, PartyName>? PartyNames { get; set; }
    public Dictionary<string, string>? IdeologyNames { get; set; }

    public double? Stability { get; set; }
    public double? WarSupport { get; set; }
    public int? ResearchSlots { get; set; }
    public int? Convoys { get; set; }

    public List<SubjectRelation>? Subjects { get; set; }
    public List<string>? StartingTechnologies { get; set; }
    public List<string>? StartingLaws { get; set; }
    public Dictionary<string, int>? Stockpile { get; set; }
    public List<CosmeticIdentity>? CosmeticIdentities { get; set; }

    public string? HistoryScript { get; set; }
    public string? GraphicalCulture { get; set; }
    public string? GraphicalCulture2d { get; set; }
    public Rgb? MapColor { get; set; }
}

internal sealed class IdeologyDto
{
    public string Id { get; set; } = "";
    public List<string>? SubIdeologies { get; set; }
}

internal sealed class FocusTextDto
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public static class ProjectFile
{
    public const int CurrentFormat = 2;
    public const string Extension = ".modpr";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Serialize(ModProject project)
    {
        var dto = new ProjectDto
        {
            FormatVersion = CurrentFormat,
            Name = project.Name,
            ModId = project.ModId,
            GameVersion = project.GameVersion,
            ModVersion = project.ModVersion,
            ExportFolder = project.ExportFolder,
            ImportFromFolder = project.ImportFromFolder,
            GameInstallFolder = project.GameInstallFolder,
            isSubmod = project.isSubmod,
            WorldMode = project.WorldMode,
            Tags = new List<string>(project.Tags),
            Ideologies = project.Ideologies.Select(i => new IdeologyDto { Id = i.Id, SubIdeologies = new List<string>(i.SubIdeologies) }).ToList(),
            Countries = project.Countries.Select(ToDto).ToList(),
            FocusTrees = new List<TreeDto>(),
            ProtectedFiles = project.ProtectedFiles.OrderBy(f => f, StringComparer.Ordinal).ToList(),
        };

        foreach (var tree in project.FocusTrees)
        {
            dto.FocusTrees.Add(new TreeDto
            {
                SourceFile = tree.SourceFile,
                Script = PdxWriter.Write(FocusTreeMapper.ToDocument(new[] { tree })),
                Texts = tree.Focuses
                    .Where(f => f.Name != null || f.Description != null)
                    .Select(f => new FocusTextDto { Id = f.Id, Name = f.Name, Description = f.Description })
                    .ToList(),
            });
        }

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static ModProject Deserialize(string json)
    {
        ProjectDto? dto;

        try
        {
            dto = JsonSerializer.Deserialize<ProjectDto>(json);
        }
        catch (JsonException ex)
        {
            throw new ProjectFileException("The file is not a valid project file: " + ex.Message, ex);
        }

        if (dto is null || dto.FormatVersion < 1)
            throw new ProjectFileException("The file is not a IronBuilder project.");
        
        if (dto.FormatVersion > CurrentFormat)
        {
            throw new ProjectFileException(
                    $"This project was saved by a newer version of the tool (file format {dto.FormatVersion}).");
        }

        var project = new ModProject
        {
            Name = dto.Name,
            ModId = dto.ModId,
            GameVersion = dto.GameVersion,
            ModVersion = dto.ModVersion,
            ExportFolder = dto.ExportFolder,
            ImportFromFolder = dto.ImportFromFolder,
            GameInstallFolder = dto.GameInstallFolder,
            isSubmod = dto.isSubmod,
            WorldMode = dto.WorldMode,
        };

        project.Tags.AddRange(dto.Tags ?? new List<string>());

        if (dto.Ideologies is not null)
        {
            project.Ideologies.Clear();
            foreach (var IdeologDto in dto.Ideologies)
            {
                var group = new IdeologyGroup(IdeologDto.Id);
                group.SubIdeologies.AddRange(IdeologDto.SubIdeologies ?? new List<string>());
                project.Ideologies.Add(group);
            }
        }

        foreach (var treeDto in dto.FocusTrees ?? new List<TreeDto>())
        {
            try
            {
                foreach (var tree in FocusTreeMapper.ReadTrees(PdxParser.Parse(treeDto.Script)))
                {
                    tree.SourceFile = treeDto.SourceFile;
                    project.FocusTrees.Add(tree);

                    foreach (var text in treeDto.Texts ?? new List<FocusTextDto>())
                    {
                        var focus = tree.FindFocus(text.Id);
                        if (focus is null)
                            continue;

                        focus.Name = text.Name;
                        focus.Description = text.Description;
                    }
                }
            }
            catch (PdxParseException ex)
            {
                throw new ProjectFileException($"A focus tree in project file is damaged: {ex.Message}", ex);
            }
        }
        foreach (var countryDto in dto.Countries ?? new List<CountryDto>())
            project.Countries.Add(FromDto(countryDto));
        
        foreach (var file in dto.ProtectedFiles ?? new List<string>())
            project.ProtectedFiles.Add(file);
        project.EnsureCountriesForTrees();

        return project;
    }

    private static CountryDto ToDto(Country c) => new()
    {
        Tag = c.Tag,
        Name = c.Name,
        DefinitieName = c.DefinitieName,
        Adjective = c.Adjective,
        CapitalStateId = c.CapitalStateId,
        RulingIdeology = c.RulingIdeology,
        InitialLeaderSubIdeology = c.InitialLeaderSubIdeology,
        ElectionsAllowed = c.ElectionsAllowed,
        ElectionFrequency = c.ElectionFrequency,
        LastElection = c.LastElection,
        PartyPopularity = new Dictionary<string, int>(c.PartyPopularity),
        PartyNames = new Dictionary<string, PartyName>(c.PartyNames),
        IdeologyNames = new Dictionary<string, string>(c.IdeologyNames),
        Stability = c.Stability,
        WarSupport = c.WarSupport,
        ResearchSlots = c.ResearchSlots,
        Convoys = c.Convoys,
        Subjects = new List<SubjectRelation>(c.Subjects),
        StartingTechnologies = new List<string>(c.StartingTechnologies),
        StartingLaws = new List<string>(c.StartingLaws),
        Stockpile = new Dictionary<string, int>(c.Stockpile),
        CosmeticIdentities = new List<CosmeticIdentity>(c.CosmeticIdentities),
        HistoryScript = c.HistoryScript,
        GraphicalCulture = c.GraphicalCulture,
        GraphicalCulture2d = c.GraphicalCulture2d,
        MapColor = c.MapColor,    
    };

    private static Country FromDto(CountryDto dto)
    {
        var c = new Country(dto.Tag, dto.Name);

        c.DefinitieName = dto.DefinitieName ?? c.DefinitieName;
        c.Adjective = dto.Adjective ?? c.Adjective;
        c.CapitalStateId = dto.CapitalStateId;

        c.RulingIdeology = dto.RulingIdeology ?? c.RulingIdeology;
        c.InitialLeaderSubIdeology = dto.InitialLeaderSubIdeology ?? c.InitialLeaderSubIdeology;
        c.ElectionsAllowed = dto.ElectionsAllowed ?? c.ElectionsAllowed;
        c.ElectionFrequency = dto.ElectionFrequency ?? c.ElectionFrequency;
        c.LastElection = dto.LastElection;

        if (dto.PartyPopularity is { } popularity)
            foreach (var pair in popularity) c.PartyPopularity[pair.Key] = pair.Value;
        if (dto.PartyNames is { } partyNames)
            foreach (var pair in partyNames) c.PartyNames[pair.Key] = pair.Value;
        if (dto.IdeologyNames is { } ideologyNames)
            foreach (var pair in ideologyNames) c.IdeologyNames[pair.Key] = pair.Value;

        c.Stability = dto.Stability ?? c.Stability;
        c.WarSupport = dto.WarSupport ?? c.WarSupport;
        c.ResearchSlots = dto.ResearchSlots ?? c.ResearchSlots;
        c.Convoys = dto.Convoys ?? c.Convoys;

        if (dto.Subjects is { } subjects) c.Subjects.AddRange(subjects);
        if (dto.StartingTechnologies is { } techs) c.StartingTechnologies.AddRange(techs);
        if (dto.StartingLaws is { } laws) c.StartingLaws.AddRange(laws);
        if (dto.Stockpile is { } stockpile)
            foreach (var pair in stockpile) c.Stockpile[pair.Key] = pair.Value;
        if (dto.CosmeticIdentities is { } identities) c.CosmeticIdentities.AddRange(identities);

        c.HistoryScript = dto.HistoryScript ?? c.HistoryScript;
        c.GraphicalCulture = dto.GraphicalCulture ?? c.GraphicalCulture;
        c.GraphicalCulture2d = dto.GraphicalCulture2d ?? c.GraphicalCulture2d;
        c.MapColor = dto.MapColor ?? c.MapColor;

        return c;
        
    }

    public static void Save(ModProject project, string path)
    {
        var json = Serialize(project);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // write the new version next to the target first, so a failed write never damages the old file.
        var temp = path + ".tmp";
        File.WriteAllText(temp, json, new UTF8Encoding(false));

        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", overwrite: true);
            File.Move(temp, path, overwrite: true);
        }
        else
        {
            File.Move(temp, path);
        }

        project.FilePath = path;
    }

    public static ModProject Load(string path)
    {
        var project = Deserialize(File.ReadAllText(path));
        project.FilePath = path;
        return project;
    }
}