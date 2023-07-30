using Lox.IR;
using Lox.Scanning;
using static Lox.Scanning.TokenType;
using Void = Lox.IR.Void;

namespace Lox.Interpreting;

internal class Interpreter : Expr.Visitor<object>, Stmt.Visitor<Void>
{
    #region Fields/Properties
    private Environment _environment;
    
    public Environment Globals { get; }
    #endregion

    #region Constructors
    public Interpreter()
    {
        Globals = new Environment();
        _environment = Globals;

        // define native functions
        Globals.Define("clock", new Clock());
    }
    #endregion

    #region API
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }
    #endregion

    #region Expr visitor
    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.Value);
        _environment.Assign(expr.Name, value);
        return value;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        object left = Evaluate(expr.Left);
        object right = Evaluate(expr.Right);

        if (expr.Operator.Type == PLUS)
        {
            if (left is double && right is double)
            {
                return (double)left + (double)right;
            }
            if (left is string && right is string)
            {
                return (string)left + (string)right;
            }
            throw new RuntimeError(expr.Operator, "Operands must be two numbers or two strings.");
        }

        if (expr.Operator.Type == BANG_EQUAL)
        {
            return !IsEqual(left, right);
        }
        if (expr.Operator.Type == EQUAL_EQUAL)
        {
            return IsEqual(left, right);
        }

        CheckNumberOperands(expr.Operator, left, right);

        double a = (double)left;
        double b = (double)right;
        #pragma warning disable format
        return expr.Operator.Type switch
        {
            GREATER       => a > b,
            GREATER_EQUAL => a >= b,
            LESS          => a < b,
            LESS_EQUAL    => a <= b,
            MINUS         => a - b,
            SLASH         => a / b,
            STAR          => a * b,
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
        if (expr.Operator.Type == OR)
        {
            if (IsTruthy(left)) { return left; }
        }
        else
        {
            if (!IsTruthy(left)) { return left; }
        }

        return Evaluate(expr.Right);
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        object right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case BANG:
                return !IsTruthy(right);
            case MINUS:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right;
        }

        return new object(); // unreachable
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        return _environment.Get(expr.Name);
    }
    #endregion

    #region Stmt visitor
    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return default(Void);
    }

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.InnerExpression);
        return default(Void);
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        CallableFunction function = new CallableFunction(stmt);
        _environment.Define(stmt.Name.Lexeme, function);
        return default(Void);
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
        return default(Void);
    }

    public Void VisitPrintStmt(Stmt.Print stmt)
    {
        object value = Evaluate(stmt.Content);
        Console.WriteLine(Stringify(value));
        return default(Void);
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        object value = Evaluate(stmt.Initializer);
        _environment.Define(stmt.Name.Lexeme, value);
        return default(Void);
    }

    public Void VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }
        return default(Void);
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

    public void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = _environment;
        _environment = environment;
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous; // discard this block's scope
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
