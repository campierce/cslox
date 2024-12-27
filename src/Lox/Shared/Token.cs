namespace Lox;

internal class Token
{
    /// <summary>
    /// The token's type.
    /// </summary>
    public TokenType Type { get; }

    /// <summary>
    /// The snippet of source text from which the token was derived.
    /// </summary>
    public string Lexeme { get; }

    /// <summary>
    /// The literal value (string or number) represented by the lexeme, if applicable.
    /// </summary>
    public object? Literal { get; }

    /// <summary>
    /// The line in the source text from which the token was derived.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Creates a Token.
    /// </summary>
    /// <param name="type">The token's type.</param>
    /// <param name="lexeme">The token's lexeme.</param>
    /// <param name="literal">The token's literal.</param>
    /// <param name="line">The token's line.</param>
    public Token(TokenType type, string lexeme, object? literal, int line)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }
}
