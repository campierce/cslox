using Lox.AST;
using Lox.Interpreting;

namespace Lox.StaticAnalysis;

internal class Resolver : Expr.IVisitor<Void>, Stmt.IVisitor<Void>
{
    #region Fields
    private readonly Interpreter _interpreter;

    private readonly Stack<Dictionary<string, bool>> _scopes = new();

    private FunctionType _currentFunction = FunctionType.None;
    #endregion

    #region Constructors
    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
    }
    #endregion

    #region API
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

    public Void VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return default;
    }

    public Void VisitVariableExpr(Expr.Variable expr)
    {
        if (_scopes.Count != 0
            && _scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool value) // declared
            && !value) // but not yet defined
        {
            Lox.Error(
                new ResolutionError(expr.Name, "Can't read local variable in its own initializer.")
            );
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

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.InnerExpression);
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
        Resolve(stmt.Content);
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        if (_currentFunction == FunctionType.None)
        {
            Lox.Error(
                new ResolutionError(stmt.Keyword, "Can't return from top-level code.")
            );
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
    private void ResolveLocal(Expr expr, Token name)
    {
        foreach (var (depth, scope) in _scopes.Enumerate())
        {
            if (scope.ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, depth);
                return;
            }
        }
    }
    #endregion

    #region Scopes access
    private void BeginScope()
    {
        _scopes.Push(new Dictionary<string, bool>());
    }

    private void EndScope()
    {
        _scopes.Pop();
    }

    private void Declare(Token name)
    {
        if (_scopes.Count == 0) { return; }

        Dictionary<string, bool> scope = _scopes.Peek();
        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(
                new ResolutionError(name, "Already a variable with this name in this scope.")
            );
        }

        scope[name.Lexeme] = false;
    }

    private void Define(Token name)
    {
        if (_scopes.Count == 0) { return; }

        _scopes.Peek()[name.Lexeme] = true;
    }
    #endregion

    #region Helpers
    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFunction = _currentFunction;
        _currentFunction = type;

        BeginScope();
        foreach (Token param in function.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.Body);
        EndScope();

        _currentFunction = enclosingFunction;
    }
    #endregion Helpers
}
