namespace Lox;

internal class Parser
{
    #region State
    private readonly List<Token> _tokens;

    private int _current;

    private bool IsAtEnd => Peek().Type == TokenType.EOF;

    private delegate Expr BinaryLikeExprOperand();

    private delegate TItem ItemConsumer<TItem>();
    #endregion

    #region Constructor
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

        List<Stmt> statements = [];
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
        // assignment → ( call "." )? IDENTIFIER "=" assignment | logicOr ;

        Expr expr = Or();

        if (Match(TokenType.Equal))
        {
            Token equals = Previous();
            Expr value = Assignment(); // right-associative

            if (expr is Expr.Variable variable)
            {
                return new Expr.Assign(variable.Name, value);
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
        // unary → ( "!" | "-" ) unary | call ;

        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token op = Previous();
            Expr right = Unary();
            return new Expr.Unary(op, right);
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
        // primary → "true" | "false" | "nil" | "this"
        //         | NUMBER | STRING | IDENTIFIER | "(" expression ")"
        //         | "super" "." IDENTIFIER ;

        if (Match(TokenType.False)) { return new Expr.Literal(false); }
        if (Match(TokenType.True)) { return new Expr.Literal(true); }
        if (Match(TokenType.Nil)) { return new Expr.Literal(Nil.Instance); }

        if (Match(TokenType.This))
        {
            return new Expr.This(Previous());
        }

        if (Match(TokenType.Number, TokenType.String))
        {
            // we are assured the scanner set a non-null literal on these token types
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

        if (Match(TokenType.Super))
        {
            Token keyword = Previous();
            Consume(TokenType.Dot, "Expect '.' after 'super'.");
            Token method = Consume(TokenType.Identifier, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
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

        if (Match(TokenType.Class)) { return ClassDeclaration(); }
        if (Match(TokenType.Fun)) { return Function("function"); }
        if (Match(TokenType.Var)) { return VarDeclaration(); }
        return Statement();
    }

    private Stmt.Class ClassDeclaration()
    {
        // classDecl → "class" IDENTIFIER ( "<" IDENTIFIER )? "{" function* "}" ;

        Token name = Consume(TokenType.Identifier, "Expect class name.");

        Expr.Variable? superclass = null;
        if (Match(TokenType.Less))
        {
            Consume(TokenType.Identifier, "Expect superclass name.");
            superclass = new Expr.Variable(Previous());
        }

        Consume(TokenType.LeftBrace, "Expect '{' before class body.");

        List<Stmt.Function> methods = [];
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            methods.Add(Function("method"));
        }

        Consume(TokenType.RightBrace, "Expect '}' after class body.");

        return new Stmt.Class(name, superclass, methods);
    }

    private Stmt.Function Function(string kind)
    {
        // funDecl → "fun" function ;
        // function → IDENTIFIER "(" parameters? ")" block ;

        Token name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");

        List<Token> parameters = [];
        if (!Check(TokenType.RightParen))
        {
            parameters = Parameters();
        }
        Consume(TokenType.RightParen, "Expect ')' after parameters.");

        Consume(TokenType.LeftBrace, $"Expect '{{' before {kind} body.");
        List<Stmt> body = Block();

        return new Stmt.Function(name, parameters, body);
    }

    private List<Token> Parameters()
    {
        // parameters → IDENTIFIER ( "," IDENTIFIER )* ;

        return ItemList(
            "parameters", () => Consume(TokenType.Identifier, "Expect parameter name.")
        );
    }

    private Stmt.Var VarDeclaration()
    {
        // varDecl → "var" IDENTIFIER ( "=" expression )? ";" ;

        Token name = Consume(TokenType.Identifier, "Expect variable name.");

        Expr? initializer;
        if (Match(TokenType.Equal))
        {
            initializer = Expression();
        }
        else
        {
            initializer = null;
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
        if (Match(TokenType.LeftBrace)) { return new Stmt.Block(Block()); }
        return ExpressionStatement();
        #pragma warning restore format
    }

    private Stmt ForStatement()
    {
        // forStmt → "for"
        //           "(" ( varDecl | exprStmt | ";" ) expression? ";" expression? ")"
        //           statement ;

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

        // translate/desugar to a while loop...

        // evaluate the increment after the body
        if (increment is not null)
        {
            body = new Stmt.Block([body, new Stmt.Expression(increment)]);
        }

        // make sure there's a condition
        condition ??= new Expr.Literal(true);

        // create the loop
        body = new Stmt.While(condition, body);

        // run the initializer once, before the loop
        if (initializer is not null)
        {
            body = new Stmt.Block([initializer, body]);
        }

        return body;
    }

    private Stmt.If IfStatement()
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

    private Stmt.Print PrintStatement()
    {
        // printStmt → "print" expression ";" ;

        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new Stmt.Print(expr);
    }

    private Stmt.Return ReturnStatement()
    {
        // returnStmt → "return" expression? ";" ;

        Token keyword = Previous();

        Expr? value = null;
        if (!Check(TokenType.Semicolon))
        {
            value = Expression();
        }

        Consume(TokenType.Semicolon, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt.While WhileStatement()
    {
        // whileStmt → "while" "(" expression ")" statement ;

        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");
        Stmt body = Statement();

        return new Stmt.While(condition, body);
    }

    private List<Stmt> Block()
    {
        // block → "{" declaration* "}" ;

        List<Stmt> statements = [];
        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Stmt.Expression ExpressionStatement()
    {
        // exprStmt → expression ";" ;

        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }
    #endregion

    #region Helpers
    private static ParseError Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseError();
    }

    /// <summary>
    /// Parses a "binary-like" expression. In practice, expressions are either
    /// <see cref="Expr.Binary"/> or <see cref="Expr.Logical"/>.
    /// </summary>
    /// <typeparam name="TExpr">The type to parse.</typeparam>
    /// <param name="operand">A delegate that parses expressions of higher precedence than that
    /// specified by the given operators.</param>
    /// <param name="operators">The operators that are allowed to participate in this binary-like
    /// expression.</param>
    /// <returns>An expression.</returns>
    private Expr BinaryLikeExpr<TExpr>(BinaryLikeExprOperand operand, params TokenType[] operators)
    where TExpr : Expr
    {
        // binaryLikeExpr → operand ( operators[x] operand )* ;

        // parse expression of higher precedence
        Expr expr = operand();

        // operator at the current precedence level
        while (Match(operators))
        {
            Token op = Previous();
            // again, expression of higher precedence
            Expr right = operand();
            // new expression is left-associative
            expr = (TExpr)Activator.CreateInstance(typeof(TExpr), expr, op, right)!;
        }

        return expr;
    }

    /// <summary>
    /// Parses a comma-separated list of items. In practice, items are either
    /// <see cref="Stmt.Function.Params"/> or <see cref="Expr.Call.Arguments"/>.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <param name="kind">The item name, plural.</param>
    /// <param name="consumer">A delegate that knows how to parse an item.</param>
    /// <returns>A list of items.</returns>
    private List<TItem> ItemList<TItem>(string kind, ItemConsumer<TItem> consumer)
    {
        // itemList → item ( "," item )* ;

        List<TItem> items = [];

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
