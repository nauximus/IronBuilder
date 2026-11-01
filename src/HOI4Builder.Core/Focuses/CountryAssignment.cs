using HOI4Builder.Core.Projects;
using HOI4Builder.Core.Script;

namespace HOI4Builder.Core.Focuses;

public static class CountryAssignment
{
    // the tag a tree is assigned to: the first modifier with a tag and a positive add.
    public static string? ReadTag(PdxBlock? country)
    {
        if (country is null)
            return null;

        foreach (var entry in country.FindAll("modifier"))
        {
            if (entry.Value is not PdxBlock modifier)
                continue;
            
            var tag = modifier.GetString("tag");
            if (tag is null)
                continue;
            
            if (modifier.Find("add")?.Value is PdxScalar add && add.TryGetDouble(out var weight) && weight > 0)
                return tag;
            
        }

        return null;
    }

    // the standard block: factor = 0 and a modifier that adds weight for one tag.
    public static PdxBlock Build(string tag)
    {
        var country = new PdxBlock();
        country.Add("factor", "0");

        var modifier = country.AddBlock("modifier");
        modifier.Add("add", "10");
        modifier.Add("tag", tag);

        return country;
    }
}