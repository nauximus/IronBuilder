using System.Text;

namespace HOI4Builder.Core.Script;

public sealed class PdxParseException : Exception
{
    public PdxParseException(string message, int line, int column) : base($"{message} {line}, column {column}")
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }
    public int Column { get; }
}

internal enum TokenKind
{
    Atom,
    String,
    Operator,
    LBrace,
    RBrace,
    Comment,
    Eof,
}

internal readonly record struct Token(TokenKind Kind, string Text, PdxOperator Op, int Line, int Column);

internal sealed class PdxTokenizer
{
    private readonly string _src;
    private int _pos;
    private int _line = 1;
    private int _lineStart;

    public PdxTokenizer(string source)
    {
        _src = source.Length > 0 && source[0] == '\uFEFF' ? source.Substring(1) : source;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            var token = Next();
            tokens.Add(token);
            if (token.Kind == TokenKind.Eof)
                break;
        }
        return tokens;
    }

    private Token Next()
    {
        SkipWhitespace();
        int col = _pos - _lineStart + 1;
        if (_pos >= _src.Length)
            return new Token(TokenKind.Eof, "", default, _line, col);
        
        char c = _src[_pos];
        switch (c)
        {
            case '{':
                _pos++;
                return new Token(TokenKind.LBrace, "{", default, _line,col);
            case '}':
                _pos++;
                return new Token(TokenKind.RBrace, "}", default, _line, col);
            case '#':
                return ReadComment(col);
            case '"':
                return ReadString(col);
            case '=':
                _pos++;
                return Op(PdxOperator.Equal, "=", col);
            case '<':
                if (Peek(1) == '=') { _pos += 2; return Op(PdxOperator.LessOrEqual, "<=", col); }
                _pos++;
                return Op(PdxOperator.Less, "<", col);
            case '>':
                if (Peek(1) == '=') { _pos += 2; return Op(PdxOperator.GreaterOrEqual, ">=", col); }
                _pos++;
                return Op(PdxOperator.Greater, ">", col);
            case '!':
                if (Peek(1) == '=') { _pos += 2; return Op(PdxOperator.NotEqual, "!=", col); }
                break;
            case '?':
                if (Peek(1) == '=') { _pos += 2; return Op(PdxOperator.QuestionEqual, "?=", col); }
                break;
        }

        return ReadAtom(col);
    }

    private Token Op(PdxOperator op, string text, int col) => new(TokenKind.Operator, text, op, _line, col);

    private char Peek(int offset) => _pos + offset < _src.Length ? _src[_pos + offset] : '\0';

    private void SkipWhitespace()
    {
        while (_pos < _src.Length)
        {
            char c = _src[_pos];
            if (c == '\n')
            {
                _pos++;
                _line++;
                _lineStart = _pos;
            }
            else if (char.IsWhiteSpace(c))
            {
                _pos++;
            }
            else
            {
                break;
            }
        }
    }

    private Token ReadComment(int col)
    {
        int start = ++_pos; // skip '#'
        while (_pos < _src.Length && _src[_pos] != '\n') 
            _pos++;
        string text = _src.Substring(start, _pos - start).TrimEnd('\r');
        return new Token(TokenKind.Comment, text, default, _line, col);
    }

    private Token ReadString(int col)
    {
        int startLine = _line;
        _pos++; // skip opening quote
        var sb = new StringBuilder();
        while (true)
        {
            if (_pos >= _src.Length)
                throw new PdxParseException("Unterminated string", startLine, col);
            
            char ch = _src[_pos++];
            if (ch == '"')
                break;
            if (ch == '\n')
            {
                _line++;
                _lineStart = _pos;
            }
            sb.Append(ch);
        }
        return new Token(TokenKind.String, sb.ToString(), default, startLine, col);
    }

    private Token ReadAtom(int col)
    {
        int start = _pos;
        while (_pos < _src.Length && IsAtomChar(_pos))
            _pos++;

        if (_pos == start) // safety net: always consume something
            _pos++;
        
        return new Token(TokenKind.Atom, _src.Substring(start, _pos - start), default, _line, col);   
    }

    private bool IsAtomChar(int i)
    {
        char c = _src[i];
        if (char.IsWhiteSpace(c))
            return false;
        
        switch (c)
        {
            case   '{':
            case   '}':
            case   '=':
            case   '<':
            case   '>':
            case   '"':
            case   '#':
                return false;
            case   '!':
            case   '?':
                return !(i + 1 < _src.Length && _src[i + 1] == '=');
            default:
                return true;
        }
    }
}