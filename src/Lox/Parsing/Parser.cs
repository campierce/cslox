using Lox.AST;

namespace Lox.Parsing;

internal class Parser
{
    #region Fields/Properties/Delegates
    private readonly List<Token> _tokens;

    private int _current;

    private bool IsAtEnd => Peek().Type == TokenType.EOF;

    private delegate Expr BinaryLikeExprOperand();

    private delegate TItem ListItemConsumer<TItem>();
    #endregion

    #region Constructors
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _current = 0;
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
            catch (ParsingError)
            {
                // parsing error means we won't try to interpret the statements
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

    private Token Consume(TokenType type, string message)
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
            if (Previous().Type == TokenType.Semicolon)
            {
                return;
            }

            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
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
        // assignment → ( call "." )? IDENTIFIER "=" assignment
        //            | logicOr ;

        Expr expr = Or();

        if (Match(TokenType.Equal))
        {
            Token equals = Previous();
            Expr value = Assignment(); // right-associative

            if (expr is Expr.Variable variable)
            {
                return new Expr.Assign(variable, value);
            }
            else if (expr is Expr.Get get)
            {
                return new Expr.Set(get.Object, get.Name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        // logicOr → logicAnd ( "or" logicAnd )* ;

        return BinaryLikeExpr<Expr.Logical>(And, TokenType.Or);
    }

    private Expr And()
    {
        // logicAnd → equality ( "and" equality )* ;

        return BinaryLikeExpr<Expr.Logical>(Equality, TokenType.And);
    }

    private Expr Equality()
    {
        // equality → comparison ( ( "!=" | "==" ) comparison )* ;

        return BinaryLikeExpr<Expr.Binary>(Comparison, TokenType.BangEqual, TokenType.EqualEqual);
    }

    private Expr Comparison()
    {
        // comparison → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;

        return BinaryLikeExpr<Expr.Binary>(
            Term, TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual
        );
    }

    private Expr Term()
    {
        // term → factor ( ( "-" | "+" ) factor )* ;

        return BinaryLikeExpr<Expr.Binary>(Factor, TokenType.Minus, TokenType.Plus);
    }

    private Expr Factor()
    {
        // factor → unary ( ( "/" | "*" ) unary )* ;

        return BinaryLikeExpr<Expr.Binary>(Unary, TokenType.Slash, TokenType.Star);
    }

    private Expr Unary()
    {
        // unary → ( "!" | "-" ) unary
        //       | call ;

        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token @operator = Previous();
            Expr right = Unary();
            return new Expr.Unary(@operator, right);
        }

        return Call();
    }

    private Expr Call()
    {
        // call → primary ( "(" arguments? ")" | "." IDENTIFIER )* ;

        Expr expr = Primary();

        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                List<Expr> args = Arguments();
                Token paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
                expr = new Expr.Call(expr, paren, args);
            }
            else if (Match(TokenType.Dot))
            {
                Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
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

        return ItemList("arguments", Expression);
    }

    private Expr Primary()
    {
        // primary → "true" | "false" | "nil"
        //         | NUMBER | STRING
        //         | IDENTIFIER
        //         | "(" expression ")" ;

        if (Match(TokenType.False)) { return new Expr.Literal(false); }
        if (Match(TokenType.True)) { return new Expr.Literal(true); }
        if (Match(TokenType.Nil)) { return Nil.Literal; }

        if (Match(TokenType.Number, TokenType.String))
        {
            // the scanner sets a non-null literal on these token types
            return new Expr.Literal(Previous().Literal!);
        }

        if (Match(TokenType.Identifier))
        {
            return new Expr.Variable(Previous());
        }

        if (Match(TokenType.LeftParen))
        {
            Expr expr = Expression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }
    #endregion

    #region Statements
    private Stmt Declaration()
    {
        // declaration → classDecl
        //             | funcDecl
        //             | varDecl
        //             | statement ;

        #pragma warning disable format
        if (Match(TokenType.Class)) { return ClassDeclaration(); }
        if (Match(TokenType.Fun))   { return Function("function"); } // funDecl → "fun" function ;
        if (Match(TokenType.Var))   { return VarDeclaration(); }
        return Statement();
        #pragma warning restore format
    }

    private Stmt ClassDeclaration()
    {
        // classDecl → "class" IDENTIFIER "{" function* "}" ;

        Token name = Consume(TokenType.Identifier, "Expect class name.");
        Consume(TokenType.LeftBrace, "Expect '{' before class body.");

        List<Stmt.Function> methods = new();
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            methods.Add(Function("method"));
        }

        Consume(TokenType.RightBrace, "Expect '}' after class body.");

        return new Stmt.Class(name, methods);
    }

    private Stmt.Function Function(string kind)
    {
        // function → IDENTIFIER "(" parameters? ")" block ;

        Token name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");

        List<Token> parameters = new();
        if (!Check(TokenType.RightParen))
        {
            parameters = Parameters();
        }
        Consume(TokenType.RightParen, "Expect ')' after parameters.");

        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        Stmt.Block body = Block();

        return new Stmt.Function(name, parameters, body);
    }

    private List<Token> Parameters()
    {
        // parameters → IDENTIFIER ( "," IDENTIFIER )* ;

        return ItemList(
            "parameters", () => Consume(TokenType.Identifier, "Expect parameter name.")
        );
    }

    private Stmt VarDeclaration()
    {
        // varDecl → "var" IDENTIFIER ( "=" expression )? ";" ;

        Token name = Consume(TokenType.Identifier, "Expect variable name.");

        Expr initializer;
        if (Match(TokenType.Equal))
        {
            initializer = Expression();
        }
        else
        {
            initializer = Nil.Literal;
        }

        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    private Stmt Statement()
    {
        // statement → forStmt
        //           | ifStmt
        //           | printStmt
        //           | returnStmt
        //           | whileStmt
        //           | block
        //           | exprStmt ;

        #pragma warning disable format
        if (Match(TokenType.For))       { return ForStatement(); }
        if (Match(TokenType.If))        { return IfStatement(); }
        if (Match(TokenType.Print))     { return PrintStatement(); }
        if (Match(TokenType.Return))    { return ReturnStatement(); }
        if (Match(TokenType.While))     { return WhileStatement(); }
        if (Match(TokenType.LeftBrace)) { return Block(); }
        return ExpressionStatement();
        #pragma warning restore format
    }

    private Stmt ForStatement()
    {
        // forStmt → "for" "(" ( varDecl | exprStmt | ";" )
        //           expression? ";"
        //           expression? ")" statement ;

        Consume(TokenType.LeftParen, "Expect '(' after 'for'.");

        Stmt? initializer;
        if (Match(TokenType.Semicolon))
        {
            initializer = null;
        }
        else if (Match(TokenType.Var))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = Expression();
        }
        Consume(TokenType.Semicolon, "Expect ';' after loop condition");

        Expr? increment = null;
        if (!Check(TokenType.RightParen))
        {
            increment = Expression();
        }
        Consume(TokenType.RightParen, "Expect ')' after for clauses.");

        Stmt body = Statement();

        // translate ("desugar") to a while loop...

        // evaluate the increment after the body
        if (increment is not null)
        {
            body = new Stmt.Block(new List<Stmt> { body, new Stmt.Expression(increment) });
        }

        // make sure there's a condition
        condition ??= new Expr.Literal(true);

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

        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        Stmt thenBranch = Statement(); // notice: not a declaration

        Stmt? elseBranch = null;
        if (Match(TokenType.Else))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt PrintStatement()
    {
        // printStmt → "print" expression ";" ;

        Expr value = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    private Stmt ReturnStatement()
    {
        // returnStmt → "return" expression? ";" ;

        Token keyword = Previous();

        Expr value;
        if (!Check(TokenType.Semicolon))
        {
            value = Expression();
        }
        else
        {
            value = Nil.Literal;
        }

        Consume(TokenType.Semicolon, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt WhileStatement()
    {
        // whileStmt → "while" "(" expression ")" statement ;

        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");
        Stmt body = Statement();

        return new Stmt.While(condition, body);
    }

    private Stmt.Block Block()
    {
        // block → "{" declaration* "}" ;

        List<Stmt> statements = new();
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return new Stmt.Block(statements);
    }

    private Stmt ExpressionStatement()
    {
        // exprStmt → expression ";" ;

        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }
    #endregion

    #region Helpers
    private static ParsingError Error(Token token, string message)
    {
        ParsingError error = new(token, message);
        Lox.Error(error);
        return error;
    }

    /// <summary>
    /// Parses a "binary-like" expression, i.e. an <see cref="Expr.Binary"/> or
    /// <see cref="Expr.Logical"/>.
    /// </summary>
    /// <typeparam name="TExpr">The concrete type to parse.</typeparam>
    /// <param name="operand">A delegate that parses expressions of higher precedence than that
    /// specified by the given operators.</param>
    /// <param name="operators">The operators that are allowed to participate in this binary-like
    /// expression.</param>
    /// <returns>An expression.</returns>
    private Expr BinaryLikeExpr<TExpr>(
        BinaryLikeExprOperand operand, params TokenType[] operators
    ) where TExpr : Expr
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
        // itemList → item ( "," item )* ;

        List<TItem> items = new();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (items.Count > 255)
                {
                    Error(Peek(), $"Can't have more than 255 {kind}.");
                }
                items.Add(consumer());
            } while (Match(TokenType.Comma));
        }

        return items;
    }
    #endregion
}
