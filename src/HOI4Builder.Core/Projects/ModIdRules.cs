using System.Globalization;
using System.Text;

namespace HOI4Builder.Core.Projects;

public static class ModIdRules
{
    public const int MaxLength = 64;
    public const string Fallback = "my_mod";

    // builds a valid mod id from a display name
    public static string FromName(string name)
    {
        var sb = new StringBuilder();

        foreach (char c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            
            char mapped = c switch
            {
                'ı' or 'İ' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ç' or 'Ç' => 'c',
                'ö' or 'Ö' => 'o',
                'ü' or 'Ü' => 'u',
                _ => c,
            };

            char lower = char.ToLowerInvariant(mapped);

            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(lower);
            else if (sb.Length > 0 && sb[^1] != '_')
                sb.Append('_');
        }

        string id = sb.ToString().Trim('_');

        if (id.Length == 0)
            return Fallback;
        
        if (char.IsDigit(id[0]))
            id = "mod_" + id;

        if (id.Length > MaxLength)
            id = id.Substring(0, MaxLength).TrimEnd('_');

        return id;
    }

    // returns an error message or null when the id is valid
    public static string? Validate(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return "The mod id cannot be empty.";
        
        if (id.Length > MaxLength)
            return $"The mod id can be at most {MaxLength} characters long.";
        
        if (id[0] is < 'a' or > 'z')
            return "The mod id must start with a lowercase letter.";
        
        foreach (char c in id)
        {
            bool ok = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_';
            if (!ok)
                return "The mod id can only contain lowercase letters, digits and underscores";
        }

        return null;
    }
}