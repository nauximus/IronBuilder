using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;

namespace HOI4Builder.Core.Projects;

public sealed class ModProject
{
    public const string DefaultGameVersion = "1.19.*";

    public string Name { get; set; } = "My HOI4 Mod";
    public string ModId { get; set; } = "my_hoi4_mod";
    public string GameVersion { get; set; } = DefaultGameVersion;
    public string? ExportFolder { get; set; }
    public string? FilePath { get; set; }
    public string? ModVersion { get; set; }
    public string? ImportFromFolder { get; set; }
    public List<string> Tags { get; } = new();

    public List<FocusTree> FocusTrees { get; } = new();

    public List<Country> Countries { get; } = new();

    public List<IdeologyGroup> Ideologies { get; } = IdeologyGroup.CreateVanilla();

    public IdeologyGroup? FindIdeology(string id) => Ideologies.FirstOrDefault(i => i.Id == id);

    public IEnumerable<string> AllSubIdeologies() => Ideologies.SelectMany(i => i.SubIdeologies);
    
    public Country? FindCountry(string tag) => Countries.FirstOrDefault(c => c.Tag == tag);

    public IEnumerable<FocusTree> TreesFor(string tag) => FocusTrees.Where(t => t.CountryTag == tag);

    public List<FocusTree> UnassignedTrees() => FocusTrees.Where(t => t.CountryTag is null).ToList();

    public HashSet<string> ProtectedFiles { get; } = new();

    public UndoStack History { get; } = new();

    public static ModProject CreateNew(string name, string modId, string gameVersion, string? exportFolder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The mod name cannot be empty.", nameof(name));
        
        var idError = ModIdRules.Validate(modId);
        if (idError != null)
            throw new ArgumentException(idError, nameof(modId));
        
        if (string.IsNullOrWhiteSpace(gameVersion))
            throw new ArgumentException("The game version cannot be empty.", nameof(gameVersion));

        return new ModProject
        {
            Name = name.Trim(),
            ModId = modId,
            GameVersion = gameVersion.Trim(),
            ExportFolder = string.IsNullOrWhiteSpace(exportFolder) ? null : exportFolder.Trim(),
        };

    }

    public string MakeUniqueTreeId(string prefix)
    {
        int n = 1;
        while (FocusTrees.Any(t => t.Id == $"{prefix}{n}"))
            n++;
        return $"{prefix}{n}";
    }

    public void EnsureCountriesForTrees()
    {
        foreach (var tag in FocusTrees.Select(t => t.CountryTag).OfType<string>().Distinct().ToList())
        {
            if (FindCountry(tag) is null)
                Countries.Add(new Country(tag, tag));
        }
    }
}