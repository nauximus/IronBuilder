using System.Globalization;

namespace HOI4Builder.Core.Script;

public enum PdxOperator
{
    Equal,
    NotEqual,
    Less,
    Greater,
    LessOrEqual,
    GreaterOrEqual,
    QuestionEqual,
}

public static class PdxOperatorExtensions
{
    public static string ToText(this PdxOperator op) => op switch
    {
        PdxOperator.Equal => "=",
        PdxOperator.NotEqual => "!=",
        PdxOperator.Less => "<",
        PdxOperator.Greater => ">",
        PdxOperator.LessOrEqual => "<=",
        PdxOperator.GreaterOrEqual => ">=",
        PdxOperator.QuestionEqual => "?=",
        _ => "=",
    };
}

public abstract class PdxValue
{
}

public sealed class PdxScalar : PdxValue
{
    public PdxScalar(string text, bool quoted = false)
    {
        Text = text;
        Quoted = quoted;
    }

    public string Text { get; set; }
    public bool Quoted { get; set; }

    public bool IsYes => !Quoted && Text == "yes";
    public bool IsNo => !Quoted && Text == "no";

    public bool TryGetInt(out int value) =>
        int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    
    public bool TryGetDouble(out double value) =>
        double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    
    public override string ToString() => Text;
}

public sealed class PdxBlock : PdxValue
{
    public string? Tag { get; set; }
    public List<PdxEntry> Entries { get; } = new();
    public List<string> DanglingComments { get; } = new();

    public PdxEntry? Find(string key) => Entries.FirstOrDefault(e => e.Key == key);

    public IEnumerable<PdxEntry> FindAll(string key) => Entries.Where(e => e.Key == key);

    public PdxBlock? GetBlock(string key) => Find(key)?.Value as PdxBlock;

    public string? GetString(string key) => (Find(key)?.Value as PdxScalar)?.Text;

    public int? GetInt(string key)
    {
        if (Find(key)?.Value is PdxScalar s && s.TryGetInt(out var v))
            return v;
        return null;
    }

    public IEnumerable<string> BareScalars() =>
        Entries.Where(e => e.IsBare && e.Value is PdxScalar).Select(e => ((PdxScalar)e.Value).Text);
    
    public PdxBlock Add(string key, string value, PdxOperator op = PdxOperator.Equal, bool quoted = false)
    {
        Entries.Add(new PdxEntry(key, new PdxScalar(value, quoted)) { Operator = op });
        return this;
    }

    public PdxBlock AddBare(string value, bool quoted = false)
    {
        Entries.Add(new PdxEntry(null, new PdxScalar(value, quoted)));
        return this;
    }

    public PdxBlock AddBlock(string key)
    {
        var child = new PdxBlock();
        Entries.Add(new PdxEntry(key, child));
        return child;
    }

    public int RemoveAll(string key) => Entries.RemoveAll(e => e.Key == key);
}

public sealed class PdxEntry
{
    public PdxEntry(string? key, PdxValue value)
    {
        Key = key;
        Value = value;
    }
    public string? Key { get; set; }
    public bool KeyQuoted { get; set; }
    public PdxOperator Operator { get; set; } = PdxOperator.Equal;
    public PdxValue Value { get; set; }

    public List<string> LeadingComments { get; } = new();
    public string? TrailingComment { get; set; }

    public bool IsBare => Key is null;
}