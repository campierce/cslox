namespace cslox.lox.scanner;

internal class Token
{
    internal TokenType Type { get; }
    internal string Lexeme { get; }
    internal object? Literal { get; }
    internal int Line { get; }

    internal Token(TokenType type, string lexeme, object? literal, int line)
    {
        this.Type = type;
        this.Lexeme = lexeme;
        this.Literal = literal;
        this.Line = line;
    }

    public override string ToString()
    {
        return $"{Type} {Lexeme} {Literal}";
    }
}
