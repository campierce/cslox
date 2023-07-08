using Lox.IR;
using Lox.Scanning;
using static Lox.Scanning.TokenType;

namespace Lox.Parsing;

internal class Parser
{
    #region Fields/Properties/Delegates
    private readonly List<Token> _tokens;

    private int _current = 0;

    private bool IsAtEnd => Peek().Type == EOF;

    private delegate Expr BinaryExprOperand();
    #endregion

    #region Constructors
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }
    #endregion

    #region API
    public List<Stmt> Parse()
    {
        // program → declaration* EOF ;

        List<Stmt> statements = new();
        while (!IsAtEnd)
        {
            try
            {
                statements.Add(Declaration());
            }
            catch (ParseError)
            {
                // parse error means we won't try to interpret the statements
                // but we should recover and keep parsing, to see what else we find
                Synchronize();
            }
        }
        return statements;
    }
    #endregion

    #region Token list access
    private Token Previous()
    {
        return _tokens[_current - 1];
    }

    private Token Advance()
    {
        if (!IsAtEnd)
        {
            _current++;
        }
        return Previous();
    }

    private Token Peek()
    {
        return _tokens[_current];
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd)
        {
            return false;
        }
        return Peek().Type == type;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private ParseError Error(Token token, String message)
    {
        Lox.Error(token, message);
        return new ParseError();
    }

    private Token Consume(TokenType type, String message)
    {
        if (Check(type))
        {
            return Advance();
        }
        throw Error(Peek(), message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd)
        {
            if (Previous().Type == SEMICOLON)
            {
                return;
            }

            switch (Peek().Type)
            {
                case CLASS:
                case FUN:
                case VAR:
                case FOR:
                case IF:
                case WHILE:
                case PRINT:
                case RETURN:
                    return;
            }

            Advance();
        }
    }
    #endregion

    #region Expressions
    private Expr Expression()
    {
        // expression → assignment ;

        return Assignment();
    }

    private Expr Assignment()
    {
        // assignment → IDENTIFIER "=" assignment
        //            | equality ;

        Expr expr = Equality();

        if (Match(EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment(); // right associative

            if (expr is Expr.Variable variable)
            {
                return new Expr.Assign(variable.Name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr LeftAssociativeBinaryExpr(BinaryExprOperand operand, params TokenType[] operators)
    {
        // parse expressions with higher precedence
        Expr expr = operand();

        // consume operators at the current precedence level
        while (Match(operators))
        {
            Token @operator = Previous();
            Expr right = operand();
            expr = new Expr.Binary(expr, @operator, right); // left associative
        }

        return expr;
    }

    private Expr Equality()
    {
        // equality → comparison ( ( "!=" | "==" ) comparison )* ;

        return LeftAssociativeBinaryExpr(Comparison, BANG_EQUAL, EQUAL_EQUAL);
    }

    private Expr Comparison()
    {
        // comparison → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;

        return LeftAssociativeBinaryExpr(Term, GREATER, GREATER_EQUAL, LESS, LESS_EQUAL);
    }

    private Expr Term()
    {
        // term → factor ( ( "-" | "+" ) factor )* ;

        return LeftAssociativeBinaryExpr(Factor, MINUS, PLUS);
    }

    private Expr Factor()
    {
        // factor → unary ( ( "/" | "*" ) unary )* ;

        return LeftAssociativeBinaryExpr(Unary, SLASH, STAR);
    }

    private Expr Unary()
    {
        // unary → ( "!" | "-" ) unary
        //       | primary ;

        if (Match(BANG, MINUS))
        {
            Token @operator = Previous();
            Expr right = Unary();
            return new Expr.Unary(@operator, right);
        }

        return Primary();
    }

    private Expr Primary()
    {
        // primary → "true" | "false" | "nil"
        //         | NUMBER | STRING
        //         | "(" expression ")"
        //         | IDENTIFIER ;

        if (Match(FALSE)) { return new Expr.Literal(false); }
        if (Match(TRUE)) { return new Expr.Literal(true); }
        if (Match(NIL)) { return Nil.GetLiteral(); }

        if (Match(NUMBER, STRING))
        {
            return new Expr.Literal(Previous().Literal);
        }

        if (Match(IDENTIFIER))
        {
            return new Expr.Variable(Previous());
        }

        if (Match(LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }
    #endregion

    #region Statements
    private Stmt Declaration()
    {
        // declaration → varDecl
        //             | statement ;

        if (Match(VAR))
        {
            return VarDeclaration();
        }
        return Statement();
    }

    private Stmt VarDeclaration()
    {
        // varDecl → "var" IDENTIFIER ( "=" expression )? ";" ;

        Token name = Consume(IDENTIFIER, "Expect variable name.");

        Expr initializer;
        if (Match(EQUAL))
        {
            initializer = Expression();
        }
        else
        {
            initializer = Nil.GetLiteral();
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }
    
    private Stmt Statement()
    {
        // statement → printStmt
        //           | block
        //           | exprStmt ;

        if (Match(PRINT)) { return PrintStatement(); }
        if (Match(LEFT_BRACE)) { return new Stmt.Block(Block()); }
        return ExpressionStatement();
    }

    private Stmt PrintStatement()
    {
        // printStmt → "print" expression ";" ;

        Expr value = Expression();
        Consume(SEMICOLON, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt ExpressionStatement()
    {
        // exprStmt → expression ";" ;

        Expr expr = Expression();
        Consume(SEMICOLON, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    private List<Stmt> Block()
    {
        // block → "{" declaration* "}" ;

        List<Stmt> statements = new();
        while (!Check(RIGHT_BRACE) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }

        Consume(RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }
    #endregion
}
