namespace Lox;

abstract internal class Error : Exception
{
    private readonly int? _line;

    private readonly Token? _token;

    abstract public string Name { get; }

    public string Line
    {
        get
        {
            if (_line is not null)
            {
                return $"[line {_line}] ";
            }
            return string.Empty;
        }
    }

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

    public Error(string message) : base(message)
    {
    }

    public Error(int line, string message) : base(message)
    {
        _line = line;
    }

    public Error(Token token, string message) : base(message)
    {
        _line = token.Line;
        _token = token;
    }
}
