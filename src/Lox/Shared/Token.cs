namespace Lox;

internal class Token
{
    /// <summary>
    /// The type of this token.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// The snippet of source text from which the token was derived.
    /// </summary>
    public string Lexeme { get; }

    /// <summary>
    /// The runtime value represented by the lexeme; only non-null for numbers and strings.
    /// </summary>
    public object? Literal { get; }

    /// <summary>
    /// The line in the source text from which the token was derived.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Creates a Token.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="lexeme">The lexeme.</param>
    /// <param name="literal">The runtime value.</param>
    /// <param name="line">The line.</param>
    public Token(TokenType type, string lexeme, object? literal, int line)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }
}
