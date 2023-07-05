using Lox.IR;
using Lox.Scanning;
using static Lox.Scanning.TokenType;
using Void = Lox.IR.Void;

namespace Lox.Interpreting;

internal class Interpreter : Expr.Visitor<object>, Stmt.Visitor<Void>
{
    #region Fields
    private Environment _environment = new();
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
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
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
    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.InnerExpression);
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
        object value;
        if (stmt.Initializer is not null)
        {
            value = Evaluate(stmt.Initializer);
        }
        else
        {
            value = Nil.GetLiteral();
        }

        _environment.Define(stmt.Name.Lexeme, value);
        return default(Void);
    }
    #endregion

    #region Helpers
    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private bool IsTruthy(object obj)
    {
        // false and nil are falsey, everything else is truthy
        if (obj is Nil) { return false; }
        if (obj is bool b) { return b; }
        return true;
    }

    private bool IsEqual(object a, object b)
    {
        // we're fine with C#'s notion of equality
        return a.Equals(b);
    }

    private void CheckNumberOperand(Token @operator, object operand)
    {
        if (operand is double)
        {
            return;
        }
        throw new RuntimeError(@operator, "Operand must be a number.");
    }

    private void CheckNumberOperands(Token @operator, object left, object right)
    {
        if (left is double && right is double)
        {
            return;
        }
        throw new RuntimeError(@operator, "Operands must be numbers.");
    }

    private string Stringify(object obj)
    {
        if (obj is double num)
        {
            string text = num.ToString();
            if (text.EndsWith(".0"))
            {
                text = text[..^2];
            }
            return text;
        }

        if (obj is bool b)
        {
            return b.ToString().ToLower();
        }

        return obj.ToString() ?? string.Empty;
    }
    #endregion
}
