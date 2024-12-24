namespace Lox;

/// <summary>
/// Resolves variables in the AST (i.e., finds where a variable is declared, relative to where it is
/// used). This allows us to detect some kinds of invalid programs before runtime, and it allows the
/// interpreter to reuse the same resolution path (which is required for static scope).
/// </summary>
internal class Resolver : Expr.IVisitor<Void>, Stmt.IVisitor<Void>
{
    #region State
    /// <summary>
    /// The interpreter on which to store the results of this variable resolution pass.
    /// </summary>
    private readonly Interpreter _interpreter;

    /// <summary>
    /// Stack of lexical scopes, each of which maps a variable name to whether we have resolved its
    /// initializer (which allows us to prevent the initializer from referencing the variable
    /// itself).
    /// <para>
    /// Note the concept of an initializer only applies to variable statements, so other usages of
    /// variables (like a function's name in its declaration) will be considered initialized right
    /// away. Tracking all variables like this allows us to prevent name collisions within the same
    /// scope.
    /// </para>
    /// </summary>
    private readonly Stack<Dictionary<string, bool>> _scopes;

    /// <summary>
    /// The function "context" that surrounds the syntax node we are currently visiting; if none,
    /// then we know a return statement is not permitted.
    /// </summary>
    private FunctionType _fnContext;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new Resolver.
    /// </summary>
    /// <param name="interpreter">The associated interpreter.</param>
    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
        _scopes = new();
        _fnContext = FunctionType.None;
    }
    #endregion

    #region API
    /// <summary>
    /// Resolves a list of statements.
    /// </summary>
    /// <param name="statements">The statements to be resolved.</param>
    public void Resolve(List<Stmt> statements)
    {
        statements.ForEach(Resolve);
    }
    #endregion

    #region Expr visitor
    public Void VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return default;
    }

    public Void VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return default;
    }

    public Void VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);
        expr.Arguments.ForEach(Resolve);
        return default;
    }

    public Void VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Object);
        return default;
    }

    public Void VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
        return default;
    }

    public Void VisitLiteralExpr(Expr.Literal expr)
    {
        return default;
    }

    public Void VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return default;
    }

    public Void VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Object);
        return default;
    }

    public Void VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return default;
    }

    public Void VisitVariableExpr(Expr.Variable expr)
    {
        if (_scopes.Count != 0 // not global
            && _scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool value) // declared
            && !value) // but not yet defined
        {
            Lox.Error(expr.Name, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expr, expr.Name);
        return default;
    }
    #endregion

    #region Stmt visitor
    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return default;
    }

    public Void VisitClassStmt(Stmt.Class stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        foreach (Stmt.Function method in stmt.Methods)
        {
            FunctionType declaration = FunctionType.Method;
            ResolveFunction(method, declaration);
        }

        return default;
    }

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Expr);
        return default;
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionType.Function);
        return default;
    }

    public Void VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch is not null)
        {
            Resolve(stmt.ElseBranch);
        }
        return default;
    }

    public Void VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Expr);
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        if (_fnContext == FunctionType.None)
        {
            Lox.Error(stmt.Keyword, "Can't return from top-level code.");
        }

        Resolve(stmt.Value);
        return default;
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        Resolve(stmt.Initializer);
        Define(stmt.Name);
        return default;
    }

    public Void VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return default;
    }
    #endregion

    #region Interpreter access
    /// <summary>
    /// Resolves a local variable within the interpreter, by finding the number of environments
    /// between that variable's usage and its declaration (its "distance").
    /// </summary>
    /// <param name="expr">The assignment/variable expression in which the variable is used.</param>
    /// <param name="name">A token containing the name of the variable.</param>
    private void ResolveLocal(Expr expr, Token name)
    {
        // walk from inner to outermost scope
        foreach ((int distance, Dictionary<string, bool> scope) in _scopes.Enumerate())
        {
            // if we find the variable, pass its distance to the interpreter
            if (scope.ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, distance);
                return;
            }
        }
        // if we didn't find it, assume it's global; will be handled at runtime
    }
    #endregion

    #region Scopes access
    /// <summary>
    /// Creates a new scope.
    /// </summary>
    private void BeginScope()
    {
        _scopes.Push([]);
    }

    /// <summary>
    /// Removes the current scope.
    /// </summary>
    private void EndScope()
    {
        _scopes.Pop();
    }

    /// <summary>
    /// Declares an uninitialized variable.
    /// </summary>
    /// <param name="name">A token with the variable's name.</param>
    private void Declare(Token name)
    {
        if (_scopes.Count == 0) { return; } // global

        Dictionary<string, bool> scope = _scopes.Peek();
        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(name, "Already a variable with this name in this scope.");
        }

        scope[name.Lexeme] = false; // uninitialized
    }

    /// <summary>
    /// Defines a variable (i.e., marks it as initialized).
    /// </summary>
    /// <param name="name">A token with the variable's name.</param>
    private void Define(Token name)
    {
        if (_scopes.Count == 0) { return; } // global

        _scopes.Peek()[name.Lexeme] = true; // initialized
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Resolves an expression.
    /// </summary>
    /// <param name="expr">The expression to be resolved.</param>
    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    /// <summary>
    /// Resolves a statement.
    /// </summary>
    /// <param name="stmt">The statement to be resolved.</param>
    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    /// <summary>
    /// Resolves a function.
    /// </summary>
    /// <param name="function">The function to be resolved.</param>
    /// <param name="type">The type of function.</param>
    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFnContext = _fnContext;
        _fnContext = type;

        BeginScope();
        foreach (Token param in function.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.Body);
        EndScope();

        _fnContext = enclosingFnContext;
    }
    #endregion Helpers
}
