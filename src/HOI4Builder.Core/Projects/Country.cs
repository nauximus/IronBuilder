using System.Dynamic;
using System.Runtime.InteropServices;

namespace HOI4Builder.Core.Projects;

public sealed class Country
{
    public Country(string tag, string name)
    {
        Tag = tag;
        Name = name;
    }

    // identity
    public string Tag { get; set; }
    public string Name { get; set; }
    public string DefinitieName { get; set; } = "";
    public string Adjective { get; set; } = "";
    public int? CapitalStateId { get; set; }

    // government
    public string RulingIdeology { get; set; } = "";
    public string InitialLeaderSubIdeology { get; set; } = "";
    public bool ElectionsAllowed { get; set; } = true;
    public int ElectionFrequency { get; set; } = 48;
    public DateOnly? LastElection { get; set; }

    // keyed by ideology id
    public Dictionary<string, int> PartyPopularity { get; } = new();
    public Dictionary<string, PartyName> PartyNames { get; } = new();
    public Dictionary<string, string> IdeologyNames { get; } = new();

    // starting values (percent, 0 to 100)
    public double Stability { get; set; } = 100;
    public double WarSupport { get; set; } = 50;
    public int ResearchSlots { get; set; } = 3;
    public int Convoys { get; set; }

    public List<SubjectRelation> Subjects { get; } = new();

    // plain ids for now
    public List<string> StartingTechnologies { get; } = new();
    public List<string> StartingLaws { get; } = new();
    public Dictionary<string, int> Stockpile { get; set; } = new();

    public List<CosmeticIdentity> CosmeticIdentities { get; } = new();

    public string HistoryScript { get; set; } = "";

    // country appearance
    public string GraphicalCulture { get; set; } = "";
    public string GraphicalCulture2d { get; set; } = "";
    public Rgb MapColor { get; set; } = new(128, 128, 128);

    public override string ToString() => Name == Tag ? Tag : $"{Tag} - {Name}";
}