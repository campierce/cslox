namespace Lox;

internal class Parser
{
    #region State
    /// <summary>
    /// The index of the current token.
    /// </summary>
    private int _current = 0;

    /// <summary>
    /// The tokens to parse.
    /// </summary>
    private readonly List<Token> _tokens;

    /// <summary>
    /// Whether we've reached the end of the tokens.
    /// </summary>
    private bool IsAtEnd => Peek().Type == TokenType.Eof;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a Parser.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }
    #endregion

    #region API
    /// <summary>
    /// Parses the list of tokens into an abstract syntax tree (AST).
    /// </summary>
    /// <returns>The AST as a list of Stmt nodes.</returns>
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
            catch (ParseError) // this means we won't try to interpret the AST later on
            {
                Synchronize(); // but we should recover and keep parsing, to see what else we find
            }
        }
        return statements;
    }
    #endregion

    #region Token list access
    /// <summary>
    /// Gets the current token.
    /// </summary>
    /// <returns>The current token.</returns>
    private Token Peek()
    {
        return _tokens[_current];
    }

    /// <summary>
    /// Gets the previous token.
    /// </summary>
    /// <returns>The previous token.</returns>
    private Token Previous()
    {
        return _tokens[_current - 1];
    }

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    /// <returns>The previous token, after advancing.</returns>
    private Token Advance()
    {
        if (!IsAtEnd)
        {
            _current++;
        }
        return Previous();
    }

    /// <summary>
    /// Checks whether the current token has the given type.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <returns>Whether the current token has the given type.</returns>
    private bool Check(TokenType type)
    {
        if (IsAtEnd)
        {
            return false;
        }
        return Peek().Type == type;
    }

    /// <summary>
    /// Tries to match the current token against a set of allowed types. If matched, advances.
    /// </summary>
    /// <param name="types">The token types on which to match.</param>
    /// <returns>Whether the current token matched one of the given types.</returns>
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

    /// <summary>
    /// Tries to consume the current token; if it does not match the given type, throws an error.
    /// </summary>
    /// <param name="type">The expected token type.</param>
    /// <param name="message">The error message if the type does not match.</param>
    /// <returns>The matched token.</returns>
    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }
        throw Error(Peek(), message);
    }

    /// <summary>
    /// Discards tokens until reaching the beginning of the next statement.
    /// </summary>
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
    /// <summary>
    /// Parses the expression rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Expression()
    {
        // expression → assignment ;

        return Assignment();
    }

    /// <summary>
    /// Parses the assignment rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
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

    /// <summary>
    /// Parses the logicOr rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Or()
    {
        // logicOr → logicAnd ( "or" logicAnd )* ;

        return BinaryLikeExpr<Expr.Logical>(And, TokenType.Or);
    }

    /// <summary>
    /// Parses the logicAnd rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr And()
    {
        // logicAnd → equality ( "and" equality )* ;

        return BinaryLikeExpr<Expr.Logical>(Equality, TokenType.And);
    }

    /// <summary>
    /// Parses the equality rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Equality()
    {
        // equality → comparison ( ( "!=" | "==" ) comparison )* ;

        return BinaryLikeExpr<Expr.Binary>(Comparison, TokenType.BangEqual, TokenType.EqualEqual);
    }

    /// <summary>
    /// Parses the comparison rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Comparison()
    {
        // comparison → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;

        return BinaryLikeExpr<Expr.Binary>(
            Term, TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual
        );
    }

    /// <summary>
    /// Parses the term rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Term()
    {
        // term → factor ( ( "-" | "+" ) factor )* ;

        return BinaryLikeExpr<Expr.Binary>(Factor, TokenType.Minus, TokenType.Plus);
    }

    /// <summary>
    /// Parses the factor rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Factor()
    {
        // factor → unary ( ( "/" | "*" ) unary )* ;

        return BinaryLikeExpr<Expr.Binary>(Unary, TokenType.Slash, TokenType.Star);
    }

    /// <summary>
    /// Parses the unary rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
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

    /// <summary>
    /// Parses the call rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
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

    /// <summary>
    /// Parses the arguments rule.
    /// </summary>
    /// <returns>An Expr list that adheres to the rule.</returns>
    private List<Expr> Arguments()
    {
        // arguments → expression ( "," expression )* ;

        return ItemList("arguments", Expression);
    }

    /// <summary>
    /// Parses the primary rule.
    /// </summary>
    /// <returns>An Expr that adheres to the rule.</returns>
    private Expr Primary()
    {
        // primary → "true" | "false" | "nil" | "this"
        //         | NUMBER | STRING | IDENTIFIER | "(" expression ")"
        //         | "super" "." IDENTIFIER ;

        if (Match(TokenType.True)) { return new Expr.Literal(true); }
        if (Match(TokenType.False)) { return new Expr.Literal(false); }
        if (Match(TokenType.Nil)) { return new Expr.Literal(Nil.Instance); }

        if (Match(TokenType.This))
        {
            return new Expr.This(Previous());
        }

        if (Match(TokenType.Number, TokenType.String))
        {
            // we're assured the scanner set a non-null literal on these token types
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
    /// <summary>
    /// Parses the declaration rule.
    /// </summary>
    /// <returns>A Stmt that adheres to the rule.</returns>
    private Stmt Declaration()
    {
        // declaration → classDecl
        //             | funcDecl
        //             | varDecl
        //             | statement ;

        if (Match(TokenType.Class)) { return ClassDeclaration(); }
        if (Match(TokenType.Fun)) { return Function("function"); } // funDecl → "fun" function ;
        if (Match(TokenType.Var)) { return VarDeclaration(); }
        return Statement();
    }

    /// <summary>
    /// Parses the classDecl rule (after the "class" token).
    /// </summary>
    /// <returns>A Stmt.Class.</returns>
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

    /// <summary>
    /// Parses the function rule.
    /// </summary>
    /// <param name="kind">The kind of function.</param>
    /// <returns>A Stmt.Function.</returns>
    private Stmt.Function Function(string kind)
    {
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

    /// <summary>
    /// Parses the parameters rule.
    /// </summary>
    /// <returns>A list of tokens.</returns>
    private List<Token> Parameters()
    {
        // parameters → IDENTIFIER ( "," IDENTIFIER )* ;

        return ItemList(
            "parameters", () => Consume(TokenType.Identifier, "Expect parameter name.")
        );
    }

    /// <summary>
    /// Parses the varDecl rule (after the "var" token).
    /// </summary>
    /// <returns>A Stmt.Var.</returns>
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

    /// <summary>
    /// Parses the statement rule.
    /// </summary>
    /// <returns>A Stmt that adheres to the rule.</returns>
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

    /// <summary>
    /// Parses a forStmt rule (after the "for" token).
    /// </summary>
    /// <returns>A Stmt that adheres to the rule.</returns>
    private Stmt ForStatement()
    {
        // forStmt → "for"
        //         "(" ( varDecl | exprStmt | ";" ) expression? ";" expression? ")"
        //         statement ;

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

        // desugar to a while loop...

        // evaluate the increment after the body
        if (increment is not null)
        {
            body = new Stmt.Block([body, new Stmt.Expression(increment)]);
        }

        // make sure there's a condition
        condition ??= new Expr.Literal(true);

        // create the loop
        body = new Stmt.While(condition, body);

        // execute the initializer once, before the loop
        if (initializer is not null)
        {
            body = new Stmt.Block([initializer, body]);
        }

        return body;
    }

    /// <summary>
    /// Parses the ifStmt rule (after the "if" token).
    /// </summary>
    /// <returns>A Stmt.If.</returns>
    private Stmt.If IfStatement()
    {
        // ifStmt → "if" "(" expression ")" statement
        //        ( "else" statement )? ;

        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");

        Stmt thenBranch = Statement();

        Stmt? elseBranch = null;
        if (Match(TokenType.Else))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    /// <summary>
    /// Parses the printStmt rule (after the "print" token).
    /// </summary>
    /// <returns>A Stmt.Print.</returns>
    private Stmt.Print PrintStatement()
    {
        // printStmt → "print" expression ";" ;

        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value.");
        return new Stmt.Print(expr);
    }

    /// <summary>
    /// Parses the returnStmt rule (after the "return" token).
    /// </summary>
    /// <returns>A Stmt.Return.</returns>
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

    /// <summary>
    /// Parses a whileStmt rule (after the "while" token).
    /// </summary>
    /// <returns>A Stmt.While.</returns>
    private Stmt.While WhileStatement()
    {
        // whileStmt → "while" "(" expression ")" statement ;

        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");

        Stmt body = Statement();

        return new Stmt.While(condition, body);
    }

    /// <summary>
    /// Parses the block rule (after the "{" token).
    /// </summary>
    /// <returns>A Stmt list that adheres to the rule.</returns>
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

    /// <summary>
    /// Parses the exprStmt rule.
    /// </summary>
    /// <returns>A Stmt.Expression.</returns>
    private Stmt.Expression ExpressionStatement()
    {
        // exprStmt → expression ";" ;

        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Reports an error and creates an exception for the caller to throw.
    /// </summary>
    /// <param name="token">The token where the error occurred.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A ParserError.</returns>
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
    /// <param name="operators">The operators that can participate in this expression.</param>
    /// <returns>An Expr of type <typeparamref name="TExpr"/> or higher precedence.</returns>
    private Expr BinaryLikeExpr<TExpr>(Func<Expr> operand, params TokenType[] operators)
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
    /// <param name="kind">The kind of item, plural.</param>
    /// <param name="consumer">A delegate that knows how to parse an item.</param>
    /// <returns>A list of <typeparamref name="TItem"/>.</returns>
    private List<TItem> ItemList<TItem>(string kind, Func<TItem> consumer)
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

    /// <summary>
    /// Represents an error during parsing. Allows the parser to communicate to itself whether an
    /// exception is recoverable or not.
    /// </summary>
    private class ParseError : Exception { }
    #endregion
}
