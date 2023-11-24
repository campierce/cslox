namespace Lox.Scanning;

internal class Scanner
{
    #region Fields/Properties
    /// <summary>
    /// The source text to be scanned.
    /// </summary>
    private readonly string _source;

    /// <summary>
    /// Index of the current character.
    /// </summary>
    private int _current;

    /// <summary>
    /// Current line number.
    /// </summary>
    private int _line;

    /// <summary>
    /// Index of the character that starts the current token.
    /// </summary>
    private int _start;

    /// <summary>
    /// Tokens derived from the source text.
    /// </summary>
    private readonly List<Token> _tokens;

    /// <summary>
    /// Length of the current token.
    /// </summary>
    private int CurrentLength => _current - _start;

    /// <summary>
    /// Whether the scanner has reached the end of the source text.
    /// </summary>
    private bool IsAtEnd => _current >= _source.Length;

    /// <summary>
    /// Maps keyword literals to their token types.
    /// </summary>
    private static readonly Dictionary<string, TokenType> s_keywordMap = new()
    {
        #pragma warning disable format
        ["and"]    = TokenType.And,
        ["class"]  = TokenType.Class,
        ["else"]   = TokenType.Else,
        ["false"]  = TokenType.False,
        ["for"]    = TokenType.For,
        ["fun"]    = TokenType.Fun,
        ["if"]     = TokenType.If,
        ["nil"]    = TokenType.Nil,
        ["or"]     = TokenType.Or,
        ["print"]  = TokenType.Print,
        ["return"] = TokenType.Return,
        ["super"]  = TokenType.Super,
        ["this"]   = TokenType.This,
        ["true"]   = TokenType.True,
        ["var"]    = TokenType.Var,
        ["while"]  = TokenType.While
        #pragma warning restore format
    };
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new Scanner.
    /// </summary>
    /// <param name="sourceText">The source text to be scanned.</param>
    public Scanner(string sourceText)
    {
        _source = sourceText;
        _current = 0;
        _line = 1;
        _start = 0;
        _tokens = new();
    }
    #endregion

    #region API
    /// <summary>
    /// Builds a list of tokens from the source text.
    /// </summary>
    /// <returns>A list of tokens.</returns>
    public List<Token> ScanTokens()
    {
        while (!IsAtEnd)
        {
            _start = _current;
            ScanToken();
        }
        _tokens.Add(new Token(TokenType.EOF, string.Empty, null, _line));
        return _tokens;
    }
    #endregion

    #region Source text access
    /// <summary>
    /// Gets the current character, and advances to the next.
    /// </summary>
    /// <returns>The current character.</returns>
    private char Advance()
    {
        return _source[_current++];
    }

    /// <summary>
    /// Gets the current character, without advancing.
    /// </summary>
    /// <returns>The current character.</returns>
    private char Peek()
    {
        if (IsAtEnd)
        {
            return '\0';
        }
        return _source[_current];
    }

    /// <summary>
    /// Gets the next character, without advancing.
    /// </summary>
    /// <returns>The next character.</returns>
    private char PeekNext()
    {
        int next = _current + 1;
        if (next >= _source.Length)
        {
            return '\0';
        }
        return _source[next];
    }

    /// <summary>
    /// Checks if the current and expected characters match. If so, advances.
    /// </summary>
    /// <param name="expected">The expected character.</param>
    /// <returns>Whether the current and expected characters match.</returns>
    private bool Match(char expected)
    {
        if (IsAtEnd || Peek() != expected)
        {
            return false;
        }
        _current++;
        return true;
    }
    #endregion

    #region Token list access
    /// <summary>
    /// Adds a non-literal token to the list.
    /// </summary>
    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    /// <summary>
    /// Adds a token to the list.
    /// </summary>
    private void AddToken(TokenType type, object? literal)
    {
        string text = _source.Substring(_start, CurrentLength);
        _tokens.Add(new Token(type, text, literal, _line));
    }
    #endregion

    #region Token creation
    /// <summary>
    /// Creates the next token. (Where the magic happens.)
    /// </summary>
    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            // one-char tokens
            case '(':
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                AddToken(TokenType.RightParen);
                break;
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            case ',':
                AddToken(TokenType.Comma);
                break;
            case '.':
                AddToken(TokenType.Dot);
                break;
            case '-':
                AddToken(TokenType.Minus);
                break;
            case '+':
                AddToken(TokenType.Plus);
                break;
            case ';':
                AddToken(TokenType.Semicolon);
                break;
            case '*':
                AddToken(TokenType.Star);
                break;
            case '/':
                Slash();
                break;
            // one- or two-char tokens
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            // ignore whitespace
            case ' ':
            case '\r':
            case '\t':
                break;
            // track new lines
            case '\n':
                _line++;
                break;
            // literal string
            case '"':
                String();
                break;
            // ok, we've exhausted tokens with a distinct first char
            default:
                // literal number
                if (IsDigit(c))
                {
                    Number();
                }
                // identifier or keyword
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    Error("Unexpected character.");
                }
                break;
        }
    }

    /// <summary>
    /// Tokenizes a thing that starts with a slash (comment or slash).
    /// </summary>
    private void Slash()
    {
        // handle comments
        if (Match('/'))
        {
            // by throwing them out
            while (Peek() != '\n' && !IsAtEnd)
            {
                Advance();
            }
            return;
        }

        // else it's an actual slash
        AddToken(TokenType.Slash);
    }

    /// <summary>
    /// Tokenizes a string.
    /// </summary>
    private void String()
    {
        // walk the string to its end
        while (Peek() != '"' && !IsAtEnd)
        {
            // multiline strings are fine
            if (Peek() == '\n')
            {
                _line++;
            }
            Advance();
        }

        // if we ran out of road, that's a problem
        if (IsAtEnd)
        {
            Error("Unterminated string.");
            return;
        }

        // consume the closing "
        Advance();

        // trim surrounding quotes
        string value = _source.Substring(_start + 1, CurrentLength - 2);
        // tokenize
        AddToken(TokenType.String, value);
    }

    /// <summary>
    /// Tokenizes a number.
    /// </summary>
    private void Number()
    {
        // walk the integer part
        while (IsDigit(Peek()))
        {
            Advance();
        }

        // look for a fractional part
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // consume the .
            Advance();
            // walk the rest
            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        // parse the number
        double value = double.Parse(_source.Substring(_start, CurrentLength));
        // tokenize
        AddToken(TokenType.Number, value);
    }

    /// <summary>
    /// Tokenizes an identifier.
    /// </summary>
    private void Identifier()
    {
        // walk to the end
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        // grab the lexeme
        string text = _source.Substring(_start, CurrentLength);

        // maybe it's a keyword we support
        if (!s_keywordMap.TryGetValue(text, out TokenType type))
        {
            // otherwise it's an identifier
            type = TokenType.Identifier;
        }

        // tokenize
        AddToken(type);
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Logs a scanning error.
    /// </summary>
    /// <param name="message">The error message.</param>
    private void Error(string message)
    {
        Lox.Error(new ScanningError(_line, message));
    }

    /// <summary>
    /// Decides whether a character is a digit. Supported: [0-9].
    /// </summary>
    /// <param name="c">The character to check</param>
    /// <returns>Whether the given character is a digit.</returns>
    private static bool IsDigit(char c)
    {
        // char.IsDigit gets fancy with Unicode, so we roll our own
        return c >= '0' && c <= '9';
    }

    /// <summary>
    /// Decides whether a character is alphabetical. Supported: [a-z,A-Z,_].
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>Whether the given character is alphabetical.</returns>
    private static bool IsAlpha(char c)
    {
        return char.IsAsciiLetter(c) || c == '_';
    }

    /// <summary>
    /// Decides whether a character is alphanumeric.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>Whether the given character is alphanumeric.</returns>
    private static bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }
    #endregion
}
