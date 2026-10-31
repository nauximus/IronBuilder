using HOI4Builder.Core.Script;

namespace HOI4Builder.Core.Projects;

// Mod descriptor fields | descriptor.mod file that tool cares about
public sealed class ModDescriptor
{
    public string? Name { get; private set; }
    public string? Version { get; private set; }
    public string? SupportedVersion { get; private set; }
    public List<string> Tags { get; } = new();

    public static ModDescriptor Parse(string text)
    {
        var root = PdxParser.Parse(text);

        var descriptor = new ModDescriptor
        {
            Name = root.GetString("name"),
            Version = root.GetString("version"),
            SupportedVersion = root.GetString("supported_version"),
        };

        var tags = root.GetBlock("tags");
        if (tags != null)
            descriptor.Tags.AddRange(tags.BareScalars());
        
        return descriptor;
    }
}