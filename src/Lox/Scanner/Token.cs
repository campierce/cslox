namespace cslox.lox.scanner;

internal class Token
{
    internal TokenType type { get; }
    internal string lexeme { get; }
    internal object? literal { get; }
    internal int line { get; }

    internal Token(TokenType type, string lexeme, object? literal, int line)
    {
        this.type = type;
        this.lexeme = lexeme;
        this.literal = literal;
        this.line = line;
    }

    public override string ToString()
    {
        return $"{type} {lexeme} {literal}";
    }
}
