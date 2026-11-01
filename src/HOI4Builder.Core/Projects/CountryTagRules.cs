namespace HOI4Builder.Core.Projects;

public static class CountryTagRules
{
    public static string? Validate(string? tag)
    {
        if (string.IsNullOrEmpty(tag) || tag.Length != 3)
            return "A country tag must be exactly 3 characters.";

        foreach (char c in tag)
        {
            bool ok = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
            if (!ok)
                return "A country tag can only contain capital letters and digits.";
        }

        return null;
    }
    
}