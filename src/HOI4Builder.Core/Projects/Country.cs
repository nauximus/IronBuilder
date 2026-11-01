namespace HOI4Builder.Core.Projects;

public sealed class Country
{
    public Country(string tag, string name)
    {
        Tag = tag;
        Name = name;
    }

    public string Tag { get; set; }
    public string Name { get; set; }

    public override string ToString() => Name == Tag ? Tag : $"{Tag} - {Name}";
}