using System.Diagnostics.Tracing;
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
    public List<string>? Tags { get; set; }
    public List<CountryDto>? Countries { get; set; }
    public List<TreeDto>? FocusTrees { get; set; }
}

internal sealed class TreeDto
{
    public string? SourceFile { get; set; }
    public string Script { get; set; } = "";
}

internal sealed class CountryDto
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
}

public static class ProjectFile
{
    public const int CurrentFormat = 1;
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
            Tags = new List<string>(project.Tags),
            Countries = project.Countries.Select(c => new CountryDto { Tag = c.Tag, Name = c.Name}).ToList(),
            FocusTrees = new List<TreeDto>(),
        };

        foreach (var tree in project.FocusTrees)
        {
            dto.FocusTrees.Add(new TreeDto
            {
                SourceFile = tree.SourceFile,
                Script = PdxWriter.Write(FocusTreeMapper.ToDocument(new[] { tree }))
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
        };

        project.Tags.AddRange(dto.Tags ?? new List<string>());

        foreach (var treeDto in dto.FocusTrees ?? new List<TreeDto>())
        {
            try
            {
                foreach (var tree in FocusTreeMapper.ReadTrees(PdxParser.Parse(treeDto.Script)))
                {
                    tree.SourceFile = treeDto.SourceFile;
                    project.FocusTrees.Add(tree);
                }
            }
            catch (PdxParseException ex)
            {
                throw new ProjectFileException($"A focus tree in project file is damaged: {ex.Message}", ex);
            }
        }
        foreach (var country in dto.Countries ?? new List<CountryDto>())
            project.Countries.Add(new Country(country.Tag, country.Name));

        project.EnsureCountriesForTrees();

        return project;
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