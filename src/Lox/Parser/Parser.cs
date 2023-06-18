using cslox.lox;
using cslox.lox.ir;
using cslox.lox.scanner;
using static cslox.lox.scanner.TokenType;

namespace lox.cslox.parser;

internal class Parser
{
    #region Exceptions
    private class ParseError : Exception
    {
    }
    #endregion

    #region Fields/Properties
    private readonly List<Token> _tokens;

    private int _current = 0;

    private bool IsAtEnd => Peek().Type == EOF;
    #endregion

    #region Constructors
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }
    #endregion

    #region API
    public Expr? Parse()
    {
        try
        {
            return Expression();
        }
        catch // ParseError
        {
            return null;
        }
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

    #region Grammar helpers
    private delegate Expr BinaryExprOperand();
    
    private Expr LeftAssociativeBinaryExpr(BinaryExprOperand operand, params TokenType[] operators)
    {
        Expr expr = operand();

        while (Match(operators))
        {
            Token _operator = Previous();
            Expr right = operand();
            expr = new Expr.Binary(expr, _operator, right);
        }

        return expr;
    }
    #endregion

    #region Grammar rules
    private Expr Expression()
    {
        // expression → equality ;
        
        return Equality();
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
            Token _operator = Previous();
            Expr right = Unary();
            return new Expr.Unary(_operator, right);
        }

        return Primary();
    }

    private Expr Primary()
    {
        // primary → NUMBER | STRING | "true" | "false" | "nil"
        //         | "(" expression ")" ;

        if (Match(FALSE)) return new Expr.Literal(false);
        if (Match(TRUE)) return new Expr.Literal(true);
        if (Match(NIL)) return new Expr.Literal(Nil.Instance);

        if (Match(NUMBER, STRING))
        {
            return new Expr.Literal(Previous().Literal);
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
}
