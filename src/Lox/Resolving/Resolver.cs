namespace Lox;

/// <summary>
/// Resolves local variables in the AST (i.e., finds where a variable is declared relative to where
/// it is used). This allows us to detect some kinds of invalid programs before runtime, and it
/// allows the interpreter to reuse the same resolution path (which is required for lexical scoping
/// rules).
/// </summary>
internal class Resolver : Expr.IVisitor<Void>, Stmt.IVisitor<Void>
{
    #region State
    /// <summary>
    /// The class context that surrounds the node we are currently visiting.
    /// </summary>
    private ClassType _currentClass = ClassType.None;

    /// <summary>
    /// The function context that surrounds the node we are currently visiting.
    /// </summary>
    private FunctionType _currentFunction = FunctionType.None;

    /// <summary>
    /// The interpreter on which to store the results of this pass.
    /// </summary>
    private readonly Interpreter _interpreter;

    /// <summary>
    /// Stack of scopes that mirror the runtime environment chain, each of which maps a variable
    /// name to whether we have resolved its initializer (which allows us to prevent the initializer
    /// from referencing the variable itself). This is how we prevent name collisions within a given
    /// scope.
    /// <para>
    /// Note the concept of an initializer only applies to variable statements, so other usages of
    /// variables (like a function's name in its declaration) are considered initialized right away.
    /// </para>
    /// </summary>
    private readonly Stack<Dictionary<string, bool>> _scopes = [];
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a Resolver.
    /// </summary>
    /// <param name="interpreter">The associated interpreter.</param>
    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
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

    public Void VisitSuperExpr(Expr.Super expr)
    {
        if (_currentClass == ClassType.None)
        {
            Lox.Error(expr.Keyword, "Can't use 'super' outside of a class.");
        }
        else if (_currentClass != ClassType.Subclass)
        {
            Lox.Error(expr.Keyword, "Can't use 'super' in a class with no superclass.");
        }

        ResolveLocal(expr, expr.Keyword);
        return default;
    }

    public Void VisitThisExpr(Expr.This expr)
    {
        if (_currentClass == ClassType.None)
        {
            Lox.Error(expr.Keyword, "Can't use 'this' outside of a class.");
            return default;
        }

        ResolveLocal(expr, expr.Keyword);
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
            && _scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool isInitialized) // declared
            && !isInitialized) // but not yet defined
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
        ClassType enclosingClsContext = _currentClass;
        _currentClass = ClassType.Class;

        Declare(stmt.Name);
        Define(stmt.Name);

        if (stmt.Superclass is not null)
        {
            if (stmt.Superclass.Name.Lexeme == stmt.Name.Lexeme)
            {
                Lox.Error(stmt.Superclass.Name, "A class can't inherit from itself.");
            }

            _currentClass = ClassType.Subclass;

            // can declare classes in blocks, so the superclass could be a local variable
            Resolve(stmt.Superclass);

            // when a class is declared at runtime, we splice in a closure that binds `super`
            BeginScope(); // must do the same here
            _scopes.Peek()["super"] = true;
        }

        // when a method is accessed at runtime, we splice in a closure that binds `this`
        BeginScope(); // must do the same here
        _scopes.Peek()["this"] = true;

        foreach (Stmt.Function method in stmt.Methods)
        {
            FunctionType type = FunctionType.Method;
            if (method.Name.Lexeme == "init")
            {
                type = FunctionType.Initializer;
            }
            ResolveFunction(method, type);
        }

        EndScope(); // end the `this` scope
        if (stmt.Superclass is not null) { EndScope(); } // end the `super` scope
        _currentClass = enclosingClsContext; // restore previous context
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
        if (_currentFunction == FunctionType.None)
        {
            Lox.Error(stmt.Keyword, "Can't return from top-level code.");
        }

        if (stmt.Value is not null) // user specified a return value
        {
            if (_currentFunction == FunctionType.Initializer) // in a class constructor
            {
                Lox.Error(stmt.Keyword, "Can't return a value from an initializer.");
            }

            Resolve(stmt.Value);
        }
        return default;
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer is not null)
        {
            Resolve(stmt.Initializer);
        }
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
    /// Resolves the "distance" of a local variable -- i.e., finds the number of environments
    /// between that variable's usage (in the given assignment/super/this/variable expr) and its
    /// declaration. Passes the distance to the interpreter for use at runtime.
    /// </summary>
    /// <param name="expr">The expression in which the variable is used.</param>
    /// <param name="name">A token whose lexeme is the variable name.</param>
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
        // if we didn't find it, assume it will exist in the global scope by the time it's needed
    }
    #endregion

    #region Scopes access
    /// <summary>
    /// Begins a new scope.
    /// </summary>
    private void BeginScope()
    {
        _scopes.Push([]);
    }

    /// <summary>
    /// Ends the current scope.
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
    /// Resolves a function of the given type.
    /// </summary>
    /// <param name="function">The function to be resolved.</param>
    /// <param name="type">The type of function.</param>
    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFnContext = _currentFunction;
        _currentFunction = type;

        BeginScope();
        foreach (Token param in function.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.Body);
        EndScope();

        _currentFunction = enclosingFnContext;
    }

    private enum ClassType
    {
        None, // `this` is not permitted
        Class,
        Subclass // `super` is not permitted
    }

    private enum FunctionType
    {
        None, // `return` is not permitted
        Function,
        Initializer, // (class constructor) `return` _with value_ is not permitted
        Method // (class function)
    }
    #endregion
}
