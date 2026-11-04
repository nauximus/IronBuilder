using HOI4Builder.Core.Script;

namespace HOI4Builder.Core.Focuses;

public sealed class Focus
{
    public string Id { get; set; } = "";
    public string? Icon {get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    // When set, X and Y are relative to the this focus instead of the tree origin.
    public string? RelativePositionId { get; set; }

    public double? Cost { get; set; }

    // Each inner list is one "prerequisite = { .... }" block: any ONE focus in it satisfies the block (OR).
    // Several blocks must ALL be satisfied (AND).
    public List<List<string>> Prerequisites { get; } = new();

    public List<string> MutuallyExclusive { get; } = new();
    public List<string> SearchFilters { get; } = new();

    // Triggers and effects stay as raw script until the visual editors exist
    public PdxBlock? Available { get; set; }
    public PdxBlock? Bypass { get; set; }
    public PdxBlock? AllowBranch { get; set; }
    public PdxBlock? CompletionReward { get; set; }
    public PdxBlock? AiWillDo { get; set; }

    // Every key this model doesn'T know about, kept so import and export loses nothing.
    public List<PdxEntry> ExtraEntries { get; } = new();

    public override string ToString() => Id;

    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class FocusTree
{
    public string Id { get; set; } = "";

    // The "country = { factor = ... modifier = ... }" block, raw for now
    public PdxBlock? Country { get; set; }
    public string? CountryTag => CountryAssignment.ReadTag(Country);
    public bool IsDefault { get; set; }
    public string? SourceFile { get; set; }
    public List<Focus> Focuses { get; } = new();

    // Ids from "shared_focus = X" lines that point at focuses defined elswhere
    public List<string> SharedFocusIds { get; } = new();
    
    public List<PdxEntry> ExtraEntries { get; } = new();

    public Focus? FindFocus(string id) => Focuses.FirstOrDefault(f => f.Id == id);

    public override string ToString() => Id;

    public string MakeUniqueId(string prefix)
    {
        int n = 1;
        while (FindFocus($"{prefix}{n}") != null)
            n++;
        return $"{prefix}{n}";
    }
}