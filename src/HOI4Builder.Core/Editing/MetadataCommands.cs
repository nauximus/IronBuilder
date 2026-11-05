using HOI4Builder.Core.Projects;

namespace HOI4Builder.Core.Editing;

public sealed class ResetAdvancedMetadataCommand : IEditCommand
{
    private readonly ModProject _project;
    private readonly string? _oldVersion;
    private readonly List<string> _oldTags;

    public ResetAdvancedMetadataCommand(ModProject project)
    {
        _project = project;
        _oldVersion = project.ModVersion;
        _oldTags = new List<string>(project.Tags);
    }

    public string Description => "Reset advanced metadata";

    public void Do()
    {
        _project.ModVersion = null;
        _project.Tags.Clear();
    }

    public void Undo()
    {
        _project.ModVersion = _oldVersion;
        _project.Tags.Clear();
        _project.Tags.AddRange(_oldTags);
    }
}