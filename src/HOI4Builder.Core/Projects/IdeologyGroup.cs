namespace HOI4Builder.Core.Projects;

public sealed class IdeologyGroup
{
    public IdeologyGroup(string id)
    {
        Id = id;
    }

    public string Id { get; set; }
    public List<string> SubIdeologies { get; } = new();

    public override string ToString() => Id;

    public static List<IdeologyGroup> CreateVanilla() => new()
    {
        new("democratic"),
        new("communism"),
        new("fascism"),
        new("neutrality"),
    };
}