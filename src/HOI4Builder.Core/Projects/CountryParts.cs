namespace HOI4Builder.Core.Projects;

public readonly record struct Rgb(byte R, byte G, byte B);

public sealed class PartyName
{
    public string Name { get; set; } = "";
    public string LongName { get; set; } = "";
}

public sealed class SubjectRelation
{
    public string TargetTag { get; set; } = "";
    public string AutonomyType { get; set; } = "";
    public double FreedomPercent { get; set; } = 50;
}

public sealed class CosmeticIdentity
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set;} = "";
    public string? FlagFile { get; set; }
    public Rgb? Color { get; set; }
}