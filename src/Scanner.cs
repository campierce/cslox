namespace lox;

/// <summary>
/// Tokenizes a piece of text.
/// </summary>
internal class Scanner
{
    #region Fields/Props
    /// <summary>
    /// Source text to scan.
    /// </summary>
    private readonly string source;
    /// <summary>
    /// Tokens derived from source.
    /// </summary>
    private readonly List<Token> tokens = new List<Token>();
    /// <summary>
    /// Start index of the token under consideration.
    /// </summary>
    private int start = 0;
    /// <summary>
    /// Current index in source.
    /// </summary>
    private int current = 0;
    /// <summary>
    /// Line number in source.
    /// </summary>
    private int line = 1;
    /// <summary>
    /// Whether the source has no more characters for us to scan.
    /// </summary>
    private bool isAtEnd => (current >= source.Length);
    /// <summary>
    /// Length of the token under consideration.
    /// </summary>
    private int currentLength => (current - start);
    /// <summary>
    /// Map from keyword literals to their token types.
    /// </summary>
    private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
    {
        ["and"] = TokenType.AND,
        ["class"] = TokenType.CLASS,
        ["else"] = TokenType.ELSE,
        ["false"] = TokenType.FALSE,
        ["for"] = TokenType.FOR,
        ["fun"] = TokenType.FUN,
        ["if"] = TokenType.IF,
        ["nil"] = TokenType.NIL,
        ["or"] = TokenType.OR,
        ["print"] = TokenType.PRINT,
        ["return"] = TokenType.RETURN,
        ["super"] = TokenType.SUPER,
        ["this"] = TokenType.THIS,
        ["true"] = TokenType.TRUE,
        ["var"] = TokenType.VAR,
        ["while"] = TokenType.WHILE
    };
    #endregion

    internal Scanner(string source)
    {
        this.source = source;
    }

    #region API
    /// <summary>
    /// Builds a tokens list from the source text.
    /// </summary>
    internal List<Token> ScanTokens()
    {
        while (!isAtEnd)
        {
            start = current;
            ScanToken();
        }
        tokens.Add(new Token(TokenType.EOF, string.Empty, null, line));
        return tokens;
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
                line++;
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
                // (user-defined) identifier, or keyword
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    Lox.Error(line, "Unexpected character.");
                }
                break;
        }
    }
    #endregion

    #region Text helpers
    /// <summary>
    /// Increments our index in the source by one.
    /// </summary>
    private char Advance()
    {
        return source[current++];
    }

    /// <summary>
    /// Checks if the current char matches an expected char; advances if so.
    /// </summary>
    private bool Match(char expected)
    {
        if (isAtEnd || source[current] != expected)
        {
            return false;
        }
        current++;
        return true;
    }

    /// <summary>
    /// Gets the next char, if it exists.
    /// </summary>
    private char Peek()
    {
        if (isAtEnd)
        {
            return '\0';
        }
        return source[current];
    }

    /// <summary>
    /// Gets the next-next char, if it exists.
    /// </summary>
    private char PeekNext()
    {
        if (current + 1 >= source.Length)
        {
            return '\0';
        }
        return source[current + 1];
    }
    #endregion

    #region Static char-acterizers
    /// <summary>
    /// Decides whether a char is a digit. Supported: [0-9].
    /// </summary>
    private static bool IsDigit(char c)
    {
        // char.IsDigit gets fancy with Unicode, so we roll our own
        return c >= '0' && c <= '9';
    }

    /// <summary>
    /// Decides whether a char is alphabetical. Supported: [a-z,A-Z,_].
    /// </summary>
    private static bool IsAlpha(char c)
    {
        return char.IsAsciiLetter(c) || c == '_';
    }

    /// <summary>
    /// Decides whether a char is alphanumeric.
    /// </summary>
    private static bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }
    #endregion

    #region Type-specific tokenizers
    /// <summary>
    /// Tokenizes a thing that starts with a slash (comment or slash).
    /// </summary>
    private void Slash()
    {
        // handle comments
        if (Match('/'))
        {
            // by throwing them out
            while (Peek() != '\n' && !isAtEnd)
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
        while (Peek() != '"' && !isAtEnd)
        {
            // multiline strings are fine
            if (Peek() == '\n')
            {
                line++;
            }
            Advance();
        }

        // if we ran out of road, that's a problem
        if (isAtEnd)
        {
            Lox.Error(line, "Unterminated string.");
            return;
        }

        // consume the closing "
        Advance();

        // trim surrounding quotes
        string value = source.Substring(start + 1, currentLength - 2);
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
        double value = Double.Parse(source.Substring(start, currentLength));
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
        string text = source.Substring(start, currentLength);

        // maybe it's a keyword we support
        if (!keywords.TryGetValue(text, out TokenType type))
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
    /// Adds token to list; call when token has no literal component.
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
        string text = source.Substring(start, currentLength);
        tokens.Add(new Token(type, text, literal, line));
    }
    #endregion
}
