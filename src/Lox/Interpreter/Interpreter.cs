using cslox.lox.ir;
using cslox.lox.scanner;
using static cslox.lox.scanner.TokenType;

namespace cslox.lox.interpreter;

internal class Interpreter : Expr.Visitor<object>
{
    #region API
    public void Interpret(Expr expression)
    {
        try
        {
            object value = Evaluate(expression);
            Console.WriteLine(Stringify(value));
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
  }
    #endregion

    #region Implementations
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
        switch (expr.Operator.Type)
        {
            case GREATER:
                return a > b;
            case GREATER_EQUAL:
                return a >= b;
            case LESS:
                return a < b;
            case LESS_EQUAL:
                return a <= b;
            case MINUS:
                return a - b;
            case SLASH:
                return a / b;
            case STAR:
                return a * b;
        }

        return new object(); // unreachable
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
    #endregion

    #region Helpers
    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private bool IsTruthy(object obj)
    {
        // false and nil are falsey
        // everything else is truthy
        if (obj is Nil) return false;
        if (obj is bool b) return b;
        return false;
    }

    private bool IsEqual(object a, object b)
    {
        // we're fine with C#'s notion of equality
        return a.Equals(b);
    }

    private void CheckNumberOperand(Token _operator, object operand)
    {
        if (operand is double) return;
        throw new RuntimeError(_operator, "Operand must be a number.");
    }

    private void CheckNumberOperands(Token _operator, object left, object right)
    {
        if (left is double && right is double) return;
        throw new RuntimeError(_operator, "Operands must be numbers.");
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
