namespace Lox;

abstract internal class Error : Exception
{
    /// <summary>
    /// The line number where the error occurred.
    /// </summary>
    private readonly int _line;

    /// <summary>
    /// The token where the error occurred, if applicable.
    /// </summary>
    private readonly Token? _token;

    /// <summary>
    /// The type of error.
    /// </summary>
    abstract public string Type { get; }

    /// <summary>
    /// The error's line as a formatted string.
    /// </summary>
    public string Line => $"[line {_line}] ";

    /// <summary>
    /// The error's token as a formatted string.
    /// </summary>
    public string Where
    {
        get
        {
            if (_token is not null)
            {
                if (_token.Type == TokenType.EOF)
                {
                    return " at end";
                }
                return $" at '{_token.Lexeme}'";
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Creates a new Error.
    /// </summary>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public Error(int line, string message) : base(message)
    {
        _line = line;
    }

    /// <summary>
    /// Creates a new Error.
    /// </summary>
    /// <param name="token">The token where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public Error(Token token, string message) : base(message)
    {
        _line = token.Line;
        _token = token;
    }
}
