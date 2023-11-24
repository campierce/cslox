using Lox.AST;

namespace Lox.Interpreting;

internal class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<Void>
{
    #region Fields/Properties
    private readonly Environment _globals;

    private Environment _environment;

    /// <summary>
    /// Maps an assignment/variable expression to the number of environments between it and the
    /// location of the corresponding variable's value.
    /// </summary>
    private readonly Dictionary<Expr, int> _locals;
    #endregion

    #region Constructors
    public Interpreter()
    {
        _globals = new();
        _environment = _globals;
        _locals = new();

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
            Lox.Error(error);
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

    public void Resolve(Expr expr, int depth)
    {
        _locals[expr] = depth;
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
            if (left is double v && right is double v1)
            {
                return v + v1;
            }
            if (left is string v2 && right is string v3)
            {
                return v2 + v3;
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
            _ => new object() // unreachable
        };
        #pragma warning restore format
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.Callee);

        List<object> arguments = new();
        foreach (Expr argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (callee is ICallable function)
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

        return new object(); // unreachable
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
        // ^ later: allows methods to reference their containing class
        LoxClass @class = new(stmt.Name.Lexeme);
        _environment.Assign(stmt.Name, @class);
        return default;
    }

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.InnerExpression);
        return default;
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        LoxFunction function = new(stmt, _environment);
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
        object value = Evaluate(stmt.Content);
        Console.WriteLine(Stringify(value));
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        object value = Evaluate(stmt.Value);
        throw new Return(value);
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        object value = Evaluate(stmt.Initializer);
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

    private static void CheckNumberOperand(Token @operator, object operand)
    {
        if (operand is double)
        {
            return;
        }
        throw new RuntimeError(@operator, "Operand must be a number.");
    }

    private static void CheckNumberOperands(Token @operator, object left, object right)
    {
        if (left is double && right is double)
        {
            return;
        }
        throw new RuntimeError(@operator, "Operands must be numbers.");
    }

    private static string? Stringify(object obj)
    {
        if (obj is bool b) // C# wants to capitalize this, but Lox does not
        {
            return b.ToString().ToLower();
        }

        return obj.ToString();
    }
    #endregion
}
