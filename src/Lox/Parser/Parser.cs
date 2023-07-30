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

    private delegate Expr BinaryLikeExprOperand();

    private delegate TItem ListItemConsumer<TItem>();
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
        //            | logicOr ;

        Expr expr = Or();

        if (Match(EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment(); // right-associative

            if (expr is Expr.Variable variable)
            {
                return new Expr.Assign(variable.Name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        // logicOr → logicAnd ( "or" logicAnd )* ;

        return BinaryLikeExpr<Expr.Logical>(And, OR);
    }

    private Expr And()
    {
        // logicAnd → equality ( "and" equality )* ;

        return BinaryLikeExpr<Expr.Logical>(Equality, AND);
    }

    private Expr Equality()
    {
        // equality → comparison ( ( "!=" | "==" ) comparison )* ;

        return BinaryLikeExpr<Expr.Binary>(Comparison, BANG_EQUAL, EQUAL_EQUAL);
    }

    private Expr Comparison()
    {
        // comparison → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;

        return BinaryLikeExpr<Expr.Binary>(Term, GREATER, GREATER_EQUAL, LESS, LESS_EQUAL);
    }

    private Expr Term()
    {
        // term → factor ( ( "-" | "+" ) factor )* ;

        return BinaryLikeExpr<Expr.Binary>(Factor, MINUS, PLUS);
    }

    private Expr Factor()
    {
        // factor → unary ( ( "/" | "*" ) unary )* ;

        return BinaryLikeExpr<Expr.Binary>(Unary, SLASH, STAR);
    }

    private Expr Unary()
    {
        // unary → ( "!" | "-" ) unary
        //       | call ;

        if (Match(BANG, MINUS))
        {
            Token @operator = Previous();
            Expr right = Unary();
            return new Expr.Unary(@operator, right);
        }

        return Call();
    }

    private Expr Call()
    {
        // call → primary ( "(" arguments? ")" )* ;

        Expr expr = Primary();

        while (true)
        {
            if (Match(LEFT_PAREN))
            {
                List<Expr> args = Arguments();
                Token paren = Consume(RIGHT_PAREN, "Expect ')' after arguments.");
                expr = new Expr.Call(expr, paren, args);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private List<Expr> Arguments()
    {
        // arguments → expression ( "," expression )* ;

        return ItemList<Expr>("arguments", Expression);
    }

    private Expr Primary()
    {
        // primary → "true" | "false" | "nil"
        //         | NUMBER | STRING
        //         | IDENTIFIER
        //         | "(" expression ")" ;

        if (Match(FALSE)) { return new Expr.Literal(false); }
        if (Match(TRUE)) { return new Expr.Literal(true); }
        if (Match(NIL)) { return Nil.Literal; }

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
        // declaration → funcDecl
        //             | varDecl
        //             | statement ;

        if (Match(FUN)) { return Function("function"); } // funDecl → "fun" function ;
        if (Match(VAR)) { return VarDeclaration(); }
        return Statement();
    }

    private Stmt Function(string kind)
    {
        // function → IDENTIFIER "(" parameters? ")" block ;

        Token name = Consume(IDENTIFIER, $"Expect {kind} name.");
        Consume(LEFT_PAREN, $"Expect '(' after {kind} name.");

        List<Token> parameters = new();
        if (!Check(RIGHT_PAREN))
        {
            parameters = Parameters();
        }
        Consume(RIGHT_PAREN, "Expect ')' after parameters.");

        Consume(LEFT_BRACE, "Expect '{' before " + kind + " body.");
        List<Stmt> body = Block();

        return new Stmt.Function(name, parameters, body);
    }

    private List<Token> Parameters()
    {
        // parameters → IDENTIFIER ( "," IDENTIFIER )* ;

        return ItemList<Token>("parameters", () => Consume(IDENTIFIER, "Expect parameter name."));
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
            initializer = Nil.Literal;
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    private Stmt Statement()
    {
        // statement → forStmt
        //           | ifStmt
        //           | printStmt
        //           | whileStmt
        //           | block
        //           | exprStmt ;

        if (Match(FOR)) { return ForStatement(); }
        if (Match(IF)) { return IfStatement(); }
        if (Match(PRINT)) { return PrintStatement(); }
        if (Match(WHILE)) { return WhileStatement(); }
        if (Match(LEFT_BRACE)) { return new Stmt.Block(Block()); }
        return ExpressionStatement();
    }

    private Stmt ForStatement()
    {
        // forStmt → "for" "(" ( varDecl | exprStmt | ";" )
        //           expression? ";"
        //           expression? ")" statement ;

        Consume(LEFT_PAREN, "Expect '(' after 'for'.");

        Stmt? initializer;
        if (Match(SEMICOLON))
        {
            initializer = null;
        }
        else if (Match(VAR))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr? condition = null;
        if (!Check(SEMICOLON))
        {
            condition = Expression();
        }
        Consume(SEMICOLON, "Expect ';' after loop condition");

        Expr? increment = null;
        if (!Check(RIGHT_PAREN))
        {
            increment = Expression();
        }
        Consume(RIGHT_PAREN, "Expect ')' after for clauses.");

        Stmt body = Statement();

        // translate ("desugar") to a while loop...

        // evaluate the increment after the body
        if (increment is not null)
        {
            body = new Stmt.Block(
                new List<Stmt> { body, new Stmt.Expression(increment) });
        }

        // make sure there's a condition
        if (condition is null)
        {
            condition = new Expr.Literal(true);
        }

        // create the loop
        body = new Stmt.While(condition, body);

        // run the initializer once, before the loop
        if (initializer is not null)
        {
            body = new Stmt.Block(new List<Stmt> { initializer, body });
        }

        return body;
    }

    private Stmt IfStatement()
    {
        // ifStmt → "if" "(" expression ")" statement
        //          ( "else" statement )? ;

        Consume(LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after if condition.");

        Stmt thenBranch = Statement(); // notice: not a declaration

        Stmt? elseBranch = null;
        if (Match(ELSE))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt PrintStatement()
    {
        // printStmt → "print" expression ";" ;

        Expr value = Expression();
        Consume(SEMICOLON, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt WhileStatement()
    {
        // whileStmt → "while" "(" expression ")" statement ;

        Consume(LEFT_PAREN, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after condition.");
        Stmt body = Statement();

        return new Stmt.While(condition, body);
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

    #region Generic helpers
    /// <summary>
    /// Parses a "binary-like" expression, i.e. an <see cref="Expr.Binary"/> or
    /// <see cref="Expr.Logical"/>.
    /// </summary>
    /// <typeparam name="TExpr">The concrete type to parse.</typeparam>
    /// <param name="operand">
    /// A delegate that parses expressions of higher precedence than that specified by the given
    /// operators.
    /// </param>
    /// <param name="operators">
    /// The operators that are allowed to participate in this binary-like expression.
    /// </param>
    /// <returns>An expression.</returns>
    private Expr BinaryLikeExpr<TExpr>(
        BinaryLikeExprOperand operand, params TokenType[] operators) where TExpr : Expr
    {
        // binaryLikeExpr → operand ( operators[x] operand )* ;

        // parse expressions of higher precedence
        Expr expr = operand();

        // consume operators at the current precedence level
        while (Match(operators))
        {
            Token @operator = Previous();
            Expr right = operand();
            // new expression is left-associative
            expr = (TExpr)Activator.CreateInstance(typeof(TExpr), expr, @operator, right)!;
        }

        return expr;
    }

    /// <summary>
    /// Parses a comma-separated list of items, i.e. <see cref="Stmt.Function.Params"/> or
    /// <see cref="Expr.Call.Arguments"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of items.</typeparam>
    /// <param name="kind">The name of the collection of items.</param>
    /// <param name="consumer">A delegate that knows how to parse an item.</param>
    /// <returns>A list of items.</returns>
    private List<TItem> ItemList<TItem>(string kind, ListItemConsumer<TItem> consumer)
    {
        // itemsList → item ( "," item )* ;

        List<TItem> items = new();
    
        if (!Check(RIGHT_PAREN))
        {
            do
            {
                if (items.Count > 255)
                {
                    Error(Peek(), $"Can't have more than 255 {kind}.");
                }
                items.Add(consumer());
            } while (Match(COMMA));
        }

        return items;
    }
    #endregion
}
