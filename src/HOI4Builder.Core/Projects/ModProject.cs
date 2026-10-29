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

    public List<FocusTree> FocusTrees { get; } = new();

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
}