namespace cslox.lox.scanner;

internal class Scanner
{
    #region Fields/Properties
    /// <summary>
    /// Source text to scan.
    /// </summary>
    private readonly string _source;
    /// <summary>
    /// Tokens derived from the source text.
    /// </summary>
    private readonly List<Token> _tokens = new();
    /// <summary>
    /// Index of the character that starts the token under consideration.
    /// </summary>
    private int _start = 0;
    /// <summary>
    /// Index of the character under consideration.
    /// </summary>
    private int _current = 0;
    /// <summary>
    /// Line number under consideration.
    /// </summary>
    private int _line = 1;
    /// <summary>
    /// Length of the token under consideration.
    /// </summary>
    private int CurrentLength => (_current - _start);
    /// <summary>
    /// Whether the scanner has reached the end of the source text.
    /// </summary>
    private bool IsAtEnd => (_current >= _source.Length);
    /// <summary>
    /// Maps keyword literals to their token types.
    /// </summary>
    private static readonly Dictionary<string, TokenType> s_keywordMap = new()
    {
        ["and"]    = TokenType.AND,
        ["class"]  = TokenType.CLASS,
        ["else"]   = TokenType.ELSE,
        ["false"]  = TokenType.FALSE,
        ["for"]    = TokenType.FOR,
        ["fun"]    = TokenType.FUN,
        ["if"]     = TokenType.IF,
        ["nil"]    = TokenType.NIL,
        ["or"]     = TokenType.OR,
        ["print"]  = TokenType.PRINT,
        ["return"] = TokenType.RETURN,
        ["super"]  = TokenType.SUPER,
        ["this"]   = TokenType.THIS,
        ["true"]   = TokenType.TRUE,
        ["var"]    = TokenType.VAR,
        ["while"]  = TokenType.WHILE
    };
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new Scanner.
    /// </summary>
    /// <param name="source">Source text.</param>
    public Scanner(string source)
    {
        this._source = source;
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

    #region Core logic
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
                AddToken(TokenType.LEFT_PAREN);
                break;
            case ')':
                AddToken(TokenType.RIGHT_PAREN);
                break;
            case '{':
                AddToken(TokenType.LEFT_BRACE);
                break;
            case '}':
                AddToken(TokenType.RIGHT_BRACE);
                break;
            case ',':
                AddToken(TokenType.COMMA);
                break;
            case '.':
                AddToken(TokenType.DOT);
                break;
            case '-':
                AddToken(TokenType.MINUS);
                break;
            case '+':
                AddToken(TokenType.PLUS);
                break;
            case ';':
                AddToken(TokenType.SEMICOLON);
                break;
            case '*':
                AddToken(TokenType.STAR);
                break;
            case '/':
                Slash();
                break;
            // one- or two-char tokens
            case '!':
                AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
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
                    Lox.Error(_line, "Unexpected character.");
                }
                break;
        }
    }
    #endregion

    #region Source text access
    /// <summary>
    /// Advances to the next character.
    /// </summary>
    /// <returns>The current character.</returns>
    private char Advance()
    {
        return _source[_current++];
    }

    /// <summary>
    /// Checks if the current and expected characters match. If so, advances.
    /// </summary>
    /// <param name="expected">The expected character.</param>
    /// <returns>Whether the current and expected characters match.</returns>
    private bool Match(char expected)
    {
        if (IsAtEnd || _source[_current] != expected)
        {
            return false;
        }
        _current++;
        return true;
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
        if (_current + 1 >= _source.Length)
        {
            return '\0';
        }
        return _source[_current + 1];
    }
    #endregion

    #region Char-acterizers
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

    #region Multi-character tokenizers
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
        AddToken(TokenType.SLASH);
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
            Lox.Error(_line, "Unterminated string.");
            return;
        }

        // consume the closing "
        Advance();

        // trim surrounding quotes
        string value = _source.Substring(_start + 1, CurrentLength - 2);
        // tokenize
        AddToken(TokenType.STRING, value);
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
        double value = Double.Parse(_source.Substring(_start, CurrentLength));
        // tokenize
        AddToken(TokenType.NUMBER, value);
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
            type = TokenType.IDENTIFIER;
        }

        // tokenize
        AddToken(type);
    }
    #endregion

    #region Token factories
    /// <summary>
    /// Adds token to list. Call this when token has no literal component.
    /// </summary>
    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    /// <summary>
    /// Adds token to list.
    /// </summary>
    private void AddToken(TokenType type, object? literal)
    {
        string text = _source.Substring(_start, CurrentLength);
        _tokens.Add(new Token(type, text, literal, _line));
    }
    #endregion
}
