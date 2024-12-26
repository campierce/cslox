using System.Globalization;

namespace Lox;

internal class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<Void>
{
    #region State
    private readonly Environment _globals;

    private Environment _environment;

    /// <summary>
    /// Maps an expression to the environment distance of the variable it references.
    /// </summary>
    private readonly Dictionary<Expr, int> _locals;
    #endregion

    #region Constructor
    public Interpreter()
    {
        _globals = new();
        _environment = _globals;
        _locals = [];

        // define native functions
        _globals.Define("clock", new Clock());
    }
    #endregion

    #region API
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            statements.ForEach(Execute);
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }

    public void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = _environment;
        try
        {
            _environment = environment;
            statements.ForEach(Execute);
        }
        finally
        {
            _environment = previous; // discard this block's scope
        }
    }

    public void Resolve(Expr expr, int distance)
    {
        _locals[expr] = distance;
    }
    #endregion

    #region Expr visitor
    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.Value);

        if (_locals.TryGetValue(expr, out int distance))
        {
            _environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            _globals.Assign(expr.Name, value);
        }

        return value;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        object left = Evaluate(expr.Left);
        object right = Evaluate(expr.Right);

        if (expr.Operator.Type == TokenType.Plus)
        {
            if (left is double d1 && right is double d2)
            {
                return d1 + d2;
            }
            if (left is string s1 && right is string s2)
            {
                return s1 + s2;
            }
            throw new RuntimeError(expr.Operator, "Operands must be two numbers or two strings.");
        }

        if (expr.Operator.Type == TokenType.BangEqual)
        {
            return !IsEqual(left, right);
        }
        if (expr.Operator.Type == TokenType.EqualEqual)
        {
            return IsEqual(left, right);
        }

        CheckNumberOperands(expr.Operator, left, right);

        double a = (double)left;
        double b = (double)right;
        #pragma warning disable format
        return expr.Operator.Type switch
        {
            TokenType.Greater      => a > b,
            TokenType.GreaterEqual => a >= b,
            TokenType.Less         => a < b,
            TokenType.LessEqual    => a <= b,
            TokenType.Minus        => a - b,
            TokenType.Slash        => a / b,
            TokenType.Star         => a * b,
            _ => throw new RuntimeError(expr.Operator, "Unrecognized operator.") // unreachable
        };
        #pragma warning restore format
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.Callee);

        List<object> arguments = [];
        foreach (Expr argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (callee is ILoxCallable function)
        {
            if (arguments.Count != function.Arity)
            {
                throw new RuntimeError(
                    expr.Paren,
                    $"Expected {function.Arity} arguments but got {arguments.Count}."
                );
            }
            return function.Call(this, arguments);
        }
        throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
    }

    public object VisitGetExpr(Expr.Get expr)
    {
        object obj = Evaluate(expr.Object);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }

        throw new RuntimeError(expr.Name, "Only instances have properties.");
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        object left = Evaluate(expr.Left);

        // attempt to short circuit
        if (expr.Operator.Type == TokenType.Or)
        {
            if (IsTruthy(left)) { return left; }
        }
        else
        {
            if (!IsTruthy(left)) { return left; }
        }

        return Evaluate(expr.Right);
    }

    public object VisitSetExpr(Expr.Set expr)
    {
        object obj = Evaluate(expr.Object);

        if (obj is LoxInstance instance)
        {
            object value = Evaluate(expr.Value);
            instance.Set(expr.Name, value);
            return value;
        }

        throw new RuntimeError(expr.Name, "Only instances have fields.");
    }

    public object VisitThisExpr(Expr.This expr)
    {
        return LookUpVariable(expr.Keyword, expr);
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        object right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(right);
            case TokenType.Minus:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right;
        }

        throw new RuntimeError(expr.Operator, "Unrecognized operator."); // unreachable
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }
    #endregion

    #region Stmt visitor
    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return default;
    }

    public Void VisitClassStmt(Stmt.Class stmt)
    {
        _environment.Define(stmt.Name.Lexeme, Nil.Instance);

        Dictionary<string, LoxFunction> methods = [];
        foreach (Stmt.Function method in stmt.Methods)
        {
            bool isInitializer = method.Name.Lexeme == "init";
            LoxFunction function = new(method, _environment, isInitializer);
            methods[method.Name.Lexeme] = function;
        }

        LoxClass cls = new(stmt.Name.Lexeme, methods);
        _environment.Assign(stmt.Name, cls);
        return default;
    }

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Expr);
        return default;
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        LoxFunction function = new(stmt, _environment, isInitializer: false); // not a method
        _environment.Define(stmt.Name.Lexeme, function);
        return default;
    }

    public Void VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }
        return default;
    }

    public Void VisitPrintStmt(Stmt.Print stmt)
    {
        object value = Evaluate(stmt.Expr);
        Console.WriteLine(Stringify(value));
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        object value;
        if (stmt.Value is null)
        {
            value = Nil.Instance;
        }
        else
        {
            value = Evaluate(stmt.Value);
        }
        throw new Return(value);
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        object value;
        if (stmt.Initializer is null)
        {
            value = Nil.Instance;
        }
        else
        {
            value = Evaluate(stmt.Initializer);
        }
        _environment.Define(stmt.Name.Lexeme, value);
        return default;
    }

    public Void VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }
        return default;
    }
    #endregion

    #region Instance helpers
    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private object LookUpVariable(Token name, Expr expr)
    {
        if (_locals.TryGetValue(expr, out int distance))
        {
            return _environment.GetAt(distance, name.Lexeme);
        }
        else
        {
            return _globals.Get(name);
        }
    }
    #endregion

    #region Static helpers
    private static bool IsTruthy(object obj)
    {
        // nil and false are falsey, everything else is truthy (like Ruby)
        #pragma warning disable format
        return obj switch
        {
            Nil    => false,
            bool b => b,
            _      => true
        };
        #pragma warning disable format
    }

    private static bool IsEqual(object a, object b)
    {
        // we're fine with C#'s notion of equality
        return a.Equals(b);
    }

    private static void CheckNumberOperand(Token op, object operand)
    {
        if (operand is double)
        {
            return;
        }
        throw new RuntimeError(op, "Operand must be a number.");
    }

    private static void CheckNumberOperands(Token op, object left, object right)
    {
        if (left is double && right is double)
        {
            return;
        }
        throw new RuntimeError(op, "Operands must be numbers.");
    }

    private static string? Stringify(object obj)
    {
        if (obj is bool b)
        {
            // C# wants to capitalize this, but Lox does not
            return b.ToString().ToLower(CultureInfo.InvariantCulture);
        }

        return obj.ToString();
    }
    #endregion
}
