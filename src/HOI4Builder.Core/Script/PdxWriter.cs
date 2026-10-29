using System.Text;

namespace HOI4Builder.Core.Script;

public sealed class PdxWriterOptions
{
    public string Indent { get; set; } = "\t";
    public string NewLine { get; set; } = "\n";
    public bool InlineSimpleLists { get; set; } = true;
    public int InlineLimit { get; set; } = 8;
}

public static class PdxWriter
{
    public static string Write(PdxBlock root, PdxWriterOptions? options = null)
    {
        options ??= new PdxWriterOptions();
        var sb = new StringBuilder();
        WriteEntries(sb, root, 0, options);
        return sb.ToString();
    }

    public static void WriteFile(string path, PdxBlock root, PdxWriterOptions? options = null, bool utf8Bom = false)
    {
        File.WriteAllText(path, Write(root, options), new UTF8Encoding(utf8Bom));
    }

    private static void WriteEntries(StringBuilder sb, PdxBlock block, int depth, PdxWriterOptions o)
    {
        foreach (var entry in block.Entries)
        {
            foreach (var comment in entry.LeadingComments)
            {
                Indent(sb, depth, o);
                sb.Append('#').Append(comment).Append(o.NewLine);
            }

            Indent(sb, depth, o);

            if (entry.Key != null)
            {
                sb.Append(FormatText(entry.Key, entry.KeyQuoted))
                    .Append(' ')
                    .Append(entry.Operator.ToText())
                    .Append(' ');
            }

            switch (entry.Value)
            {
                case PdxScalar scalar:
                    sb.Append(FormatText(scalar.Text, scalar.Quoted));
                    break;
                case PdxBlock child:
                    WriteBlock(sb, child, depth, o);
                    break;
            }

            if (entry.TrailingComment != null)
                sb.Append(" #").Append(entry.TrailingComment);

            sb.Append(o.NewLine);
        }
        
        foreach (var comment in block.DanglingComments)
        {
            Indent(sb, depth, o);
            sb.Append('#').Append(comment).Append(o.NewLine);
        }
        
    }

    private static void WriteBlock(StringBuilder sb, PdxBlock block, int depth, PdxWriterOptions o)
    {
        if (block.Tag != null)
            sb.Append(block.Tag).Append(' ');
        
        if (CanInline(block, o))
        {
            sb.Append('{');
            foreach (var entry in block.Entries)
            {
                var scalar = (PdxScalar)entry.Value;
                sb.Append(' ').Append(FormatText(scalar.Text, scalar.Quoted));
            }
            sb.Append(" }");
            return;
        }

        sb.Append('{').Append(o.NewLine);
        WriteEntries(sb, block, depth +1, o);
        Indent(sb, depth, o);
        sb.Append('}');
    }

    private static bool CanInline(PdxBlock block, PdxWriterOptions o) =>
        o.InlineSimpleLists
        && block.Entries.Count <= o.InlineLimit
        && block.DanglingComments.Count == 0
        && block.Entries.All(e =>
            e.Key is null
            && e.Value is PdxScalar
            && e.LeadingComments.Count == 0
            && e.TrailingComment is null);

    private static void Indent(StringBuilder sb, int depth, PdxWriterOptions o)
    {
        for (int i = 0; i < depth; i++)
            sb.Append(o.Indent);
    }

    private static string FormatText(string text, bool quoted) => quoted || NeedsQuotes(text) ? "\"" + text + "\"" : text;

    private static bool NeedsQuotes(string text)
    {
        if (text.Length == 0)
            return true;
        
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c) || c is '{' or '}' or '=' or '<' or '>' or '"' or '#')
                return true;
        }
        return false;
    }
}