using System.Diagnostics.CodeAnalysis;
using System.Reflection;


namespace HOI4Builder.Core.Script;

public static class PdxParser
{
    public static PdxBlock Parse(string source)
    {
        var tokens = new PdxTokenizer(source).Tokenize();
        return new ParserState(tokens).ParseRoot();
    }

    public static PdxBlock ParseFile(string path) => Parse(File.ReadAllText(path));

    public static bool TryParse(string source, [NotNullWhen(true)] out PdxBlock? result, [NotNullWhen(false)] out PdxParseException? error)
    {
        try
        {
            result = Parse(source);
            error = null;
            return true;
        }
        catch (PdxParseException ex)
        {
            result = null;
            error = ex;
            return false;
        }
    }

    private sealed class ParserState
    {
        private readonly List<Token> _tokens;
        private int _index;
        private int _lastLine = 1;

        public ParserState(List<Token> tokens) => _tokens = tokens;

        private Token Current => _tokens[_index];

        private Token Advance()
        {
            var token = _tokens[_index];
            if (token.Kind != TokenKind.Eof) _index++;
            _lastLine = token.Line;
            return token;
        }

        private Token PeekSignificant()
        {
            int i = _index;
            while (_tokens[i].Kind == TokenKind.Comment)
                i++;
            return _tokens[i];
        }

        private void SkipComments(List<string> into)
        {
            while (Current.Kind == TokenKind.Comment)
                into.Add(Advance().Text);
        }

        public PdxBlock ParseRoot()
        {
            var root = new PdxBlock();
            ParseEntries(root, isRoot: true);
            if (Current.Kind == TokenKind.RBrace)
                throw Error("Unexpected '}'", Current);
            return root;
        }

        private void ParseEntries(PdxBlock block, bool isRoot)
        {
            var pending = new List<string>();
            PdxEntry? previous = null;
            int previousEndLine = -1;

            while (true)
            {
                var token = Current;
                
                if (token.Kind == TokenKind.Eof)
                {
                    if (!isRoot)
                        throw Error("Unexpected end of file: missing '}'", token);
                    break;
                }

                if (token.Kind == TokenKind.RBrace)
                    break;

                if (token.Kind == TokenKind.Comment)
                {
                    Advance();
                    if (previous != null && previous.TrailingComment == null && token.Line == previousEndLine)
                        previous.TrailingComment = token.Text;
                    else
                        pending.Add(token.Text);
                    continue;
                }

                var entry = ParseEntry(pending);
                pending = new List<string>();
                block.Entries.Add(entry);
                previous = entry;
                previousEndLine = _lastLine;
            }

            block.DanglingComments.AddRange(pending);
        }

        private PdxEntry ParseEntry(List<string> leading)
        {
            var first = Current;
            PdxEntry entry;

            if (first.Kind == TokenKind.LBrace)
            {
                entry = new PdxEntry(null, ParseBlock(null));
            }
            else if (first.Kind is TokenKind.Atom or TokenKind.String)
            {
                Advance();

                if (PeekSignificant().Kind == TokenKind.Operator)
                {
                    var strays = new List<string>();
                    SkipComments(strays);
                    var op = Advance();
                    var value = ParseValue(strays);
                    leading.AddRange(strays);
                    entry = new PdxEntry(first.Text, value)
                    {
                        KeyQuoted = first.Kind == TokenKind.String,
                        Operator = op.Op,
                    };
                }
                else if (Current.Kind == TokenKind.LBrace && first.Kind == TokenKind.Atom)
                {
                    entry = new PdxEntry(null, ParseBlock(first.Text));
                }
                else
                {
                    entry = new PdxEntry(null, new PdxScalar(first.Text, first.Kind == TokenKind.String));
                }
            }
            else
            {
                throw Error($"Unexpected {Describe(first)}", first);
            }

            entry.LeadingComments.AddRange(leading);
            return entry;
        }

        private PdxValue ParseValue(List<string> strays)
        {
            SkipComments(strays);
            var token = Current;

            if (token.Kind == TokenKind.LBrace)
                return ParseBlock(null);
            
            if (token.Kind is TokenKind.Atom or TokenKind.String)
            {
                Advance();
                if (token.Kind == TokenKind.Atom && Current.Kind == TokenKind.LBrace)
                    return ParseBlock(token.Text);
                return new PdxScalar(token.Text, token.Kind == TokenKind.String);
            }

            throw Error($"Expected a value but found {Describe(token)}", token);
        }

        private PdxBlock ParseBlock(string? tag)
        {
            Advance(); // '{'
            var block = new PdxBlock { Tag = tag };
            ParseEntries(block, isRoot: false);
            
            if (Current.Kind != TokenKind.RBrace)
                throw Error("Expected '}", Current);
            Advance();
            return block;
        }

        private static string Describe(Token t) => t.Kind switch
        {
            TokenKind.Eof => "end of file",
            TokenKind.Operator => $"operator '{t.Text}'",
            TokenKind.LBrace => "'{'",
            TokenKind.RBrace => "'}'",
            TokenKind.Comment => "comment",
            TokenKind.String => $"string \"{t.Text}\"",
            _ => $"'{t.Text}'",
        };

        private static PdxParseException Error(string message, Token t) => new(message, t.Line, t.Column);
    }
}